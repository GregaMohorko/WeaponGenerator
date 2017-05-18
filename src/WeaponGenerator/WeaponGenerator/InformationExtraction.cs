using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LAIR.Collections.Generic;
using LAIR.ResourceAPIs.WordNet;
using WeaponGenerator.Common;
using WeaponGenerator.Game;
using WeaponGenerator.Helpers;
using WeaponGenerator.mysql;

namespace WeaponGenerator
{
	/// <summary>
	/// We convert the textual data stored in the database into numerical data that can be used in the game one record at a time.
	/// </summary>
	static class InformationExtraction
	{
		/// <summary>
		/// Weapon type detection stop words.
		/// </summary>
		private static readonly string[] STOPWORDS = { "naval", "Naval", "navy", "Navy", "firearm", "Firearm", "gunpowder", "Gunpowder", "mechanical", "Mechanical", "shield", "Shield", "fortification", "Fortification" };

		/// <summary>
		/// Pre-determined weapon classification terms.
		/// </summary>
		private static readonly Dictionary<WeaponType, string[]> TERMS_CLASSIFICATION;
		private static readonly string[] TERMS_CLASSIFICATION_ALL;

		private static readonly List<string> TERMS_ATTACK;
		private static readonly List<string> TERMS_DEFENSE;

		static InformationExtraction()
		{
			TERMS_CLASSIFICATION = new Dictionary<WeaponType, string[]>();
			TERMS_CLASSIFICATION.Add(WeaponType.Dagger, new string[] { "dagger", "Dagger", "knives", "Knives", "knife", "Knife" });
			TERMS_CLASSIFICATION.Add(WeaponType.Sword, new string[] { "sword", "Sword", "cut", "cutting" });
			TERMS_CLASSIFICATION.Add(WeaponType.Axe, new string[] { "axe", "Axe", "ax", "Ax" });
			TERMS_CLASSIFICATION.Add(WeaponType.PoleWeapon, new string[] { "pole", "Pole", "staff", "Staff", "spear", "Spear" });
			TERMS_CLASSIFICATION.Add(WeaponType.Bow, new string[] { "bow", "Bow" });
			TERMS_CLASSIFICATION.Add(WeaponType.Ranged, new string[] { "throw", "Throw", "projectile", "Projectile", "dart", "Dart", "missile", "Missile", "javelin", "Javelin", "throwing", "Throwing" });
			TERMS_CLASSIFICATION.Add(WeaponType.Club, new string[] { "club", "Club", "blunt", "Blunt", "flail", "Flail", "chain", "Chain" });
			TERMS_CLASSIFICATION.Add(WeaponType.Siege, new string[] { "siege", "Siege", "artillery", "Artillery" });

			TERMS_CLASSIFICATION_ALL = TERMS_CLASSIFICATION.SelectMany(kvp => kvp.Value).ToArray();

			// Unlike weapon classification, where we defined the terms we were looking for from beforehand, we cannot manually define the terms needed to represent Attack and Defense.

			// Our chosen solution picks a few root words and searches for similar words.
			string[] attackRoots = { "powerful", "vicious" };
			string[] defenseRoots = { "sturdy", "defensive", "protective", "fortified" };

			// For each root word, we scan through the linked words that are presented by WordNet and add them to a list, leaving out words that are marked as Antonyms, or opposites. We then check out root words and keep adding the results to the list.
			WordNetEngine wordnet = new WordNetEngine(WeaponGeneratorConstants.FOLDER_WORDNET, true);
			Func<string[], List<string>> scanAndAdd = roots =>
			{
				List<string> terms = new List<string>();
				terms.AddRange(roots);
				// go 3 levels
				for(int i = 3; i > 0; --i) {
					List<string> newRoots = new List<string>();
					foreach(string root in roots) {
						Set<SynSet> synonims = wordnet.GetSynSets(root, WordNetEngine.POS.Adjective, WordNetEngine.POS.Adverb, WordNetEngine.POS.Noun);
						foreach(SynSet synonim in synonims) {
							if(synonim.LexicalRelations.Any(r => r == WordNetEngine.SynSetRelation.Antonym))
								continue;
							Debug.Assert(synonim.SemanticRelations.All(r => r != WordNetEngine.SynSetRelation.Antonym));

							newRoots.AddRange(synonim.Words.Where(w => !terms.Contains(w)));
							terms.AddRange(synonim.Words);
						}
					}
					roots = newRoots.ToArray();
				}
				return terms.Distinct().ToList();
			};
			TERMS_ATTACK = scanAndAdd(attackRoots);
			TERMS_DEFENSE = scanAndAdd(defenseRoots);
			wordnet.Close();
			GC.Collect();
		}

		public static List<GameWeapon> ExtractWeaponInformation(List<weapon> weaponDatas)
		{
			List<GameWeapon> weapons = new List<GameWeapon>(weaponDatas.Count);

			Console.WriteLine("Extracting numeric values ... ");
			for(int i = 0; i < weaponDatas.Count; ++i) {
				Console.WriteLine($"Extracting numeric values: {i + 1}/{weaponDatas.Count}");

				weapon data = weaponDatas[i];

				GameWeapon weapon = new GameWeapon();
				weapon.Name = data.weap_name;

				// page 54
				// Weapon Classification
				{
					// We initially took the approach of classifying the articles into the identified weapon types by using term frequency using pre-determined weapon classification terms. This is achieved by counting the number of times a word that is associated with a weapon type appears.

					// Stop words are needed to avoid false positives.
					if(STOPWORDS.Any(sw => Regex.Match(data.weap_text, sw).Success)) {
						continue;
					}

					Dictionary<string, int> frequencies = new Dictionary<string, int>(TERMS_CLASSIFICATION_ALL.Length);

					// To improve our technique, we used weighted term frequency, where the weapon classification terms were given more weighting depending on where they appeared in the text. Terms that appeared in the first two sentences of the article text were given 5 points, while terms that appeared in the category box at the bottom of the web page were also given 5 points.
					string introAndCat;
					{
						string first2Sentences;
						{
							Regex regex = new Regex(@"\.(\[\d\])? ");
							Match first = regex.Match(data.weap_text);
							if(!first.Success) {
								first2Sentences = data.weap_text;
							} else {
								Match second = first.NextMatch();
								if(!second.Success) {
									first2Sentences = data.weap_text;
								} else {
									first2Sentences = data.weap_text.Substring(0, second.Index);
								}
							}
						}
						string categories = data.weap_cat;
						introAndCat = first2Sentences + " " + categories;
					}

					foreach(string term in TERMS_CLASSIFICATION_ALL) {
						int weight = introAndCat.Contains(term) ? 5 : 1;
						int freq = Regex.Matches(data.weap_text, term).Count;
						freq *= weight;
						frequencies.Add(term, freq);
					}

					// The weapon type with the highest number is then assigned to the article being classified.
					WeaponType type = WeaponType.Unknown;
					int highest = -1;
					foreach(var pair in TERMS_CLASSIFICATION) {
						int score = pair.Value.Sum(term => frequencies[term]);
						if(score > highest) {
							type = pair.Key;
							highest = score;
						}
					}
					weapon.Type = type;
				}

				string weap_text_lower = data.weap_text.ToLower();

				// page 55
				// Extracting an Attack and a Defence value
				{
					// To extract values for Attack and Defence, we used a similar method to the one used to classify weapons, we used term frequency for all of the words to determine values that could express that concept.
					weapon.Attack = TERMS_ATTACK.Sum(term => Regex.Matches(weap_text_lower, term).Count);
					//Debug.Assert(weapon.Attack > 0);
					if(weapon.Attack == 0)
						// useless weapon
						continue;
					weapon.Defense = TERMS_DEFENSE.Sum(term => Regex.Matches(weap_text_lower, term).Count);
				}

				// page 56
				// Extracting a Range value
				{
					// We extract the range of the weapon by first finding all values of length in the text. We limit this to values that are measured in metres, centimetres and feet. This is done by using a Regex expression.
					// lengths in metres
					List<double> lengths = new List<double>();
					{
						// metres
						Regex regex = new Regex(@"(\d*\.?\d+) m(et(re|er))?");
						MatchCollection matches = regex.Matches(data.weap_text);
						foreach(Match match in matches) {
							string stringValue = match.Groups[1].Value;
							double metreValue = double.Parse(stringValue);
							lengths.Add(metreValue);
						}
						// centimetres
						regex = new Regex(@"(\d*\.?\d+) c(enti)?m(et(re|er))?");
						matches = regex.Matches(data.weap_text);
						foreach(Match match in matches) {
							string stringValue = match.Groups[1].Value;
							double centimetreValue = double.Parse(stringValue);
							double metreValue = centimetreValue / 100;
							lengths.Add(metreValue);
						}
						// feet
						regex = new Regex(@"(\d*\.?\d+) f(ee|oo)?t");
						matches = regex.Matches(data.weap_text);
						foreach(Match match in matches) {
							string stringValue = match.Groups[1].Value;
							double feetValue = double.Parse(stringValue);
							double metreValue = feetValue * 0.3048;
							lengths.Add(metreValue);
						}
					}

					// To determine a value for each article, we simply compare the result of the average of the values returned to the average length of the article’s weapon type.
					int valuesCount = lengths.Count;
					double weapLength = double.NaN;
					double avgLength = weapon.Type.GetAverageLength();
					if(valuesCount != 0) {
						weapLength = lengths.Average();
					}
					if(valuesCount == 0 || weapLength < (avgLength * 0.5)) {
						// If no values were returned, or if the average of the values returned is significantly lower compared to the average length of its weapon type, then a random number from half the average length to the average length is assigned.
						weapLength = RandomHelper.Between(avgLength * 0.5, avgLength);
					} else if(weapLength > (avgLength * 1.5)) {
						// If, on the other hand, the average of the values returned is significantly higher compared to the average length of its weapon type, then a random number from the average length to 1.5 times the average length is assigned.
						weapLength = RandomHelper.Between(avgLength, avgLength * 1.5);
					}

					weapon.Length = weapLength;
				}

				// page 57
				// Extracting the weapon’s wield type
				{
					// Only weapons that fit in a certain weapon type may override the default wield type according to the instances found in the article text.
					// We set a default value depending on the weapon type.
					weapon.WieldType = weapon.Type.GetDefaultWieldType();
					if(!weapon.Type.IsWieldTypeStatic()) {
						// Similar to the way we extracted the values for Attack and Defence, we extract the weapon’s wield type using term frequency.
						// We thus search the article text for instances of any words with hand in them, notably words such as one-handed, two-handed or offhand and count the instances of each.
						// comment by Grega: why the 'offhand'? I'm going to ignore it for now
						Regex regex = new Regex(@"(one|1)-?hand");
						int oneHandCount = regex.Matches(weap_text_lower).Count;
						regex = new Regex(@"(two|2)-?hand");
						int twoHandCount = regex.Matches(weap_text_lower).Count;
						// Similarly to the weapon’s range, we cannot be certain that all weapons have a wield type written down in the article text.
						if(oneHandCount + twoHandCount > 0) {
							if(oneHandCount > twoHandCount) {
								weapon.WieldType = WieldType.OneHanded;
							} else if(twoHandCount > oneHandCount) {
								weapon.WieldType = WieldType.TwoHanded;
							}
						}
					}
				}

				// page 58
				// Price Determination
				{
					//  Instead of analyzing the article text again to directly determine the price, we opt for a different route by setting a price based on the values of attack, defence and range that have already been retrieved.
					weapon.Price = weapon.Attack * 100 + weapon.Defense * 100 + (int)Math.Floor(weapon.Length / 100) * 100;
				}

				weapons.Add(weapon);
			}
			Console.WriteLine("OK");

			return weapons;
		}
	}
}

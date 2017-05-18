using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeaponGenerator.Common;
using WeaponGenerator.Game;
using WeaponGenerator.mysql;

namespace WeaponGenerator
{
	/// <summary>
	/// This project is an implementation of "Information Extraction Over the Internet for a Dynamic Game" paper, found at: https://pdfs.semanticscholar.org/e8a2/1d38476f84e25fed7d040219916342413b6b.pdf
	/// <para>This project implements only the Information Extraction part of the paper, with a few minor modifications.</para>
	/// <para>Running the program from zero can be quite time consuming (~1h), depending mostly on the internet connection speed.</para>
	/// <para>Results were quite promising, resulting in 900+ weapons successfuly found, categorized and analyzed with all numerical parameters.</para>
	/// </summary>
	class Program
	{
		static void Main(string[] args)
		{
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;

			List<weapon> weaponsData = InformationRetrieval.RetrieveWeaponsData();
			List<GameWeapon> gameWeapons = InformationExtraction.ExtractWeaponInformation(weaponsData);

			if(gameWeapons.Count < weaponsData.Count) {
				Console.WriteLine($"{weaponsData.Count - gameWeapons.Count} were dropped.");
			}
			Console.WriteLine($"Weapons: {gameWeapons.Count}");

			Console.Write("Writing weapon details to file ... ");
			using(FileStream stream = File.Create(WeaponGeneratorConstants.FILE_WEAPONS)) {
				using(StreamWriter writer = new StreamWriter(stream)) {
					for(int i = 0; i < gameWeapons.Count; ++i) {
						GameWeapon weapon = gameWeapons[i];

						writer.WriteLine($"[{i + 1}] {weapon.Name}");
						writer.WriteLine($"\tType: {weapon.Type}");
						writer.WriteLine($"\tLength: {weapon.Length}");
						writer.WriteLine($"\tAttack: {weapon.Attack}");
						writer.WriteLine($"\tDefense: {weapon.Defense}");
						writer.WriteLine($"\tWield type: {weapon.WieldType}");
						writer.WriteLine($"\tPrice: {weapon.Price}");
					}
				}
			}
			Console.WriteLine("OK");

			Console.WriteLine();
			Console.WriteLine($"Details about weapons can be read in file: {WeaponGeneratorConstants.FILE_WEAPONS}");

			Console.WriteLine();
			Console.Write("THE END ");
			Console.ReadKey(true);
		}
	}
}

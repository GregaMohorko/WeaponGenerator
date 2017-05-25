using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using WeaponGenerator.Helpers;
using WeaponGenerator.mysql;
using WeaponGenerator.Utility;

namespace WeaponGenerator.InfoRetrieval
{
	class Link : IDisposable
	{
		private const string WIKIPEDIA_DOMAIN = "https://en.wikipedia.org";
		/// <summary>
		/// We used a list of stopwords to filter out any potentially ambiguous weapons.
		/// </summary>
		private static readonly string[] STOPWORDS = { "People", "people", "Compan", "compan", "Practices", "practices", "List", "list", "Preparation", "preparation", "Armour", "armour", "Disambiguation", "defence", "sport", "Steelmaking", "Robots", "formations", "Muskets", "guns", "Guns", "Magic", "magic", "Political", "Naval", "insignia", "Rifle", "rifle", "Missile", "missile", "Rocket", "rocket", "Aircraft", "aircraft", "Nuclear", "nuclear", "Bomb", "bomb" };

		/// <summary>
		/// Links that are discarded are set aside so that if by some chance they are encountered again later on in the runtime, they do not need to be checked again.
		/// </summary>
		public readonly static List<string> DiscardedLinks = new List<string>();

		public LinkState State;
		public string URL;
		private HtmlNode website
		{
			get
			{
				if(_website != null)
					return _website;
				if(URL != null) {
					_website = NetHelper.DownloadWebsite(WIKIPEDIA_DOMAIN + URL);
					return _website;
				}
				return null;
			}
		}
		private HtmlNode _website;

		public void Dispose()
		{
			_website = null;
		}

		/// <summary>
		/// We use XPath expressions to extract all available links from the article, making sure that the links are taken only from the main content section where the article is present.
		/// </summary>
		public List<Link> GetLinks()
		{
			HtmlNode mainContent = WikipediaUtility.GetMainContent(website);
			HtmlNodeCollection links = mainContent.SelectNodes("//a[starts-with(@href,'/wiki/')]");
			return links
					.Where(n => n.GetAttributeValue("href", null) != null)
					.Select(n => n.Attributes["href"].Value)
					.Distinct()
					.Where(urll =>
					{
						string urllow = urll.ToLower();
						return !urllow.StartsWith("/wiki/file:") &&
							!urllow.StartsWith("/wiki/template:") &&
							!urllow.StartsWith("/wiki/special:");
					})
					.Select(urll => new Link() { URL = urll })
					.ToList();
		}

		/// <summary>
		/// Checks this links URL.
		/// 
		/// All links start off in an Unchecked state. By filtering out the rest of the URL data and extracting the weapon name, we can determine which state to move the link into.Note that we can never discard the link(and thus move into a Discard state) without checking it first, since we do not have enough information from just the URL.
		/// </summary>
		public void Check()
		{
			if(DiscardedLinks.Contains(URL)) {
				goto DISCARD;
			}

			{
				string urllow = URL.ToLower();
				if(urllow.StartsWith("/wiki/talk:") ||
					urllow.StartsWith("/wiki/category:") ||
					urllow.StartsWith("/wiki/category_talk:") ||
					urllow.StartsWith("/wiki/template_talk:")) {
					goto DISCARD;
				}

				// If the link contains the word "weapon" in it, then we change the state of the link to Certain and download the needed information to the database.
				if(urllow.Contains("weapon")) {
					if(STOPWORDS.Any(sw => URL.Contains(sw))) {
						goto DISCARD;
					}
					goto CERTAIN;
				}
			}

			// If however, the link does not contain the word "weapon", then we must check if the word weapon is contained in the Categories section at the bottom of the article.
			List<string> categories;
			{
				categories = WikipediaUtility.GetCategories(website);
				if(categories == null) {
					goto DISCARD;
				}
			}
			if(!categories.Any(c => c.ToLower().Contains("weapon"))) {
				goto DISCARD;
			}

			if(STOPWORDS.Any(sw => categories.Any(c => c.Contains(sw)))) {
				goto DISCARD;
			}

			CERTAIN:
			State = LinkState.Certain;
			return;

			DISCARD:
			State = LinkState.Discard;
		}

		/// <summary>
		/// The details that are necessary in order to perform the information extraction are downloaded.
		/// </summary>
		public weapon DownloadDetails()
		{
			weapon w = new weapon();
			w.weap_name = URL
				.Substring(URL.LastIndexOf('/') + 1)
				.Replace('_', ' ');
			w.weap_url = URL;
			List<string> articleText = WikipediaUtility.GetArticleText(website);
			if(articleText == null || string.IsNullOrWhiteSpace(string.Join("", articleText)))
				return null;
			w.weap_text = string.Join(" ", articleText);
			List<string> categories = WikipediaUtility.GetCategories(website);
			if(categories == null)
				return null;
			w.weap_cat = string.Join(",", categories);
			return w;
		}

		public override bool Equals(object obj)
		{
			if(obj == null)
				return false;

			Link link = obj as Link;

			return URL == link.URL;
		}

		public override int GetHashCode()
		{
			return URL.GetHashCode();
		}

		public override string ToString()
		{
			return $"[{State.ToString()}] {URL}";
		}
	}
}

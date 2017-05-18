using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using WeaponGenerator.Common;
using WeaponGenerator.InfoRetrieval;
using WeaponGenerator.mysql;

namespace WeaponGenerator
{
	static class InformationRetrieval
	{
		public static List<weapon> RetrieveWeaponsData()
		{
			List<Link> links;
			if(!File.Exists(WeaponGeneratorConstants.FILE_LINKS)) {
				// page 48
				List<Link> firstLayerLinks;
				Console.Write("Getting first layer links ... ");
				{
					// We begin the implementation of our solution by choosing the starting page of the information retrieval to be the Wikipedia article titled \List of ancient weapons", found at:
					string startPage = "/wiki/List_of_premodern_combat_weapons";
					Link startingLink = new Link() { URL = startPage };
					firstLayerLinks = startingLink.GetLinks();
				}
				Console.WriteLine("OK");
				Console.WriteLine($"First layer links: {firstLayerLinks.Count}");

				// page 50
				// In order to increase the amount of potential weapons found, we extend this process to the links of the pages found in the first layer of links, which we’ll call the second layer of links.
				List<Link> secondLayerLinks;
				Console.Write("Getting second layer links ...");
				{
					secondLayerLinks = new List<Link>();
					firstLayerLinks.AsParallel().ForAll(l =>
					{
						List<Link> linkss = l.GetLinks();
						lock(secondLayerLinks) {
							secondLayerLinks.AddRange(linkss);
						}
					});
					secondLayerLinks = secondLayerLinks.Distinct().ToList();
				}
				Console.WriteLine("OK");
				// remove duplicates (from the first layer)
				for(int i = secondLayerLinks.Count - 1; i >= 0; --i) {
					if(firstLayerLinks.Contains(secondLayerLinks[i])) {
						secondLayerLinks.RemoveAt(i);
					}
				}
				Console.WriteLine($"Second layer links: {secondLayerLinks.Count}");

				// page 49
				// Once we have a list of links from the web page, we individually check if each link points to a valid weapon article.
				Console.Write("Checking first layer links ... ");
				firstLayerLinks.AsParallel().ForAll(l => l.Check());
				Debug.Assert(firstLayerLinks.All(l => l.State != LinkState.Unchecked));
				firstLayerLinks.ForEach(l => l.Dispose());
				Console.WriteLine("OK");
				{
					List<Link> discardedLinks = firstLayerLinks.Where(l => l.State == LinkState.Discard).ToList();
					if(discardedLinks.Count > 0) {
						firstLayerLinks.RemoveAll(l => l.State == LinkState.Discard);
						Link.DiscardedLinks.AddRange(discardedLinks.Select(l => l.URL));
						Console.WriteLine($"{discardedLinks.Count} were discarded.");
					}
				}

				Console.WriteLine("Checking second layer links ... ");
				for(int i = 0; i < secondLayerLinks.Count; ++i) {
					Console.WriteLine($"Checking second layer links: {i + 1}/{secondLayerLinks.Count}");
					secondLayerLinks[i].Check();
					secondLayerLinks[i].Dispose();
				}
				Console.WriteLine("OK");
				Debug.Assert(secondLayerLinks.All(l => l.State != LinkState.Unchecked));
				{
					List<Link> discardedLinks = secondLayerLinks.Where(l => l.State == LinkState.Discard).ToList();
					if(discardedLinks.Count > 0) {
						secondLayerLinks.RemoveAll(l => l.State == LinkState.Discard);
						Link.DiscardedLinks.AddRange(discardedLinks.Select(l => l.URL));
						Console.WriteLine($"{discardedLinks.Count} were discarded.");
					}
				}

				links = new List<Link>(firstLayerLinks);
				links.AddRange(secondLayerLinks);

				Debug.Assert(links.All(l => l.State == LinkState.Certain));

				// save links to a file
				File.WriteAllLines(WeaponGeneratorConstants.FILE_LINKS, links.Select(l => l.URL));
			} else {
				// retrieve links from a file
				links = File.ReadAllLines(WeaponGeneratorConstants.FILE_LINKS).Select(url => new Link() { URL = url, State = LinkState.Certain }).ToList();
			}

			// TEST links
			// Use this for testing Categorization results
			// See appendix in section D.2
			/*
			links = new List<Link>();
			links.Add(new Link() { URL = "/wiki/Macuahuitl" });
			links.Add(new Link() { URL = "/wiki/Takoba" });
			links.Add(new Link() { URL = "/wiki/Surujin" });
			links.Add(new Link() { URL = "/wiki/Pole_weapon" });
			links.Add(new Link() { URL = "/wiki/Falx" });
			links.Add(new Link() { URL = "/wiki/Naginata" });
			links.Add(new Link() { URL = "/wiki/Rhomphaia" });
			links.Add(new Link() { URL = "/wiki/Sarissa" });
			links.Add(new Link() { URL = "/wiki/Soliferrum" });
			links.Add(new Link() { URL = "/wiki/Spear" });
			links.Add(new Link() { URL = "/wiki/Tepoztopilli" });
			links.Add(new Link() { URL = "/wiki/Trident" });
			*/
			// Use this for testing Numerical Data Extraction
			// See appendix in section E.1
			/*
			links = new List<Link>();
			links.Add(new Link() { URL = "/wiki/Small_sword" });
			links.Add(new Link() { URL = "/wiki/Flame-bladed_sword" });
			links.Add(new Link() { URL = "/wiki/Firangi_(sword)" });
			links.Add(new Link() { URL = "/wiki/Jian" });
			links.Add(new Link() { URL = "/wiki/Dao_(sword)" });
			links.Add(new Link() { URL = "/wiki/Scimitar" });
			links.Add(new Link() { URL = "/wiki/Spatha" });
			links.Add(new Link() { URL = "/wiki/Model_1832_foot_artillery_sword" });
			links.Add(new Link() { URL = "/wiki/Butterfly_sword" });
			links.Add(new Link() { URL = "/wiki/Sword_of_Goujian" });
			links.Add(new Link() { URL = "/wiki/Shamshir" });
			links.Add(new Link() { URL = "/wiki/Ida_(sword)" });
			links.Add(new Link() { URL = "/wiki/Hook_sword" });
			links.Add(new Link() { URL = "/wiki/Mameluke_sword" });
			links.Add(new Link() { URL = "/wiki/Talwar" });
			links.Add(new Link() { URL = "/wiki/Kalis" });
			links.Add(new Link() { URL = "/wiki/Kampilan" });
			links.Add(new Link() { URL = "/wiki/Falchion" });
			links.Add(new Link() { URL = "/wiki/Sica" });
			links.Add(new Link() { URL = "/wiki/Dha_(sword)" });
			*/

			Console.WriteLine($"Certain links: {links.Count}");

			// page 51
			// When the link has been determined to be a weapon, and is therefore in a Certain state, the details that are necessary in order to perform the information extraction are saved.
			List<weapon> details;
			if(!File.Exists(WeaponGeneratorConstants.FILE_DETAILS)) {
				Console.WriteLine("Downloading details of links ... ");
				details = new List<weapon>(links.Count);
				for(int i = 0; i < links.Count; ++i) {
					Console.WriteLine($"Downloading details of links: {i + 1}/{links.Count}");
					weapon detail = links[i].DownloadDetails();
					links[i].Dispose();
					if(detail == null)
						continue;
					details.Add(detail);
				}
				Console.WriteLine("OK");

				// save details to a file
				weapons weapons = new weapons();
				weapons.elements = details.ToArray();
				XmlSerializer serializer = new XmlSerializer(typeof(weapons));
				using(FileStream stream = File.OpenWrite(WeaponGeneratorConstants.FILE_DETAILS)) {
					serializer.Serialize(stream, weapons);
				}
			} else {
				// retrieve details from a file
				weapons weapons;
				XmlSerializer serializer = new XmlSerializer(typeof(weapons));
				using(FileStream stream = File.OpenRead(WeaponGeneratorConstants.FILE_DETAILS)) {
					weapons = serializer.Deserialize(stream) as weapons;
				}
				details = weapons.elements.ToList();
			}

			Console.WriteLine($"Weapons details: {details.Count}");

			return details;
		}
	}
}

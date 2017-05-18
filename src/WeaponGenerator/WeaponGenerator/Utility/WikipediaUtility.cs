using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace WeaponGenerator.Utility
{
	/// <summary>
	/// Created: 2017-05-15. It might not work if Wikipedia changed its HTML structure by the time this was coded.
	/// </summary>
	public static class WikipediaUtility
	{
		public static HtmlNode GetMainContent(HtmlNode parentNode)
		{
			return parentNode.SelectSingleNode("//div[@id='mw-content-text']");
		}

		public static List<string> GetArticleText(HtmlNode parentNode)
		{
			HtmlNode mainContent = GetMainContent(parentNode);
			HtmlNodeCollection paragraphs = mainContent.SelectNodes("//p");
			if(paragraphs == null)
				return null;
			return paragraphs.Select(n => n.InnerText).ToList();
		}

		public static List<string> GetCategories(HtmlNode parentNode)
		{
			HtmlNode categoriesDiv = parentNode.SelectSingleNode("//div[@id='mw-normal-catlinks']");
			if(categoriesDiv == null) {
				return null;
			}
			HtmlNode categoriesUl = categoriesDiv.SelectSingleNode("./ul");
			List<HtmlNode> categorieNodes = categoriesUl.ChildNodes
				.Where(n => n.NodeType == HtmlNodeType.Element)
				.ToList();
			return categorieNodes.Select(n => n.InnerText).ToList();
		}
	}
}

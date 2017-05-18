using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeaponGenerator.mysql
{
	public class weapon
	{
		/// <summary>
		/// The weapon name, of datatype varchar, is taken from the URL of the web page. For example, the weapon name obtained from the URL http://en.wikipedia.org/wiki/Mere_(weapon) would be Mere (weapon).
		/// </summary>
		public string weap_name;
		/// <summary>
		/// The weapon URL, of datatype varchar, is the actual URL that links to the web page.
		/// </summary>
		public string weap_url;
		/// <summary>
		/// The text, of datatype mediumtext, is the article text that appears on the web page.
		/// </summary>
		public string weap_text;
		/// <summary>
		/// The categories, of datatype tinytext, are the list of categories found at the bottom of every article.These are stored as one long string.
		/// </summary>
		public string weap_cat;
	}
}

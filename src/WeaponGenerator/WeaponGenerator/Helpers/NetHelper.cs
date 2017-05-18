using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace WeaponGenerator.Helpers
{
	/// <summary>
	/// Static thread-safe net helper with max 2 simultaneous requests.
	/// </summary>
	static class NetHelper
	{
		private const int MAX_CONSECUTIVE_REQUESTS = 2;
		private static volatile int @switch = 0;
		private static object lock_switch = new object();
		private static object[] locks;

		static NetHelper()
		{
			locks = new object[MAX_CONSECUTIVE_REQUESTS];
			for(int i = locks.Length - 1; i >= 0; --i) {
				locks[i] = new object();
			}
		}

		public static HtmlNode DownloadWebsite(string url)
		{
			int mySwitch;
			lock(lock_switch) {
				mySwitch = @switch;
				@switch = (@switch + 1) % MAX_CONSECUTIVE_REQUESTS;
			}

			string content;
			lock(locks[mySwitch]) {
				using(var webClient = new WebClient()) {
					content = webClient.DownloadString(url);
				}
			}
			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(content);
			return doc.DocumentNode;
		}
	}
}

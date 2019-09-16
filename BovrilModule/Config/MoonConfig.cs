using System;
using System.Collections.Generic;
using System.Text;

namespace BovrilModule
{
	public class MoonConfig
	{
		public string MoonMessage = "@here New moon {0} popping in 10 minutes!";

		public List<string> MoonDataUrls = new List<string>();

		public int Timeout = 2000;

		public string ConnectionString = "Server=localhost; database=moons; UID=root; password=prople;";

		public ulong MoonPingChannelID = 0;

		public string MoonPingMessage = "<@&568038554631143424> This moon is popping in 10 minutes!";
	}
}

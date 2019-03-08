using System;
using System.Collections.Generic;
using System.Text;

namespace BovrilModule
{
	public class BovrilConfig
	{
		public string MoonMessage = "@here New moon {0} popping in 10 minutes!";

		public string AnomalyMessage = "@here New {0} in {1} spawning in 10 minutes!";

		public string OutputTimeFormat = "dd/MM hh:mm:ss";

        public string InputTimeFormat = "yyyy.MM.dd HH:mm";

        public List<string> MoonDataUrls = new List<string>();

		public List<(string regEx, string message, bool dm)> FileterdWords = new List<(string, string, bool)>();

        public int Timeout = 2000;
    }
}

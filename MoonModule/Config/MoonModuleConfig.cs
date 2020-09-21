using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Config
{
	public class MoonModuleConfig
	{
		public string Database { get; set; }

		public string MoonPingMessage { get; set; }

		public string MoonPingChannel { get; set; }

		public int CalendarCheckInterval { get; set; }
	}
}

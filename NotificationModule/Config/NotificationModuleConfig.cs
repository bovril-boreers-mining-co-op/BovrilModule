using System;
using System.Collections.Generic;
using System.Text;
using YahurrFramework;

namespace Modules
{
	public class NotificationModuleConfig
	{
		public string OutputTimeFormat { get; set; } = "dd/MM hh:mm:ss";

		public string[] InputTimeFormats { get; set; } = new string[] {
			"yyyy.MM.dd HH:mm",
			"yyyy.M.d HH:mm",
		};
	}
}

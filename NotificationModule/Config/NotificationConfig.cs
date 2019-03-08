using System;
using System.Collections.Generic;
using System.Text;

namespace NModule
{
	public class NotificationConfig
	{
		public string OutputTimeFormat = "dd/MM hh:mm:ss";

		public string[] InputTimeFormats = new string[] {
			"yyyy.MM.dd HH:mm",
			"yyyy.M.d HH:mm",
		};
	}
}

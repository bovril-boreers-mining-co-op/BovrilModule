using System;
using System.Collections.Generic;
using System.Text;
using YahurrFramework.Enums;

namespace LogModule.Structs
{
	public struct LogChannel
	{
		public string Name { get; set; }

		public LogLevel LogLevel { get; set; }

		public ulong ID { get; set; }
	}
}

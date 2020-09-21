using BovrilModule.Config;
using System;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;
using YahurrFramework.Enums;

namespace Modules
{
	[Config(typeof(BovrilModuleConfig))]
	public class BovrilModule : YModule
	{
		public new BovrilModuleConfig Config
		{
			get
			{
				return (BovrilModuleConfig)base.Config;
			}
		}

		protected override Task Init()
		{
			return LogAsync(LogLevel.Message, "Hello, World!");
		}
	}
}

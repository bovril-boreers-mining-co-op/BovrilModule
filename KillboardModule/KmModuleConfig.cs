using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace KillboardModule
{
	public class KmModuleConfig
	{
		[JsonProperty]
		public ulong OutputChannelID { get; private set; }

		[JsonProperty]
		public ulong TargetCorp { get; private set; } = 0;

		[JsonProperty]
		public string ZkillChannel { get; private set; } = "killstream";
	}
}

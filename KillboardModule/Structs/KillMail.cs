using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace KillboardModule
{
	internal class KillMail
	{
		[JsonProperty("attackers")]
		public List<Attacker> Attackers { get; private set; }

		[JsonProperty("killmail_id")]
		public ulong Killmail { get; private set; }

		[JsonProperty("killmail_time")]
		public string Time { get; private set; }

		[JsonProperty("solar_system_id")]
		public ulong Solarsystem { get; private set; }

		[JsonProperty("victim")]
		public Victim Victim { get; private set; }

		[JsonProperty("zkb")]
		public ZKB Zkb { get; private set; }
	}
}

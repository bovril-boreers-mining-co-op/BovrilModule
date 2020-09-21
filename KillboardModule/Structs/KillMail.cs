using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Modules
{
	public struct KillMail
	{
		[JsonPropertyName("attackers")]
		public List<Attacker> Attackers { get; set; }

		[JsonPropertyName("killmail_id")]
		public ulong KillmailId { get; set; }

		[JsonPropertyName("killmail_time")]
		public string Time { get; set; }

		[JsonPropertyName("solar_system_id")]
		public ulong Solarsystem { get; set; }

		[JsonPropertyName("victim")]
		public Victim Victim { get; set; }

		[JsonPropertyName("zkb")]
		public ZKB Zkb { get; set; }
	}
}

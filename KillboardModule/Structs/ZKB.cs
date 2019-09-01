using Newtonsoft.Json;

namespace KillboardModule
{
	internal class ZKB
	{
		[JsonProperty("locationID")]
		public ulong Location { get; private set; }

		[JsonProperty("hash")]
		public string Hash { get; private set; }

		[JsonProperty("fittedValue")]
		public float FittedValue { get; private set; }

		[JsonProperty("totalValue")]
		public float TotalValue { get; private set; }

		[JsonProperty("points")]
		public int Points { get; private set; }

		[JsonProperty("npc")]
		public bool Npc { get; private set; }

		[JsonProperty("solo")]
		public bool Solo { get; private set; }

		[JsonProperty("awox")]
		public bool Awox { get; private set; }

		[JsonProperty("esi")]
		public string Esi { get; private set; }

		[JsonProperty("url")]
		public string Url { get; private set; }
	}
}
using System.Text.Json.Serialization;

namespace Modules
{
	public struct ZKB
	{
		[JsonPropertyName("locationID")]
		public ulong Location { get; set; }

		[JsonPropertyName("hash")]
		public string Hash { get; set; }

		[JsonPropertyName("fittedValue")]
		public float FittedValue { get; set; }

		[JsonPropertyName("totalValue")]
		public float TotalValue { get; set; }

		[JsonPropertyName("points")]
		public int Points { get; set; }

		[JsonPropertyName("npc")]
		public bool Npc { get; set; }

		[JsonPropertyName("solo")]
		public bool Solo { get; set; }

		[JsonPropertyName("awox")]
		public bool Awox { get; set; }

		[JsonPropertyName("esi")]
		public string Esi { get; set; }

		[JsonPropertyName("url")]
		public string Url { get; set; }
	}
}

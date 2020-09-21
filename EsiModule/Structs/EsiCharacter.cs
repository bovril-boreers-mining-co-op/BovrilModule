using System;
using System.Text.Json.Serialization;

namespace Modules
{
	public struct EsiCharacter
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("description")]
		public string Description { get; set; }

		[JsonPropertyName("title")]
		public string Title { get; set; }

		[JsonPropertyName("alliance_id")]
		public int AllianceId { get; set; }

		[JsonPropertyName("corporation_id")]
		public int CorporationId { get; set; }

		[JsonPropertyName("ancestry_id")]
		public int AncestryId { get; set; }

		[JsonPropertyName("bloodline_id")]
		public int BloodlineId { get; set; }

		[JsonPropertyName("faction_id")]
		public int FactionId { get; set; }

		[JsonPropertyName("race_id")]
		public int RaceId { get; set; }

		[JsonPropertyName("birthday")]
		public DateTime Birthday { get; set; }

		[JsonPropertyName("security_status")]
		public float SecurityStatus { get; set; }
	}
}

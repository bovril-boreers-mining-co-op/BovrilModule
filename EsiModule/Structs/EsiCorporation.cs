using System;
using System.Text.Json.Serialization;

namespace Modules
{
	public struct EsiCorporation
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("description")]
		public string Description { get; set; }

		[JsonPropertyName("ticker")]
		public string Ticker { get; set; }

		[JsonPropertyName("url")]
		public string Url { get; set; }

		[JsonPropertyName("corporation_id")]
		public int CorporationID { get; set; }

		[JsonPropertyName("alliance_id")]
		public int AllianceId { get; set; }

		[JsonPropertyName("ceo_id")]
		public int CeoId { get; set; }

		[JsonPropertyName("creator_id")]
		public int CreatorId { get; set; }

		[JsonPropertyName("faction_id")]
		public int FactionId { get; set; }

		[JsonPropertyName("home_station_id")]
		public int HomeStationId { get; set; }

		[JsonPropertyName("member_count")]
		public int MemberCount { get; set; }

		[JsonPropertyName("shares")]
		public long Shares { get; set; }

		[JsonPropertyName("tax_rate")]
		public float TaxRate { get; set; }

		[JsonPropertyName("date_founded")]
		public DateTime Founded { get; set; }

		[JsonPropertyName("war_eligible")]
		public bool WarEligible { get; set; }
	}
}

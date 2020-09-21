using System;
using System.Text.Json.Serialization;

namespace Modules
{
	public struct EsiCharacterCorporationHistory
	{
		[JsonPropertyName("corporation_id")]
		public int CorporationID { get; set; }

		[JsonPropertyName("is_deleted")]
		public bool Deleted { get; set; }

		[JsonPropertyName("record_id")]
		public int Record { get; set; }

		[JsonPropertyName("start_date")]
		public DateTime Date { get; set; }
	}
}

using System.Text.Json.Serialization;

namespace Modules
{
	public struct Character
	{
		[JsonPropertyName("alliance_id")]
		public ulong AllianceID { get; set; }

		[JsonPropertyName("character_id")]
		public ulong CharacterID { get; set; }

		[JsonPropertyName("corporation_id")]
		public ulong CorporationID { get; set; }

		[JsonPropertyName("ship_type_id")]
		public int ShipType { get; set; }
	}
}

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Modules
{
	public struct Victim
	{
		[JsonPropertyName("character_id")]
		public ulong CharacterID { get; set; }

		[JsonPropertyName("corporation_id")]
		public ulong CorporationID { get; set; }

		[JsonPropertyName("ship_type_id")]
		public int ShipType { get; set; }

		[JsonPropertyName("damage_taken")]
		public ulong DamageTaken { get; set; }

		[JsonPropertyName("items")]
		public List<Item> Items { get; set; }

		[JsonPropertyName("position")]
		public Vector3 Position { get; set; }
	}
}

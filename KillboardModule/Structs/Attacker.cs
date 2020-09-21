using System.Text.Json.Serialization;

namespace Modules
{
	public struct Attacker
	{
		[JsonPropertyName("character_id")]
		public ulong CharacterID { get; set; }

		[JsonPropertyName("corporation_id")]
		public ulong CorporationID { get; set; }

		[JsonPropertyName("ship_type_id")]
		public int ShipType { get; set; }

		[JsonPropertyName("damage_done")]
		public ulong DamageDone { get; set; }

		[JsonPropertyName("final_blow")]
		public bool FinalBlow { get; set; }

		[JsonPropertyName("security_status")]
		public float SecurityStatus { get; set; }

		[JsonPropertyName("weapon_type_id")]
		public int WeaponType { get; set; }
	}
}

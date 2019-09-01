using Newtonsoft.Json;

namespace KillboardModule
{
	internal class Character
	{
		[JsonProperty("alliance_id")]
		public ulong AllianceID { get; private set; }

		[JsonProperty("character_id")]
		public ulong CharacterID { get; private set; }

		[JsonProperty("corporation_id")]
		public ulong CorporationID { get; private set; }

		[JsonProperty("ship_type_id")]
		public int ShipType { get; private set; }
	}
}
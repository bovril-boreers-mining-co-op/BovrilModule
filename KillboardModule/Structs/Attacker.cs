using Newtonsoft.Json;

namespace KillboardModule
{
	internal class Attacker : Character
	{
		[JsonProperty("damage_done")]
		public ulong DamageDone { get; private set; }

		[JsonProperty("final_blow")]
		public bool FinalBlow { get; private set; }

		[JsonProperty("security_status")]
		public float SecurityStatus { get; private set; }

		[JsonProperty("weapon_type_id")]
		public int WeaponType { get; private set; }
	}
}
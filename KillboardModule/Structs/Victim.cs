using Newtonsoft.Json;
using System.Collections.Generic;

namespace KillboardModule
{
	internal class Victim : Character
	{
		[JsonProperty("damage_taken")]
		public ulong DamageTaken { get; private set; }

		[JsonProperty("items")]
		public List<Item> Items { get; private set; }

		[JsonProperty("position")]
		public Point Position { get; private set; }
	}
}
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Modules
{
	public struct SeatGroup
	{
		[JsonPropertyName("id")]
		public int ID { get; set; }

		[JsonPropertyName("main_character_id")]
		public string MainCharacter { get; set; }

		[JsonPropertyName("users")]
		public List<SeatGroupUser> Users { get; set; }
	}
}

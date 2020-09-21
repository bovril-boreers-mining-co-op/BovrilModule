using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Modules
{
	public struct SeatUser
	{
		[JsonPropertyName("id")]
		public long ID { get; set; }

		[JsonPropertyName("group_id")]
		public int GroupID { get; set; }

		[JsonPropertyName("associated_character_ids")]
		public List<int> AssociatedCharacters { get; set; }
	}
}

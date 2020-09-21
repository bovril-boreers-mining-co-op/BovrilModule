using System.Text.Json.Serialization;

namespace Modules
{
	public struct SeatGroupUser
	{
		[JsonPropertyName("active")]
		public bool active { get; set; }

		[JsonPropertyName("character_id")]
		public long CharacterID { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }
	}
}

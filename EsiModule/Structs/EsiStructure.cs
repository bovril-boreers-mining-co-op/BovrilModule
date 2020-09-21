using System.Text.Json.Serialization;

namespace Modules
{
	public struct EsiStructure
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("owner_id")]
		public int OwnerId { get; set; }

		[JsonPropertyName("position")]
		public EsiPosition Position { get; set; }

		[JsonPropertyName("solar_system_id")]
		public int SolarSystemId { get; set; }

		[JsonPropertyName("type_id")]
		public int TypeId { get; set; }
	}
}

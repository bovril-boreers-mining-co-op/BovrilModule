using System.Text.Json.Serialization;

namespace Modules
{
	public struct EsiItem
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("description")]
		public string Description { get; set; }
	}
}

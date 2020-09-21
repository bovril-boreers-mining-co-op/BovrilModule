using System.Text.Json.Serialization;

namespace Modules
{
	public struct EsiStrutureService
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("state")]
		public string State { get; set; }
	}
}

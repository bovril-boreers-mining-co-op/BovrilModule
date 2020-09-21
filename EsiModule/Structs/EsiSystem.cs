using System.Text.Json.Serialization;

namespace Modules
{
	public struct EsiSystem
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }
	}
}

using System.Text.Json.Serialization;

namespace Modules
{
	public struct EsiPosition
	{
		[JsonPropertyName("x")]
		public int X { get; set; }

		[JsonPropertyName("y")]
		public int Y { get; set; }

		[JsonPropertyName("z")]
		public int Z { get; set; }
	}
}

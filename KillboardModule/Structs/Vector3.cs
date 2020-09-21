using System.Text.Json.Serialization;

namespace Modules
{
	public struct Vector3
	{
		[JsonPropertyName("x")]
		public float X { get; set; }

		[JsonPropertyName("y")]
		public float Y { get; set; }

		[JsonPropertyName("z")]
		public float Z { get; set; }
	}
}

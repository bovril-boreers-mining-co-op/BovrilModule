using System.Text.Json.Serialization;

namespace Modules
{
	public struct SeatData<T>
	{
		[JsonPropertyName("data")]
		public T Data { get; set; }
	}
}

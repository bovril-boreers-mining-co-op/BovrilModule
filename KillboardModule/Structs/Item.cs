using System.Text.Json.Serialization;

namespace Modules
{
	public struct Item
	{
		[JsonPropertyName("flag")]
		public int Flag { get; set; }

		[JsonPropertyName("type_id")]
		public int TypeID { get; set; }

		[JsonPropertyName("quantity_dropped")]
		public int QuantityDropped { get; set; }

		[JsonPropertyName("singleton")]
		public int Singleton { get; set; }
	}
}

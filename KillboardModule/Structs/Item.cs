using Newtonsoft.Json;

namespace KillboardModule
{
	internal class Item
	{
		[JsonProperty("flag")]
		public int Flag { get; private set; }

		[JsonProperty("type_id")]
		public int TypeID { get; private set; }

		[JsonProperty("quantity_dropped")]
		public int QuantityDropped { get; private set; }

		[JsonProperty("singleton")]
		public int Singleton { get; private set; }
	}
}
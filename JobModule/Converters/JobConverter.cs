using Modules.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules
{
	internal class JobConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return typeof(IJob).IsAssignableFrom(objectType);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			return serializer.Deserialize(reader, objectType);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			serializer.Serialize(writer, value);
		}
	}
}

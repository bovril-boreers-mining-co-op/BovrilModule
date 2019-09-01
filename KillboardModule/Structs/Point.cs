using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace KillboardModule
{
	internal class Point
	{
		[JsonProperty("x")]
		public float X { get; private set; }

		[JsonProperty("y")]
		public float Y { get; private set; }

		[JsonProperty("z")]
		public float Z { get; private set; }
	}
}

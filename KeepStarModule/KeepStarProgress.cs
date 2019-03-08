using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace KeepStarModule
{
	class KeepStarProgress
	{
		[JsonProperty]
		public float Wallet { get; private set; }

		[JsonProperty]
		public float P1 { get; private set; }

		[JsonProperty]
		public float P2 { get; private set; }

		[JsonProperty]
		public float P3 { get; private set; }

		[JsonProperty]
		public float P4 { get; private set; }

		[JsonProperty]
		public float TotalPI { get; private set; }

		[JsonProperty]
		public float BuyOrdersEscrow { get; private set; }

		[JsonProperty]
		public float BuyOrders { get; private set; }

		[JsonProperty]
		public List<(float, string)> AssetHistory { get; private set; }

		[JsonProperty]
		public float Complete { get; private set; }
	}
}

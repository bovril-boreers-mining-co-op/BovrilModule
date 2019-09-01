using Microsoft.OpenApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace RecruitmentModule
{
	public class RecruitmentConfig
	{
		[JsonProperty]
		public string ConnectionString { get; set; }

		[JsonProperty]
		public ulong LogChannel { get; set; }

		[JsonProperty]
		public int NameCheckInterval { get; set; }

		[JsonProperty]
		public ulong AuthedRole { get; set; }

		[JsonProperty]
		public ulong CorpRole { get; set; }

		[JsonProperty]
		public string PipeName { get; set; } = "RecruitmentModule";

		[JsonProperty]
		public string WelcomeMessage { get; set; }
	}
}

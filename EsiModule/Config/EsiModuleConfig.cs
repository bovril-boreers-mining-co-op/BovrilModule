using System;
using System.Collections.Generic;
using System.Text;

namespace EsiModule.Config
{
	public class ApiModuleConfig
	{
		public string EsiClientID { get; set; }

		public string EsiCallback { get; set; }

		public string EsiUseAgent { get; set; }

		public string EsiDefaultUser { get; set; }

		public string SeatToken { get; set; }

		public string SeatDefaultUser { get; set; }
	}
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Modules
{
	public class RecruitmentModuleConfig
	{
		public ulong AuthedRole { get; set; }

		public ulong CorpRole { get; set; }

		public ulong AllianceRole { get; set; }

		public ulong LegacyRole { get; set; }

		public int CorporationID { get; set; }

		public int AllianceID { get; set; }

		public string LogChannel { get; set; }

		public string CheckupLogChannel { get; set; }

		public string RecruitmentDatabase { get; set; }

		public int NameVerificationInterval { get; set; }

		public bool NameVerificationLoop { get; set; }

		public string WelcomeMessage { get; set; }
	}
}

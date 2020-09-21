using System;
using System.Collections.Generic;
using System.Text;

namespace Modules
{
	public sealed class RoleModuleConfig
	{
		public string RoleDatabase { get; set; }

		public string LogChannel { get; set; }

		public bool RoleCheckLoop { get; set; }

		public int RoleCheckInterval { get; set; }

		public List<ManagedRole> RolesToManage { get; set; }
	}

	public struct ManagedRole
	{
		public ulong RoleID { get; set; }

		public bool ManualAdd { get; set; }

		public string Description { get; set; }
	}
}

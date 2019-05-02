using System;
using System.Collections.Generic;
using System.Text;

namespace NModule.Interfaces
{
	interface IJob
	{
		string Creator { get; }

		DateTime Start { get; }
	}
}

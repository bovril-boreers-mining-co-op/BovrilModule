using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Interfaces
{
	public interface IJob
	{
		string Creator { get; }

		TimeSpan Duartion { get; }

		bool Repeat { get; }
	}
}

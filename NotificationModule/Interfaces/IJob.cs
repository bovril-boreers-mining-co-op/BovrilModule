using System;
using System.Collections.Generic;
using System.Text;

namespace NModule.Interfaces
{
	public interface IJob : IComparable<IJob>
	{
		string Creator { get; }

		DateTime Start { get; }
	}
}

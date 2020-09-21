using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Modules
{
	public class DatabaseRow : IEnumerable<object>
	{
		public List<object> Data { get; }

		public DatabaseRow(List<object> data)
		{
			this.Data = data;
		}

		public T GetData<T>(int ordinal)
		{
			return (T)Data[ordinal];
		}

		public IEnumerator<object> GetEnumerator()
		{
			return Data.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Data.GetEnumerator();
		}
	}
}

using Modules.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules
{
	public class NotificationJob : IJob
	{
		public string Creator { get; set; }

		public TimeSpan Duartion { get; set; }

		public bool Repeat { get; set; } = false;

		public string Message { get; set; }

		public List<string> Channels { get; set; }

		public NotificationJob(string creator, TimeSpan duartion, string message, List<string> channels)
		{
			this.Creator = creator;
			this.Duartion = duartion;
			this.Message = message;
			this.Channels = channels;
		}
	}
}

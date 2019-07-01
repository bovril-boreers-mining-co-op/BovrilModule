using Discord.WebSocket;
using Newtonsoft.Json;
using NModule.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NModule
{
	public class Notification : IJob
	{
		[JsonProperty]
		public string Creator { get; private set; }

		[JsonProperty]
		public DateTime Start { get; set; }

		[JsonProperty]
		public string Message { get; set; }

		[JsonProperty]
		public List<string> Channels { get; private set; }

        [JsonConstructor]
		public Notification(string Author, DateTime Time, string Message, List<string> Channels)
		{
			this.Creator = Author;
			this.Start = Time;
			this.Message = Message;
			this.Channels = Channels;
		}

		public int CompareTo(IJob other)
		{
			return Start.CompareTo(other.Start);
		}
	}
}

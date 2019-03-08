using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace NModule
{
	public partial class Notification
	{
		[JsonProperty]
		public string Author { get; private set; }

		[JsonProperty]
		public DateTime Time { get; set; }

		[JsonProperty]
		public string Message { get; set; }

		[JsonProperty]
		public List<string> Channels { get; private set; }

        [JsonConstructor]
		public Notification(string Author, DateTime Time, string Message, List<string> Channels)
		{
			this.Author = Author;
			this.Time = Time;
			this.Message = Message;
			this.Channels = Channels;
		}
	}
}

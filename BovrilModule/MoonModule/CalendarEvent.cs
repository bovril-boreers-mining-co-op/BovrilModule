using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BovrilModule
{
	public struct CalendarEvent
	{
		[JsonProperty("event_date")]
		public string EventDate { get; set; }

		[JsonProperty("event_id")]
		public int EventID { get; set; }

		[JsonProperty("event_response")]
		public string EventResponse { get; set; }

		[JsonProperty("importance")]
		public int Importance { get; set; }

		[JsonProperty("title")]
		public string Title { get; set; }
	}
}

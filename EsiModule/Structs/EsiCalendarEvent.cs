using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Modules
{
	public struct EsiCalendarEvent
	{
		[JsonPropertyName("event_date")]
		public DateTime EventDate { get; set; }

		[JsonPropertyName("event_id")]
		public int EventID { get; set; }

		[JsonPropertyName("event_response")]
		public string EventResponse { get; set; }

		[JsonPropertyName("importance")]
		public int Importance { get; set; }

		[JsonPropertyName("title")]
		public string Title { get; set; }
	}
}

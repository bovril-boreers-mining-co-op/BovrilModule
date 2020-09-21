using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Modules
{
	public struct EsiCorporationStructure
	{

		[JsonPropertyName("corporation_id")]
		public int CorporationId { get; set; }

		[JsonPropertyName("fuel_expires")]
		public DateTime FuelExpires { get; set; }

		[JsonPropertyName("next_reinforce_apply")]
		public DateTime NextReinforceApply { get; set; }

		[JsonPropertyName("next_reinforce_hour")]
		public int NextReinforceHour { get; set; }

		[JsonPropertyName("next_reinforce_weekday")]
		public int NextReinforceWeekday { get; set; }

		[JsonPropertyName("profile_id")]
		public int ProfileId { get; set; }

		[JsonPropertyName("reinforce_hour")]
		public int ReinforceHour { get; set; }

		[JsonPropertyName("reinforce_weekday")]
		public int ReinforceWeekday { get; set; }

		[JsonPropertyName("services")]
		public List<EsiStrutureService> Services { get; set; }

		[JsonPropertyName("state")]
		public string State { get; set; }

		[JsonPropertyName("state_timer_end")]
		public DateTime StateTimerEnd { get; set; }

		[JsonPropertyName("state_timer_start")]
		public DateTime StateTimerStart { get; set; }

		[JsonPropertyName("structure_id")]
		public long StructureId { get; set; }

		[JsonPropertyName("system_id")]
		public int SystemId { get; set; }

		[JsonPropertyName("type_id")]
		public int TypeId { get; set; }

		[JsonPropertyName("unanchors_at")]
		public DateTime UnanchorsAt { get; set; }
	}
}

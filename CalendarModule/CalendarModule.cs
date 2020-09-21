using Modules.Interfaces;
using Modules.Jobs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;

namespace Modules
{
	public delegate bool EventPredicate(EsiCalendarEvent e);
	public delegate IJob JobFactory(EsiCalendarEvent e);

	[RequiredModule(typeof(ApiModule), typeof(JobModule))]
	[Config(typeof(CalendarModuleConfig))]
	public class CalendarModule : YModule
	{
		public new CalendarModuleConfig Config
		{
			get
			{
				return (CalendarModuleConfig)base.Config;
			}
		}

		private ApiModule ApiModule { get; set; }

		private JobModule JobModule { get; set; }

		List<EventPredicate> jobPredicates = new List<EventPredicate>();
		List<JobFactory> jobFactories = new List<JobFactory>();

		// Time of the last event that was checked.
		DateTime lastCheckedEvent = DateTime.UtcNow;

		protected override async Task Init()
		{
			ApiModule = await GetModuleAsync<ApiModule>();
			JobModule = await GetModuleAsync<JobModule>();

			if (await ExistsAsync("CalendarLastEventChecked"))
				lastCheckedEvent = await LoadAsync<DateTime>("CalendarLastEventChecked");

			await JobModule.RegisterJobAsync<CalendarUpdateJob>(CalendarUpdateJob);

			if (!await JobModule.JobExistsAsync<CalendarUpdateJob>(x => x.Creator == "CalendarModule"))
				await JobModule.AddJobAsync(new CalendarUpdateJob("CalendarModule", new TimeSpan(Config.CalendarCheckInterval, 0, 0)));
		}

		#region Commands

		/// <summary>
		/// Get the lastCheckedEvent proeprty.
		/// </summary>
		/// <returns></returns>
		[Summary("Get date of latest event that was checked.")]
		[Command("calendar", "last")]
		public Task GetLastCheckedEventDate()
		{
			return RespondAsync(lastCheckedEvent.ToString(), false, false);
		}

		/// <summary>
		/// Scan corp calendar for moon pulls.
		/// </summary>
		/// <returns></returns>
		[Summary("Scan calendar for new moon pulls")]
		[Command("calendar", "update")]
		public async Task CalendarUpdate()
		{
			await RespondAsync("Scanning calendar for moon pulls...", false, false);
			await CalendarUpdateJob(null, null);
			await RespondAsync("Calendar scanned for moons.", false, false);
		}

		#endregion

		/// <summary>
		/// Register a job for a calendar event as specified by the <paramref name="predicate"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="predicate">Predicate filter calendar events.</param>
		/// <param name="factory">Factory for creating job after a match has been found.</param>
		/// <param name="job">Job method that will be ran when event starts.</param>
		public Task RegisterEvent<T>(EventPredicate predicate, JobFactory factory, Job<T> job) where T : IJob
		{
			jobPredicates.Add(predicate);
			jobFactories.Add(factory);

			return JobModule.RegisterJobAsync(job);
		}

		/// <summary>
		/// Job for scanning a calendar and looking for new moon pulls.
		/// </summary>
		/// <param name="job"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		async Task CalendarUpdateJob(CalendarUpdateJob job, MethodContext context)
		{
			List<EsiCalendarEvent> events = await GetCalendarEvents();
			foreach (EsiCalendarEvent e in events)
			{
				if (e.EventDate < lastCheckedEvent)
					continue;

				for (int i = 0; i < jobPredicates.Count; i++)
				{
					if (jobPredicates[i](e))
						await JobModule.AddJobAsync(jobFactories[i](e));
				}
			}

			lastCheckedEvent = events[events.Count - 1].EventDate;
			await SaveAsync("CalendarLastEventChecked", lastCheckedEvent);
		}

		/// <summary>
		/// Return all calendar events that is a moon pull
		/// </summary>
		/// <returns></returns>
		async Task<List<EsiCalendarEvent>> GetCalendarEvents()
		{
			return (await ApiModule.Esi.Path("/characters/{character_id}/calendar/").Get<List<EsiCalendarEvent>>(("character_id", Config.CharacterID))).FirstPage;
		}
	}
}

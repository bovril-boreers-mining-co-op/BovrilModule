using Discord;
using EveOpenApi.Api;
using Modules.Config;
using Modules.Jobs;
using Modules.MoonParser;
using Org.BouncyCastle.Asn1.Cms;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;
using Modules.Interfaces;

namespace Modules
{
	[RequiredModule(typeof(DatabaseModule), typeof(JobModule), typeof(LogModule), typeof(CalendarModule))]
	[YahurrFramework.Attributes.Config(typeof(MoonModuleConfig))]
	public class MoonModule : YModule
	{
		public new MoonModuleConfig Config
		{
			get
			{
				return (MoonModuleConfig)base.Config;
			}
		}

		private MoonParser.MoonParser MoonParser { get; set; }

		private DatabaseModule DatabaseModule { get; set; }

		private JobModule JobModule { get; set; }

		private LogModule LogModule { get; set; }

		private CalendarModule CalendarModule { get; set; }

		protected override async Task Init()
		{
			await LogAsync(YahurrFramework.Enums.LogLevel.Message, $"Initializing {this.GetType().Name}...");
			this.MoonParser = new MoonParser.MoonParser();

			DatabaseModule = await GetModuleAsync<DatabaseModule>();
			JobModule = await GetModuleAsync<JobModule>();
			LogModule = await GetModuleAsync<LogModule>();
			CalendarModule = await GetModuleAsync<CalendarModule>();

			await CalendarModule.RegisterEvent<MoonJob>(IsMoonEvent, MoonJobFactory, MoonPoppedJob);
		}

		#region Commands

		/// <summary>
		/// Get moon composition information, only public and corp moon info are available.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		[Example("!moon B-7FDU 3-1")]
		[Summary("get public or corp moon composition information.")]
		[Command("moon")]
		public Task GetMoonStats(params string[] input)
		{
			if (!this.MoonParser.TryParse(input, out SystemMoon moon))
				return RespondAsync("```Invalid input.```", false, false);

			if (!TryGetMoon(moon, out MoonComposition composition))
				return RespondAsync("```Moon has not been scanned yet.```", false, false);

			return Channel.SendMessageAsync(embed: composition.PrettyMoon());
		}

		/// <summary>
		/// List all currently scheduled moons.
		/// </summary>
		/// <returns></returns>
		[Summary("List all currently scheduled moons.")]
		[Command("moon", "list")]
		public async Task MoonList()
		{
			StringBuilder reply = new StringBuilder();
			reply.Append("Scheduled moons:\n");

			List<MoonJob> jobs = await JobModule.GetJobsAsync<MoonJob>();
			for (int i = 0; i < jobs.Count; i++)
			{
				MoonJob job = jobs[i];
				DateTime callTime = JobModule.GetNextCall(job);

				reply.Append($"	{i}: {job.Moon} will pop on {callTime.ToString("dd/MM HH:mm:ss")}\n");
			}

			if (jobs.Count <= 0)
				await RespondAsync("No moons listed.", false, false);
			
			await RespondAsync($"```{reply}```", false, false);
		}

		#endregion

		/// <summary>
		/// Try to get the moon composition from a parsed moon.
		/// </summary>
		/// <param name="moon"></param>
		/// <param name="composition"></param>
		/// <returns></returns>
		bool TryGetMoon(SystemMoon moon, out MoonComposition composition)
		{
			composition = null;

			// Query for retriving a list of all ore types as well as the quantity for a moon.
			string moonDataQuery = $"SELECT c.type_name, b.quantity FROM mapdata a, moondata b, typedata c WHERE a.item_name = '{moon.Name}' AND b.moon_id = a.item_id AND c.type_id = b.type_id;";
			// Query for checking wether the moon station is a tatara or not.
			string isTataraQuery = $"SELECT EXISTS(SELECT * FROM moonrefinery a, mapdata b WHERE a.moon_id = b.item_id and b.item_name = '{moon.Name}');";

			List<DatabaseRow> moonData = DatabaseModule.RunQuery(Config.Database, moonDataQuery);
			List<DatabaseRow> isTatara = DatabaseModule.RunQuery(Config.Database, isTataraQuery);

			if (moonData.Count == 0)
				return false;

			composition = new MoonComposition(moon, isTatara[0].GetData<long>(0) == 1, moonData);
			return true;
		}

		/// <summary>
		/// Check if calendar event is for a moon pull.
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		bool IsMoonEvent(EsiCalendarEvent e)
		{
			return this.MoonParser.TryParse(e.Title.Split(' '), out SystemMoon moon) && TryGetMoon(moon, out _);
		}

		/// <summary>
		/// Create a moon job for a moon pull calendar event.
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		IJob MoonJobFactory(EsiCalendarEvent e)
		{
			SystemMoon moon = MoonParser.Parse(e.Title.Split(' '));
			if (!TryGetMoon(moon, out MoonComposition moonComp))
				throw new NullReferenceException("Moon not found");

			return new MoonJob("MoonModule", moonComp.Name, e.EventDate - DateTime.UtcNow - new TimeSpan(0, 10, 0));
		}

		/// <summary>
		/// Job for sending a discord message ten minutes before a moon is about to pop.
		/// </summary>
		/// <param name="job"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		Task MoonPoppedJob(MoonJob job, MethodContext context)
		{
			SystemMoon moon = this.MoonParser.Parse(job.Moon.Split(' '));
			if (TryGetMoon(moon, out MoonComposition comp))
				return LogModule.LogMessage(Config.MoonPingMessage, Config.MoonPingChannel, embed: comp.PrettyMoon());

			return Task.CompletedTask;
		}
	}
}

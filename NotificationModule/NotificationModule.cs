using Discord;
using Discord.WebSocket;
using Modules.NotificationParser;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;

namespace Modules
{
	[Config(typeof(NotificationModuleConfig))]
	[RequiredModule(typeof(JobModule))]
	public class NotificationModule : YModule
	{
		public new NotificationModuleConfig Config
		{
			get
			{
				return (NotificationModuleConfig)base.Config;
			}
		}

		private NotificationParser.NotificationParser NotificationParser { get; set; }

		private JobModule JobModule { get; set; }

		protected override async Task Init()
		{
			await LogAsync(YahurrFramework.Enums.LogLevel.Message, $"Initializing {this.GetType().Name}...");

			NotificationParser = new NotificationParser.NotificationParser(Config);

			JobModule = await GetModuleAsync<JobModule>();
			await JobModule.RegisterJobAsync<NotificationJob>(SayNotificationJob);
		}

		#region Commands

		/// <summary>
		/// PArse a notification useing natural language
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		[Example("!reminder 1h 2m say This is a notification to #general")]
		[Example("!reminder 1 hour 2 min say This is a notification to #general")]
		[Example("!reminder 1h 2m to #general say This is a notification")]
		[Example("!reminder to me in 1h 2m say This is a notification")]
		[Command("reminder"), Summary("Create a notification using natrual language.")]
		public async Task ParseNotifcation(
			[Summary("See examples for proper syntax.")] params string[] input)
		{
			string name = (Message.Author as IGuildUser)?.Nickname ?? Message.Author.Username;
			if (!NotificationParser.TryParse(input, name, out NotificationJob job))
			{
				await RespondAsync("Invalid input.", false, false);
				return;
			}

			await JobModule.AddJobAsync(job);

			DateTime time = JobModule.GetNextCall(job);
			await RespondAsync($"Notification added for: {time.ToString(Config.OutputTimeFormat)} to {string.Join(',', job.Channels)}", false, false);
		}

		/// <summary>
		/// Get a list of all reminders a person has created
		/// </summary>
		/// <returns></returns>
		[Summary("Get a list of all reminders you have created.")]
		[Command("reminder", "list")]
		public async Task NotificationList()
		{
			string name = (Message.Author as IGuildUser).Nickname ?? Message.Author.Username;
			List<NotificationJob> jobs = await JobModule.GetJobsAsync<NotificationJob>(x => x.Creator == name);

			await RespondAsync($"```Notifications by {name}:\n{NotificationsToString(jobs)}```", false, false);
		}

		/// <summary>
		/// Get a list of all reminders.
		/// </summary>
		/// <returns></returns>
		[Summary("Get a list of all reminders.")]
		[Command("reminder", "list", "all")]
		public async Task NotificationListAll()
		{
			List<NotificationJob> jobs = await JobModule.GetJobsAsync<NotificationJob>();
			await RespondAsync($"```Notifications:\n{NotificationsToString(jobs)}```", false, false);
		}

		/// <summary>
		/// Delete a notification
		/// </summary>
		/// <param name="reminder"></param>
		/// <returns></returns>
		[Summary("Delete a notification.")]
		[Command("del")]
		public async Task DeleteNotification(
			[Summary("Reminder to delete.")]int reminder)
		{
			string name = (Message.Author as IGuildUser).Nickname ?? Message.Author.Username;
			List<NotificationJob> jobs = await JobModule.GetJobsAsync<NotificationJob>(x => x.Creator == name);

			if (reminder < 0 || reminder >= jobs.Count)
			{
				await RespondAsync("Index out of bounds.", false, false);
				return;
			}

			await JobModule.Remove(jobs[reminder]);
			await RespondAsync("Reminder removed.", false, false);
		}

		#endregion

		/// <summary>
		/// Say a servere notification to the correct channels
		/// </summary>
		/// <param name="job"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		async Task SayNotificationJob(NotificationJob job, MethodContext context)
		{
			SetContext(context);
			foreach (string channel in job.Channels)
			{
				switch (channel)
				{
					case "pm": case "dm": case "me":
						IGuildUser user = GetUser(job.Creator, true);
						await user?.SendMessageAsync(job.Message);
						break;
					default:
						IMessageChannel messageChannel = GetChannel<SocketGuildChannel>(channel, false) as IMessageChannel;
						await messageChannel?.SendMessageAsync(job.Message);
						break;
				}
			}
		}

		/// <summary>
		/// Convert a list of notifications to a presentable string.
		/// </summary>
		/// <param name="jobs"></param>
		/// <returns></returns>
		string NotificationsToString(List<NotificationJob> jobs)
		{
			StringBuilder reply = new StringBuilder();

			for (int i = 0; i < jobs.Count; i++)
			{
				NotificationJob job = jobs[i];
				DateTime time = JobModule.GetNextCall(job);

				string shorthand = job.Message;
				if (shorthand.Length > 40)
					shorthand = shorthand.Substring(0, 40) + "...";

				reply.Append($"{i}: {time} to {string.Join(',', GetChannels(job.Channels))} by {job.Creator} say {shorthand}\n");
			}

			if (jobs.Count == 0)
				reply.Append("No notifications.");

			return reply.ToString();
		}

		/// <summary>
		/// Parse channel names and use original names where it is not a guild channel name.
		/// </summary>
		/// <param name="channels"></param>
		/// <returns></returns>
		List<string> GetChannels(List<string> channels)
		{
			List<string> parsedChannels = new List<string>();

			for (int i = 0; i < channels.Count; i++)
			{
				string channel = channels[i];
				ISocketMessageChannel foundChannel = GetChannel<SocketGuildChannel>(channel, false) as ISocketMessageChannel;

				parsedChannels.Add(foundChannel?.Name ?? channel);
			}

			return parsedChannels;
		}
	}
}

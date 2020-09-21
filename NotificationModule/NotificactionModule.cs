using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using NModule.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;
using YahurrFramework.Enums;
using YahurrLexer;

namespace NModule
{
	[Config(typeof(NotificationConfig))]
	[RequiredModule(typeof(JobQueueModule))]
	public class NotificationModule : YModule
	{
		public new NotificationConfig Config
		{
			get
			{
				return (NotificationConfig)base.Config;
			}
		}

		private NotificationParser NotificationParser { get; set; }

		private JobQueueModule JobQueueModule { get; set; }

		protected override async Task Init()
		{
			NotificationParser = new NotificationParser(Config);

			JobQueueModule = await GetModuleAsync<JobQueueModule>();
			JobQueueModule.RegisterJob<Notification>(RespondNotification);

			await Cleanup();
		}

		#region Commands

		[Command("reminder", "list"), Summary("List all active notifications.")]
		public async Task ListNotifications()
		{
			await RespondAsync($"```{NotificationsToString()}```");
		}

		[IgnoreHelp]
		[Command("reminder", "show")]
		public async Task ShowNotification(int index)
		{
			List<Notification> notifications = JobQueueModule.GetJobs<Notification>();
			Notification notification = notifications[index];
			await Channel?.SendMessageAsync(notification.Message, embed: notification.Embed?.Build());
		}

		[Example("!del 0")]
		[Command("del"), Summary("Delete selected notification.")]
		public async Task DeleteNotification(
			[Summary("Index from !notify list")]int index = -1)
		{
			if (index == -1)
			{
				await RespondAsync(
					$"```Please specify wich notification to delete.\n" +
					$"{NotificationsToString()}```");
				return;
			}

			if (!await JobQueueModule.RemoveJob(index))
			{
				await RespondAsync("Index too high ya fool!");
				return;
			}

			await RespondAsync($"```Notification removed!```");
		}

		[Example("!reminder 1h 2m say This is a notification to #general")]
		[Example("!reminder 1 hour 2 min say This is a notification to #general")]
		[Example("!reminder 1h 2m to #general say This is a notification")]
		[Command("reminder"), Summary("Create a notification using natrual language.")]
		public async Task ParseNotifcation(
			[Summary("See examples for proper syntax.")]params string[] input)
		{
			try
			{
				string author = (Message.Author as SocketGuildUser)?.Nickname ?? Message.Author.Username;
				Notification notification = Parse(input, author);

				if (notification.Channels == null || notification.Channels.Count == 0)
					throw new Exception("No channels specified");

				foreach (string channel in notification.Channels)
				{
					SocketGuildChannel socketChannel = GetChannel<SocketGuildChannel>(channel, false);

					if (socketChannel is null)
						throw new Exception($"Unknown channel: '{channel}'");
				}

				if (notification.Start == DateTime.Now)
					throw new Exception("Error parsing time");

				if (string.IsNullOrEmpty(notification.Message))
					throw new Exception("No text specified");

				await AddNotification(notification);
			}
			catch (Exception e)
			{
				string output = $"Error parsing input:\n{e.Message}";

				await RespondAsync($"```{output}```");
				return;
			}
		}

		#endregion

		/// <summary>
		/// Parse user input to notification
		/// </summary>
		/// <param name="input"></param>
		/// <param name="author"></param>
		/// <returns></returns>
		public Notification Parse(string[] input, string author)
		{
			return NotificationParser.Parse(input, author);
		}

		/// <summary>
		/// Create and add a new notification
		/// </summary>
		/// <param name="dateTime"></param>
		/// <param name="msg"></param>
		/// <param name="channels"></param>
		/// <returns></returns>
		public async Task<Notification> AddNotification(DateTime dateTime, string msg, List<string> channels, EmbedBuilder embed = null)
		{
			string authorName = (Message?.Author as SocketGuildUser)?.Nickname ?? Message.Author.Username;
			Notification notification = new Notification(authorName, dateTime, msg, channels, embed);

			return await AddNotification(notification);
		}

		/// <summary>
		/// Add notification.
		/// </summary>
		/// <param name="notification"></param>
		/// <returns></returns>
		public async Task<Notification> AddNotification(Notification notification)
		{
			await JobQueueModule.AddJob(notification);

			List<ISocketMessageChannel> channels = new List<ISocketMessageChannel>();
			foreach (string channel in notification.Channels)
			{
				ISocketMessageChannel foundChannel = GetChannel<SocketGuildChannel>(channel, true) as ISocketMessageChannel;
				channels.Add(foundChannel);
			}

			await RespondAsync($"```" +
								$"Notification added for: {notification.Start.ToUniversalTime().ToString(Config.OutputTimeFormat)} to {string.Join(',', channels)}" +
								$"```");
			return notification;
		}

		/// <summary>
		/// Get notification within timeframe.
		/// </summary>
		/// <param name="dateTime"></param>
		/// <param name="span"></param>
		/// <returns></returns>
		public Notification GetNotification(DateTime dateTime, TimeSpan span)
		{
			DateTime start = dateTime - span;
			DateTime end = dateTime + span;

			return JobQueueModule.GetJob<Notification>(x => x.Start > start && x.Start < end);
		}

		/// <summary>
		/// Remove all notifications that was due when program was offline.
		/// </summary>
		/// <returns></returns>
		async Task Cleanup()
		{
			await LogAsync(LogLevel.Message, $"Starting notifications cleanup...");

			int found = 0;
			List<Notification> notifications = JobQueueModule.GetJobs<Notification>();
			for (int i = 0; i < notifications.Count; i++)
			{
				Notification notification = notifications[i];

				if (notification.Start < DateTime.UtcNow && await JobQueueModule.RemoveJob(notification))
					found++;
			}

			if (found > 0)
			{
				await LogAsync(LogLevel.Message, $"Removed {found} notification{(found == 1 ? "" : "s")}.");
			}
			else
				await LogAsync(LogLevel.Message, $"Done.");
		}

		async Task RespondNotification(Notification notification)
		{
			foreach (var channel in notification.Channels)
			{
				List<Notification> notifications = JobQueueModule.GetJobs<Notification>();

				ISocketMessageChannel foundChannel = GetChannel<SocketGuildChannel>(channel, false) as ISocketMessageChannel;
				await foundChannel?.SendMessageAsync(notification.Message, embed: notification.Embed?.Build());
			}
		}

		string NotificationsToString()
		{
			string output = "";

			List<Notification> notifications = JobQueueModule.GetJobs<Notification>();
			if (notifications.Count == 0)
				output += "No notifications.";

			for (int i = 0; i < notifications.Count; i++)
				output += $"{i} {NotificationToString(notifications[i])}\n";

			return output;
		}

		string NotificationToString(Notification notification)
		{
			List<ISocketMessageChannel> channels = new List<ISocketMessageChannel>();

			foreach (string channel in notification.Channels)
			{
				ISocketMessageChannel foundChannel = GetChannel<SocketGuildChannel>(channel, true) as ISocketMessageChannel;
				channels.Add(foundChannel);
			}

			string shorthand = notification.Message;
			if (shorthand.Length > 40)
				shorthand = shorthand.Substring(0, 40) + "...";

			DateTime time = TimeZoneInfo.ConvertTimeToUtc(notification.Start);
			return $"{time.ToUniversalTime().ToString(Config.OutputTimeFormat)} to {string.Join(',', channels)} by {notification.Creator} say {shorthand}\n";
		}
	}
}

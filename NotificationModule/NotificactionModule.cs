using Discord.WebSocket;
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
	public class NotificationModule : YModule
	{
		public new NotificationConfig Config
		{
			get
			{
				return (NotificationConfig)base.Config;
			}
		}

		SortedList<DateTime, Notification> notifications;
		SemaphoreSlim signal;
		NotificationParser notificationParser;

		protected override async Task Init()
		{
			notifications = new SortedList<DateTime, Notification>();
			signal = new SemaphoreSlim(0, 1);
			notificationParser = new NotificationParser(Config);

			await LoadNotifications();
			await Cleanup();

			Loop();
		}

		#region Commands

		[Command("reminder", "list"), Summary("List all active notifications.")]
		public async Task ListNotifications()
		{
			await RespondAsync($"```{NotificationListToString()}```");
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
					$"{NotificationListToString()}```");
				return;
			}

			if (index >= notifications.Count)
			{
				await RespondAsync("Index too high ya fool!");
				return;
			}

			Notification notification = notifications.ElementAt(index).Value;
			notifications.RemoveAt(index);
			signal.Release();

			string output = "```";
			output += "Removed notification:\n";
			output += NotificationToString(notification);

			await RespondAsync(output + "```");
			await SaveAsync("Notifications", notifications.ToList());
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

				if (notification.Time == DateTime.Now)
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

		public Notification Parse(string[] input, string author)
		{
			return notificationParser.Parse(input, author);
		}

		public async Task<Notification> AddNotification(DateTime dateTime, string msg, List<string> channels)
		{
			string authorName = (Message?.Author as SocketGuildUser)?.Nickname ?? Message.Author.Username;
			Notification notification = new Notification(authorName, dateTime, msg, channels);

			return await AddNotification(notification);
		}

		public async Task<Notification> AddNotification(Notification notification)
		{
			notifications.Add(notification.Time, notification);
			signal.Release();

			List<ISocketMessageChannel> channels = new List<ISocketMessageChannel>();
			foreach (string channel in notification.Channels)
			{
				ISocketMessageChannel foundChannel = GetChannel<SocketGuildChannel>(channel, true) as ISocketMessageChannel;
				channels.Add(foundChannel);
			}

			await SaveAsync("Notifications", notifications.ToList());
			await RespondAsync($"```" +
								$"Notification added for: {notification.Time.ToString(Config.OutputTimeFormat)} to {string.Join(',', channels)}" +
								$"```");
			return notification;
		}

		/// <summary>
		/// Main loop for waiting on notifications
		/// </summary>
		async void Loop()
		{
			Task sephamore = signal.WaitAsync();

			while (true)
			{
				Notification notification = notifications.FirstOrDefault().Value;
				Task waitTask = Task.Delay(-1);

				if (!(notification is null) && notification.Time > TimeZoneInfo.ConvertTimeToUtc(DateTime.Now))
					waitTask = Task.Delay(notification.Time - TimeZoneInfo.ConvertTimeToUtc(DateTime.Now));

				await Task.WhenAny(sephamore, waitTask);

				if (waitTask.IsCompleted)
				{
					foreach (var channel in notification.Channels)
					{
						ISocketMessageChannel foundChannel = GetChannel<SocketGuildChannel>(channel, false) as ISocketMessageChannel;
						await foundChannel?.SendMessageAsync(notification.Message);
					}

					notifications.Remove(notification.Time);
					await SaveAsync("Notifications", notifications.ToList());
				}

				if (sephamore.IsCompleted)
					sephamore = signal.WaitAsync();
			}
		}

		/// <summary>
		/// Remove all notifications that was due when program was offline.
		/// </summary>
		/// <returns></returns>
		async Task Cleanup()
		{
			await LogAsync(LogLevel.Message, $"Starting notifications cleanup...");

			int found = 0;
			for (int i = 0; i < notifications.Count; i++)
			{
				Notification notification = notifications.ElementAt(i).Value;

				if (notification.Time < TimeZoneInfo.ConvertTimeToUtc(DateTime.Now))
				{
					notifications.RemoveAt(i);
					found++;
				}
			}

			if (found > 0)
			{
				await LogAsync(LogLevel.Message, $"Removed {found} notification{(found == 1 ? "" : "s")}.");
				await SaveAsync("Notifications", notifications.ToList());
			}
			else
				await LogAsync(LogLevel.Message, $"Done.");
		}

		/// <summary>
		/// Load previous saved notifications.
		/// </summary>
		/// <returns></returns>
		async Task LoadNotifications()
		{
			if (await ExistsAsync("Notifications"))
			{
				var savedNotifications = await LoadAsync<List<KeyValuePair<DateTime, Notification>>>("Notifications");

				foreach (var item in savedNotifications)
				{
					notifications.Add(item.Key, item.Value);
				}
			}
		}

		string NotificationListToString()
		{
			string output = "";

			if (notifications.Count == 0)
				output += "No notifications.";

			for (int i = 0; i < notifications.Count; i++)
			{
				Notification notification = notifications.ElementAt(i).Value;

				output += $"{i} {NotificationToString(notification)}\n";
			}

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

			DateTime time = TimeZoneInfo.ConvertTimeToUtc(notification.Time);
			return $"{time.ToString(Config.OutputTimeFormat)} to {string.Join(',', channels)} by {notification.Author} say {shorthand}\n";
		}
	}
}

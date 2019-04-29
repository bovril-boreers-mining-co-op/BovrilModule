using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;

namespace BovrilModule
{
	public partial class BovrilModule : YModule
	{
		[Example("!reminder colossal B-7DFU #general")]
		[Command("reminder", "colossal"), Summary("Create a notification for a colossal anomaly.")]
		public async Task NotifyColossal(
			[Summary("Anomaly system name.")]string system,
			[Summary("Channels notification will be sent to.")]params string[] channels)
		{
			await NotifySpecial("Colossal", new TimeSpan(5, 0, 0), system, channels);
		}

		[Example("!reminder enorm B-7DFU #general")]
		[Command("reminder", "enorm"), Summary("Create a notification for a enormous anomaly.")]
		public async Task NotifyEnormous(
			[Summary("Anomaly system name.")]string system,
			[Summary("Channels notification will be sent to.")]params string[] channels)
		{
			await NotifySpecial("Enorm", new TimeSpan(4, 0, 0), system, channels);
		}

		[IgnoreHelp]
		[Example("!reminder enormous B-7DFU #general")]
		[Command("reminder", "enormous"), Summary("Create a notification for a enormous anomaly.")]
		public async Task NotifyEnormous1(
			[Summary("Anomaly system name.")]string system,
			[Summary("Channels notification will be sent to.")]params string[] channels)
		{
			await NotifySpecial("Enorm", new TimeSpan(4, 0, 0), system, channels);
		}

		[Example("!reminder large B-7DFU #general")]
		[Command("reminder", "large"), Summary("Create a notification for a large anomaly.")]
		public async Task NotifyLarge(
			[Summary("Anomaly system name.")]string system,
			[Summary("Channels notification will be sent to.")]params string[] channels)
		{
			await NotifySpecial("Large", new TimeSpan(2, 0, 0), system, channels);
		}

		[Example("!reminder medium B-7DFU #general")]
		[Command("reminder", "medium"), Summary("Create a notification for a medium anomaly.")]
		public async Task NotifyMedium(
			[Summary("Anomaly system name.")]string system,
			[Summary("Channels notification will be sent to.")]params string[] channels)
		{
			await NotifySpecial("Medium", new TimeSpan(1, 0, 0), system, channels);
		}

		[Example("!reminder small B-7DFU #general")]
		[Command("reminder", "small"), Summary("Create a notification for a small anomaly.")]
		public async Task NotifySmall(
			[Summary("Anomaly system name.")]string system,
			[Summary("Channels notification will be sent to.")]params string[] channels)
		{
			await NotifySpecial("Small", new TimeSpan(0, 20, 0), system, channels);
		}

		async Task NotifySpecial(string anom, TimeSpan timeSpan, string system, params string[] inputs)
		{
			string formattedMessage = string.Format(Config.AnomalyMessage, anom, system);
			foreach (string channel in inputs)
			{
				SocketGuildChannel socketChannel = GetChannel<SocketGuildChannel>(channel, false);

				if (socketChannel is null)
				{

					string output = $"Error parsing input:\n";

					await RespondAsync($"```Unknown channel: '{channel}'```");
					return;
				}
			}

			DateTime spawnTime = DateTime.Now + timeSpan - new TimeSpan(0, 10, 0);
			var notification = notificationModule.GetNotification(spawnTime, new TimeSpan(0, 1, 0));

			if (notification is null || notification.Message != formattedMessage)
				await notificationModule.AddNotification(DateTime.Now + timeSpan - new TimeSpan(0, 10, 0), formattedMessage, inputs.ToList());
			else
				await RespondAsync($"That anomaly has already been registerd.");
				//await RespondAsync($"Im sorry but {notification.Author} was first.");
		}
	}
}

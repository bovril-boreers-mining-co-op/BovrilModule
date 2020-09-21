using Discord;
using Discord.WebSocket;
using LogModule.Structs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;
using YahurrFramework.Enums;

namespace Modules
{
	[Config(typeof(LogModuleConfig))]
	public class LogModule : YModule
	{
		public new LogModuleConfig Config
		{
			get
			{
				return (LogModuleConfig)base.Config;
			}
		}

		private Dictionary<string, LogChannel> ChannelID { get; }

		public LogModule()
		{
			ChannelID = new Dictionary<string, LogChannel>();
		}

		protected override Task Init()
		{
			if (Config.Channels != null)
				foreach (LogChannel channel in Config.Channels)
					ChannelID.Add(channel.Name, channel);

			return LogAsync(YahurrFramework.Enums.LogLevel.Message, $"Initializing {this.GetType().Name}...");
		}

		/*
		Debug = 0,
		Message = 1,
		Warning = 2,
		Critical = 3,
		Error = 4
		*/

		public Task LogDebug(object message, string channel, Embed embed = null)
			=> LogAsync(LogLevel.Debug, message, channel, embed);

		public Task LogMessage(object message, string channel, Embed embed = null)
			=> LogAsync(LogLevel.Message, message, channel, embed);

		public Task LogWarning(object message, string channel, Embed embed = null)
			=> LogAsync(LogLevel.Warning, message, channel, embed);

		public Task LogCritical(object message, string channel, Embed embed = null)
			=> LogAsync(LogLevel.Critical, message, channel, embed);

		public Task LogError(object message, string channel, Embed embed = null)
			=> LogAsync(LogLevel.Error, message, channel, embed);

		public Task LogDebug(object message, params string[] channels)
			=> LogAsync(LogLevel.Debug, message, channels);

		public Task LogMessage(object message, params string[] channels)
			=> LogAsync(LogLevel.Message, message, channels);

		public Task LogWarning(object message, params string[] channels)
			=> LogAsync(LogLevel.Warning, message, channels);

		public Task LogCritical(object message, params string[] channels)
			=> LogAsync(LogLevel.Critical, message, channels);

		public Task LogError(object message, params string[] channels)
			=> LogAsync(LogLevel.Error, message, channels);

		public async Task LogAsync(LogLevel logLevel, object message, params string[] channels)
		{
			foreach (string channel in channels)
				await LogAsync(logLevel, message, channel);
		}

		public async Task LogAsync(LogLevel logLevel, object message, string channel, Embed embed = null)
		{
			if (!ChannelID.TryGetValue(channel, out LogChannel logChannel) || logChannel.LogLevel < logLevel)
				return;

			ITextChannel guildChannel = await Guild.GetTextChannelAsync(logChannel.ID);
			await guildChannel.SendMessageAsync(message.ToString(), embed: embed);
			await LogAsync(logLevel, message);
		}
	}
}

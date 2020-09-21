﻿using Discord;
using Discord.WebSocket;
using NModule;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;
using EveOpenApi;
using EveOpenApi.Api;
using EveOpenApi.Interfaces;
using EveOpenApi.Authentication.Interfaces;
using EveOpenApi.Authentication;
using EveOpenApi.Interfaces.Configs;
using EveOpenApi.Api.Configs;

// Mining Ledger Bot
namespace BovrilModule
{
	[Config(typeof(BovrilConfig))]
	[RequiredModule(typeof(NotificationModule))]
	public partial class BovrilModule : YModule
	{
		public new BovrilConfig Config
		{
			get
			{
				return (BovrilConfig)base.Config;
			}
		}

		private IApiConfig ApiConfig { get; } = new SeATConfig()
		{
			UserAgent = "Bovril discord authentication;Prople Dudlestreis;henstr@hotmail.com",
			DefaultUser = "Prople Dudlestreis",
			SpecURL = "https://seat.bovrilbloodminers.org/docs/api-docs.json"
		};

		NotificationModule notificationModule;

		protected override async Task Init()
		{
			notificationModule = await GetModuleAsync<NotificationModule>();

			IKeyLogin login = new LoginBuilder().Key
				.Build();

			login.AddKey("VIYcOHK2jzJ7V54GxxKOJ59jUYezgXe8", "Prople Dudlestreis", (Scope)"");
			//login.SaveToFile("Files/SeatLogin.json", true);
		}

		protected override async Task MessageReceived(SocketMessage message)
		{
			//Remove filtered words
			foreach (var item in Config.FileterdWords)
			{
				Match match = Regex.Match(message.Content.ToLower(), item.regEx);

				if (match.Success)
				{
					await message.DeleteAsync();

					if (!string.IsNullOrEmpty(item.message))
						await RespondAsync(item.message, item.dm);
				}
			}

			//Alice o/ rage boner.
			//Match aliceMatch = Regex.Match(message.Content, "(?:o|\\\\|7).*(?:\\/|7|o)");
			//if (aliceMatch.Success && message?.Channel.Id != 264791114727424000)
			//	await RespondAsync($"{message.Author.Mention} hand slap ya pubbie, keep that shit in high sec!", false);
		}

		[Command("export", "users")]
		public async Task ExportUsers()
		{
			if (Guild == null)
				return;

			IReadOnlyCollection<IUser> users = await Guild.GetUsersAsync();
			string file = "Name\n";
			foreach (IUser user in users)
			{
				SocketGuildUser guildUser = user as SocketGuildUser;

				if (guildUser == null)
					continue;

				file += $"{(string.IsNullOrEmpty(guildUser.Nickname) ? guildUser.Username : guildUser.Nickname)}\n";
			}

			using (StreamWriter writer = File.CreateText("Files/Users.tsv"))
			{
				await writer.WriteAsync(file);
			}

			await Channel.SendFileAsync("Files/Users.tsv");
		}
	}
}

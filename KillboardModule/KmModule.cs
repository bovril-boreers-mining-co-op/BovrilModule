using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;
using EveOpenApi;
using System.IO;
using EveOpenApi.Enums;
using EveOpenApi.Api;
using EveOpenApi.Interfaces;
using EveOpenApi.Api.Configs;
using System.Collections;
using System.Linq;
using EveOpenApi.Authentication;

namespace KillboardModule
{
	[Config(typeof(KmModuleConfig))]
	public class KmModule : YModule
	{
		public new KmModuleConfig Config
		{
			get
			{
				return (KmModuleConfig)base.Config;
			}
		}

		ClientWebSocket webSocket;
		SocketGuild guild;
		IAPI esi;

		protected override async Task Init()
		{
			guild = Guild as SocketGuild;

			webSocket = new ClientWebSocket();
			await webSocket.ConnectAsync(new Uri("wss://zkillboard.com:2096"), CancellationToken.None);
			await webSocket.SendAsync(Encoding.UTF8.GetBytes("{\"action\":\"sub\",\"channel\":\"" + Config.ZkillChannel + "\"}"), WebSocketMessageType.Text, true, CancellationToken.None);

			/*
			EveLogin login;
			if (!File.Exists("Saves/BovrilModule/EveLogin.json"))
			{
				login = await EveLogin.Login("", "a72fcc9ce4424ce3848d0edaa5aebbf7", "http://localhost:8080");
				await login.SaveToFile("Saves/BovrilModule/EveLogin.json");
			}
			else
				login = await EveLogin.FromFile("Saves/BovrilModule/EveLogin.json");
				*/

			ILogin login = await new LoginBuilder()
				.WithCredentials("", "")
				.BuildEve();

			IApiConfig config = new EsiConfig()
			{
				UserAgent = "Henrik_Strocka;henstr@hotmail.com;Prople_Dudlestris",
				DefaultUser = "Prople Dudlestreis"
			};

			esi = new ApiBuilder(config, login).Build();

			Loop();
		}

		async void Loop()
		{
			while (true)
			{
				string json = "";
				while (true)
				{
					byte[] buffer = new byte[2048];
					WebSocketReceiveResult result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
					json += Encoding.UTF8.GetString(buffer);

					if (result.EndOfMessage)
						break;
				}

				KillMail km = JsonConvert.DeserializeObject<KillMail>(json);
				if (Config.TargetCorp == 0 || !km.Attackers.Exists(a => a.CorporationID == Config.TargetCorp))
				{
					Embed embed = await GetKmEmbed(km);
					await (guild.GetChannel(Config.OutputChannelID) as SocketTextChannel).SendMessageAsync(embed: embed);
				}
			}
		}

		/// <summary>
		/// Template
		/// </summary>
		/// <returns></returns>
		[Command("test")]
		public async Task Test()
		{
			EmbedBuilder embedBuilder = new EmbedBuilder();
			embedBuilder.Color = Color.Green;

			embedBuilder.AddField(FieldBuilder(
				"Victim",
					"Name: [Executex](https://zkillboard.com/character/1490079163/)\n" +
					"Corp: [Mindstar Technology](https://zkillboard.com/corporation/230445889/)\n" +
					"Alliance: [Goonswarm Federation](https://zkillboard.com/alliance/1354830081/)\n"
				));

			embedBuilder.AddField(FieldBuilder(
				"Final Blow",
					"Name: [Ishmael D'ren](https://zkillboard.com/character/1368908881/)\n" +
					"Ship: [Nyx](https://zkillboard.com/character/1490079163/)\n" +
					"Corp: [Mindstar Technology](https://zkillboard.com/ship/23913/)\n" +
					"Alliance: [Goonswarm Federation](https://zkillboard.com/alliance/1354830081/)\n"
				));

			embedBuilder.AddField(FieldBuilder(
				"Details",
					"~**Possible Awox**~\n" +
					"Time: 204,371,875.16 ISK\n" +
					"Nearest Celestial: Stargate (Q-HESZ)\n" +
					"[zKill link](https://zkillboard.com/kill/74492255/)\n"
				));

			embedBuilder.Author = new EmbedAuthorBuilder();
			embedBuilder.Author.Name = "Dominix Destroyed in 1-SMEB";
			embedBuilder.Author.Url = "https://zkillboard.com/kill/74492255/";
			embedBuilder.Author.IconUrl = "https://images-ext-1.discordapp.net/external/_ofyc9nPu8SLD3hZfxxUp5yfNcCioNblJBKP8b0ySXc/https/i.imgur.com/ZTKc3mr.png";

			embedBuilder.Footer = new EmbedFooterBuilder();
			embedBuilder.Footer.IconUrl = "https://images-ext-2.discordapp.net/external/tIPMLcxyxOp9dpdL5fs5QKS_TbmLaULx_wkRBXvEy_M/%3Fsize%3D1024/https/cdn.discordapp.com/avatars/408006849888124938/af8feff9ed35714183dbb869d60cf3dd.webp";

			embedBuilder.ThumbnailUrl = "https://imageserver.eveonline.com/Render/645_128.png";

			await Channel.SendMessageAsync(embed: embedBuilder.Build());
		}

		async Task<Embed> GetKmEmbed(KillMail killMail)
		{
			Attacker finalBlow = killMail.Attackers.Find(a => a.FinalBlow);
			DateTime killTime = DateTime.Parse(killMail.Time);

			List<object> charIDs = new List<object>()
			{
				killMail.Victim.CharacterID.ToString(),
				finalBlow.CharacterID.ToString(),
			};

			List<object> corpIDs = new List<object>()
			{
				killMail.Victim.CorporationID.ToString(),
				finalBlow.CorporationID.ToString(),
			};

			List<object> allianceIDs = new List<object>()
			{
				killMail.Victim.AllianceID.ToString(),
				finalBlow.AllianceID.ToString(),
			};

			List<object> shipIDs = new List<object>()
			{
				killMail.Victim.ShipType.ToString(),
				finalBlow.ShipType.ToString(),
			};

			IEnumerable<IApiResponse> charNames = await esi.Path("/characters/{character_id}/").GetBatch(("character_id", charIDs));
			IEnumerable<IApiResponse> corpNames = await esi.Path("/corporations/{corporation_id}/").GetBatch(("corporation_id", corpIDs));
			IEnumerable<IApiResponse> allianceNames = await esi.Path("/alliances/{alliance_id}/").GetBatch(("alliance_id", allianceIDs));
			IEnumerable<IApiResponse> shipNames = await esi.Path("/universe/types/{type_id}/").GetBatch(("type_id", shipIDs));
			IApiResponse killSystem = await esi.Path("/universe/systems/{system_id}/").Get(("system_id", killMail.Solarsystem.ToString()));

			EmbedBuilder embedBuilder = new EmbedBuilder();
			embedBuilder.Color = Color.Green;

			var charName = JsonConvert.DeserializeObject<dynamic>(charNames.ElementAt(0).FirstPage);
			var corpName = JsonConvert.DeserializeObject<dynamic>(corpNames.ElementAt(0).FirstPage);
			var allianceName = JsonConvert.DeserializeObject<dynamic>(allianceNames.ElementAt(0).FirstPage);
			embedBuilder.AddField(FieldBuilder(
				"Victim",
					$"Name: [{charName.name}]" +
						$"(https://zkillboard.com/character/{killMail.Victim.CharacterID}/)\n" +
					$"Corp: [{corpName.name}]" +
						$"(https://zkillboard.com/corporation/{killMail.Victim.CorporationID}/)\n" +
					$"Alliance: [{allianceName.name}]" +
						$"(https://zkillboard.com/alliance/{killMail.Victim.AllianceID}/)\n"
				));

			charName = JsonConvert.DeserializeObject<dynamic>(charNames.ElementAt(1).FirstPage);
			corpName = JsonConvert.DeserializeObject<dynamic>(corpNames.ElementAt(1).FirstPage);
			allianceName = JsonConvert.DeserializeObject<dynamic>(allianceNames.ElementAt(1).FirstPage);
			var shipName = JsonConvert.DeserializeObject<dynamic>(shipNames.ElementAt(1).FirstPage);
			embedBuilder.AddField(FieldBuilder(
				"Final Blow",
					$"Name: [{charName.name}]" +
						$"(https://zkillboard.com/character/{finalBlow.CharacterID}/)\n" +
					$"Ship: [{shipName.name}]" +
						$"(https://zkillboard.com/ship/{finalBlow.ShipType}/)\n" +
					$"Corp: [{corpName.name}]" +
						$"(https://zkillboard.com/corporation/{finalBlow.CorporationID}/)\n" +
					$"Alliance: [{allianceName.name}]" +
						$"(https://zkillboard.com/alliance/{finalBlow.AllianceID}/)\n"
				));

			embedBuilder.AddField(FieldBuilder(
				"Details",
					$"{(killMail.Zkb.Awox ? "~**Possible Awox * *~\n" : "")}" +
					$"Value: {killMail.Zkb.TotalValue.ToString("0,00")} ISK\n" +
					$"Time: {killTime.ToString("HH:mm")} EVE\n" +
					$"[zKill link](https://zkillboard.com/kill/{killMail.Killmail}/)\n"
				));

			shipName = JsonConvert.DeserializeObject<dynamic>(shipNames.ElementAt(0).FirstPage);
			var system = JsonConvert.DeserializeObject<dynamic>(killSystem.FirstPage);

			embedBuilder.Author = new EmbedAuthorBuilder();
			embedBuilder.Author.Name = $"{shipName.name} destroyed in {system.name}";
			embedBuilder.Author.Url = $"https://zkillboard.com/kill/{killMail.Killmail}/";
			embedBuilder.Author.IconUrl = "https://i.imgur.com/ZTKc3mr.png";

			embedBuilder.ThumbnailUrl = $"https://imageserver.eveonline.com/Render/{killMail.Victim.ShipType}_128.png";

			embedBuilder.Footer = new EmbedFooterBuilder();
			embedBuilder.Footer.IconUrl = $"https://imageserver.eveonline.com/Corporation/{finalBlow.CorporationID}_128.png";

			return embedBuilder.Build();
		}

		EmbedFieldBuilder FieldBuilder(string name, object value)
		{
			EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder();
			fieldBuilder.Name = name;
			fieldBuilder.Value = value;

			return fieldBuilder;
		}
	}
}

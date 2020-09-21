using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;

namespace Modules
{
	[Config(typeof(KillboardModuleConfig))]
	[RequiredModule(typeof(ApiModule), typeof(LogModule))]
	public class KillboardModule : YModule
	{
		public new KillboardModuleConfig Config
		{
			get
			{
				return (KillboardModuleConfig)base.Config;
			}
		}

		private ApiModule ApiModule { get; set; }

		private LogModule LogModule { get; set; }

		ClientWebSocket webSocket;
		CancellationTokenSource zkillLoopToken;

		protected override async Task Init()
		{
			await LogAsync(YahurrFramework.Enums.LogLevel.Message, $"Initializing {this.GetType().Name}...");

			ApiModule = await GetModuleAsync<ApiModule>();
			LogModule = await GetModuleAsync<LogModule>();

			webSocket = new ClientWebSocket();
			zkillLoopToken = new CancellationTokenSource();

			await LogAsync(YahurrFramework.Enums.LogLevel.Message, $"Connecting to zkill...");
			if (await TryConnectWebSocket(CancellationToken.None))
			{
				await LogAsync(YahurrFramework.Enums.LogLevel.Message, $"Starting zkill monitor loop...");
				await StartZkillLoop(zkillLoopToken.Token);
			}
			else
			{
				await LogAsync(YahurrFramework.Enums.LogLevel.Error, "Unable to connect to zkill websocket api.");
			}
		}

		#region Commands

		/// <summary>
		/// Restart the auth loop.
		/// </summary>
		/// <returns></returns>
		[Summary("Restart the auth loop.")]
		[Command("zkill", "loop", "restart")]
		public async Task AuthLoopRestart()
		{
			await RespondAsync("Restarting zkill loop...", false, false);
			zkillLoopToken.Cancel();

			zkillLoopToken = new CancellationTokenSource();
			await StartZkillLoop(zkillLoopToken.Token);
			await RespondAsync("Done.", false, false);
		}

		#endregion

		/// <summary>
		/// Start the zkill loop that will listen for new corp kills.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		Task<Task> StartZkillLoop(CancellationToken token)
		{
			return Task.Factory.StartNew(
				() => ZkillLoop(token),
				token,
				TaskCreationOptions.LongRunning,
				TaskScheduler.Default
			);
		}

		/// <summary>
		/// Loop that will listen for new corp kills on zkill.
		/// </summary>
		/// <returns></returns>
		async Task ZkillLoop(CancellationToken token)
		{
			while (true)
			{
				string json = "";
				while (true)
				{
					if (webSocket.State == WebSocketState.Aborted && !await TryConnectWebSocket(token))
					{
						await LogModule.LogError("Fatal: Unable to connect to zkill websocket api after five attempts.", "debug");
						return;
					}

					byte[] buffer = new byte[2048];
					WebSocketReceiveResult result = await webSocket.ReceiveAsync(buffer, token);
					json += Encoding.UTF8.GetString(buffer);

					if (result.EndOfMessage)
						break;
				}

				// Remove empty space, and replace it with less empty space.
				json = json.Replace((char)0x00, ' ');

				KillMail km = JsonSerializer.Deserialize<KillMail>(json);
				if ((Config.TargetCorp == 0 || km.Attackers.Exists(a => a.CorporationID == Config.TargetCorp)) && km.Attackers.Any(x => x.FinalBlow && x.CharacterID != 0))
				{
					Embed embed = await BuildKillmailEmbed(km);
					await LogModule.LogMessage("", Config.LogChannel, embed: embed);
				}
			}
		}

		/// <summary>
		/// Connect to zkill websocket api. Retry five times before fatally exit.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		async Task<bool> TryConnectWebSocket(CancellationToken token)
		{
			for (int i = 0; i < 5; i++)
			{
				await webSocket.ConnectAsync(new Uri("wss://zkillboard.com:2096"), token);
				await webSocket.SendAsync(Encoding.UTF8.GetBytes("{\"action\":\"sub\",\"channel\":\"" + Config.ZkillChannel + "\"}"), WebSocketMessageType.Text, true, token);

				if (webSocket.State == WebSocketState.Open)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Create a embed for a killmail
		/// </summary>
		/// <param name="km"></param>
		/// <returns></returns>
		async Task<Embed> BuildKillmailEmbed(KillMail km)
		{
			Attacker finalBlow = km.Attackers.Find(a => a.FinalBlow);
			DateTime killTime = DateTime.Parse(km.Time);

			List<EsiCharacter> chars = await GetCharacters(new List<object>()
			{
				km.Victim.CharacterID.ToString(),
				finalBlow.CharacterID.ToString(),
			});

			List<EsiCorporation> corps = await GetCorporations(new List<object>()
			{
				km.Victim.CorporationID.ToString(),
				finalBlow.CorporationID.ToString(),
			});

			List<EsiAlliance> alliances = await GetAlliances(corps.Select(x => x.AllianceId).Cast<object>().ToList());

			List<EsiItem> ships = await GetShips(new List<object>()
			{
				km.Victim.ShipType.ToString(),
				finalBlow.ShipType.ToString(),
			});

			EsiSystem system = (await ApiModule.Esi.Path("/universe/systems/{system_id}/").Get<EsiSystem>(("system_id", km.Solarsystem.ToString()))).FirstPage;

			return new KillmailEmbedBuilder()
				.AddVictim(chars[0].Name, (int)km.Victim.CharacterID, corps[0].Name, (int)km.Victim.CorporationID, alliances[0].Name, km.Victim.ShipType)
				.AddFinalBlow(chars[1].Name, (int)finalBlow.CharacterID, corps[1].Name, (int)finalBlow.CorporationID, alliances[1].Name, finalBlow.ShipType, ships[1].Name, finalBlow.ShipType)
				.AddDetails(km.Zkb.Awox, km.Zkb.TotalValue, killTime, km.KillmailId)
				.AddTitle(ships[0].Name, system.Name, km.KillmailId)
				.AddThumbnail(km.Victim.ShipType)
				.AddFooter((int)finalBlow.CorporationID)
				.Build();
		}

		async Task<List<EsiCharacter>> GetCharacters(List<object> characters)
		{
			return (await ApiModule.Esi.Path("/characters/{character_id}/").GetBatch<EsiCharacter>(("character_id", characters))).Select(x => x.FirstPage).ToList();
		}

		async Task<List<EsiCorporation>> GetCorporations(List<object> corporations)
		{
			return (await ApiModule.Esi.Path("/corporations/{corporation_id}/").GetBatch<EsiCorporation>(("corporation_id", corporations))).Select(x => x.FirstPage).ToList();
		}

		async Task<List<EsiAlliance>> GetAlliances(List<object> alliances)
		{
			return (await ApiModule.Esi.Path("/alliances/{alliance_id}/").GetBatch<EsiAlliance>(("alliance_id", alliances))).Select(x => x.FirstPage).ToList();
		}

		async Task<List<EsiItem>> GetShips(List<object> ships)
		{
			return (await ApiModule.Esi.Path("/universe/types/{type_id}/").GetBatch<EsiItem>(("type_id", ships))).Select(x => x.FirstPage).ToList();
		}
	}
}

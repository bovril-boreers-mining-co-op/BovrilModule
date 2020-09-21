using Discord.WebSocket;
using NModule;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using YahurrFramework;
using YahurrFramework.Attributes;
using System.Data.Common;
using Discord;
using EveOpenApi;
using EveOpenApi.Interfaces;
using EveOpenApi.Api.Configs;
using EveOpenApi.Authentication;
using EveOpenApi.Api;
using Newtonsoft.Json;
using EveOpenApi.Authentication.Interfaces;

namespace BovrilModule
{
	[Config(typeof(MoonConfig))]
	[RequiredModule(typeof(NotificationModule))]
	public class MoonModule : YModule
	{
		public new MoonConfig Config
		{
			get
			{
				return (MoonConfig)base.Config;
			}
		}

		private MoonParser moonParser { get; set; }

		private NotificationModule NotificationModule { get; set; }

		IAPI api;

		protected override async Task Init()
		{
			NotificationModule = await GetModuleAsync<NotificationModule>();
			moonParser = new MoonParser();

			IOauthLogin login = await new LoginBuilder().Eve
				.WithCredentials("a72fcc9ce4424ce3848d0edaa5aebbf7", "http://localhost:8080")
				.FromFile("Files/CalendarToken.txt")
				.Build();

			//await login.AddToken((Scope)"esi-calendar.read_calendar_events.v1");
			//login.SaveToFile("Files/CalendarToken.txt", true);

			IApiConfig apiConfig = new EsiConfig()
			{
				UserAgent = "Prople Dudlestreis;henstr@hotmail.com",
				DefaultUser = "Prople Dudlestreis"
			};
			api = new ApiBuilder(apiConfig, login).Build();
		}

		#region Commands

		[Example("!reminder moon B-7FDU 3-1 2h 5m to #general")]
		[Summary("Set notificaton for an upcomming moon, the ping will include moon stats")]
		[Command("reminder", "moon")]
		public async Task NotifyMoon(
			[Summary("System name")]string system,
			[Summary("Planet and Moon number")]string planetMoon,
			[Summary("Same input as !reminder")]params string[] input)
		{
			Notification notification;

			try
			{
				string author = (Message.Author as SocketGuildUser)?.Nickname ?? Message.Author.Username;
				notification = NotificationModule.Parse(input, author);

				if (notification.Channels == null || notification.Channels.Count == 0)
					throw new Exception("No channels specified");

				if (notification.Start == DateTime.Now)
					throw new Exception("Error parsing time");
			}
			catch (Exception e)
			{
				string output = $"Error parsing input:\n{e.Message}";

				await RespondAsync($"```{output}```");
				return;
			}

			if (!TryGetMoon(system, planetMoon, out MoonInformation moon) || moon.TotalOre < 90)
			{
				await RespondAsync("```Moon not found. If this is a public gsf moon or a private BOVRIL moon please contact Prople Dudlestreis.```");
				return;
			}

			notification.Start -= new TimeSpan(0, 10, 0);

			notification.Message = string.Format(Config.MoonMessage, moon.Name);
			notification.Message += $"```{GenerateMoonStats(moon)}```";
			await NotificationModule.AddNotification(notification);
		}

		[Example("!moon B-7FDU 3-1")]
		[Summary("Get moon contents.")]
		[Command("moon")]
		public async Task GetMoonStats(params string[] input)
		{
			SystemMoon systemMoon;
			try
			{
				systemMoon = moonParser.Parse(input);
			}
			catch (Exception e)
			{
				await RespondAsync($"```{e.Message}```");
				return;
			}

			if (TryGetMoon(systemMoon, out MoonInformation moon))
			{
				string moonStats = GenerateMoonStats(moon);

				if (!string.IsNullOrWhiteSpace(moonStats))
					await Channel.SendMessageAsync(embed: PrettyMoon(moon).Build());
				else
					await RespondAsync($"```Moon has not been scanned. If this is a public moon please contact Prople Dudlestreis.```");
			}
			else
				await RespondAsync("```Moon not found or it has not been scanned. If this is a public moon please contact Prople Dudlestreis.```");
		}

		[IgnoreHelp]
		[Command("update", "moon")]
		public async Task UpdateCalender()
		{
			await UpdateCalendarMoons();
		}

		void AddMoonStats(MoonInformation moon, EmbedBuilder embed)
		{
			if (moon.Exceptional > 0)
				embed.AddField($"Exceptional",
					$"{(moon.Monazite == 0 ? "" : Math.Round(moon.Monazite * 100, 0) + "% Monazite \n")}" +
					$"{(moon.Loparite == 0 ? "" : Math.Round(moon.Loparite * 100, 0) + "% Loparite \n")}" +
					$"{(moon.Xenotime == 0 ? "" : Math.Round(moon.Xenotime * 100, 0) + "% Xenotime \n")}" +
					$"{(moon.Ytterbite == 0 ? "" : Math.Round(moon.Ytterbite * 100, 0) + "% Ytterbite\n")}");

			if (moon.Rare > 0)
				embed.AddField($"Rare -- ",
					$"{(moon.Carnotite == 0 ? "" : Math.Round(moon.Carnotite * 100, 0) + "% Carnotite \n")}" +
					$"{(moon.Cinnabar == 0 ? "" : Math.Round(moon.Cinnabar * 100, 0) + "% Cinnabar \n")}" +
					$"{(moon.Pollucite == 0 ? "" : Math.Round(moon.Pollucite * 100, 0) + "% Pollucite \n")}" +
					$"{(moon.Zircon == 0 ? "" : Math.Round(moon.Zircon * 100, 0) + "% Zircon\n")}");

			if (moon.Uncommon > 0)
				embed.AddField($"Uncommon",
					$"{(moon.Chromite == 0 ? "" : Math.Round(moon.Chromite * 100, 0) + "% Chromite \n")}" +
					$"{(moon.Otavite == 0 ? "" : Math.Round(moon.Otavite * 100, 0) + "% Otavite \n")}" +
					$"{(moon.Sperrylite == 0 ? "" : Math.Round(moon.Sperrylite * 100, 0) + "% Sperrylite \n")}" +
					$"{(moon.Vanadinite == 0 ? "" : Math.Round(moon.Vanadinite * 100, 0) + "% Vanadinite\n")}");

			if (moon.Common > 0)
				embed.AddField($"Common",
					$"{(moon.Cobaltite == 0 ? "" : Math.Round(moon.Cobaltite * 100, 0) + "% Cobaltite \n")}" +
					$"{(moon.Euxenite == 0 ? "" : Math.Round(moon.Euxenite * 100, 0) + "% Euxenite \n")}" +
					$"{(moon.Scheelite == 0 ? "" : Math.Round(moon.Scheelite * 100, 0) + "% Scheelite \n")}" +
					$"{(moon.Titanite == 0 ? "" : Math.Round(moon.Titanite * 100, 0) + "% Titanite\n")}");

			if (moon.Ubiquitous > 0)
				embed.AddField($"Ubiquitous",
					$"{(moon.Bitumens == 0 ? "" : Math.Round(moon.Bitumens * 100, 0) + "% Bitumens \n")}" +
					$"{(moon.Coesite == 0 ? "" : Math.Round(moon.Coesite * 100, 0) + "% Coesite \n")}" +
					$"{(moon.Sylvite == 0 ? "" : Math.Round(moon.Sylvite * 100, 0) + "% Sylvite \n")}" +
					$"{(moon.Zeolites == 0 ? "" : Math.Round(moon.Zeolites * 100, 0) + "% Zeolites\n")}");

			if (moon.HighSec > 0)
				embed.AddField($"HighSec",
					$"{(moon.Veldspar == 0 ? "" : Math.Round(moon.Veldspar * 100, 0) + "% Veldspar \n")}" +
					$"{(moon.Scordite == 0 ? "" : Math.Round(moon.Scordite * 100, 0) + "% Scordite \n")}" +
					$"{(moon.Pyroxeres == 0 ? "" : Math.Round(moon.Pyroxeres * 100, 0) + "% Pyroxeres \n")}" +
					$"{(moon.Plagioclase == 0 ? "" : Math.Round(moon.Plagioclase * 100, 0) + "% Plagioclase \n")}" +
					$"{(moon.Omber == 0 ? "" : Math.Round(moon.Omber * 100, 0) + "% Omber \n")}" +
					$"{(moon.Kernite == 0 ? "" : Math.Round(moon.Kernite * 100, 0) + "% Kernite \n")}" +
					$"{(moon.Jaspet == 0 ? "" : Math.Round(moon.Jaspet * 100, 0) + "% Jaspet \n")}" +
					$"{(moon.Hemorphite == 0 ? "" : Math.Round(moon.Hemorphite * 100, 0) + "% Hemorphite \n")}" +
					$"{(moon.Hedbergite == 0 ? "" : Math.Round(moon.Hedbergite * 100, 0) + "% Hedbergite\n")}");

			if (moon.NullSec > 0)
				embed.AddField($"NullSec",
					$"{(moon.Gneiss == 0 ? "" : Math.Round(moon.Gneiss * 100, 0) + "% Gneiss \n")}" +
					$"{(moon.Ochre == 0 ? "" : Math.Round(moon.Ochre * 100, 0) + "% Ochre \n")}" +
					$"{(moon.Spodumain == 0 ? "" : Math.Round(moon.Spodumain * 100, 0) + "% Spodumain \n")}" +
					$"{(moon.Crokite == 0 ? "" : Math.Round(moon.Crokite * 100, 0) + "% Crokite \n")}" +
					$"{(moon.Bistot == 0 ? "" : Math.Round(moon.Bistot * 100, 0) + "% Bistot \n")}" +
					$"{(moon.Arkonor == 0 ? "" : Math.Round(moon.Arkonor * 100, 0) + "% Arkonor\n")}");
		}

		#endregion

		/// <summary>
		/// Try to get a moon from loaded moons list
		/// </summary>
		/// <param name="system"></param>
		/// <param name="planetMoon"></param>
		/// <param name="moon"></param>
		/// <returns></returns>
		public bool TryGetMoon(string system, string planetMoon, out MoonInformation moon)
		{
			SystemMoon systemMoon = moonParser.Parse(new string[] { system, planetMoon });
			return TryGetMoon(systemMoon, out moon);
		}

		/// <summary>
		/// Try to get a moon from loaded moons list
		/// </summary>
		/// <param name="systemMoon"></param>
		/// <param name="moon"></param>
		/// <returns></returns>
		public bool TryGetMoon(SystemMoon systemMoon, out MoonInformation moon)
		{
			moon = null;

			string getMoonData = $"SELECT c.type_name, b.quantity FROM mapdata a, moondata b, typedata c WHERE a.item_name = '{systemMoon.Name}' AND b.moon_id = a.item_id AND c.type_id = b.type_id;";
			string isTatara = $"SELECT EXISTS(SELECT * FROM moonrefinery a, mapdata b WHERE a.moon_id = b.item_id and b.item_name = '{systemMoon.Name}');";

			//string getMoonData = $"SELECT c.type_name, b.quantity, if (d.type = 'Tatara', true, false) FROM mapdata a,  moondata b, typedata c,  moonrefinery d WHERE a.item_name = '{systemMoon.Name}' AND b.moon_id = a.item_id AND d.moon_id = a.item_id AND c.type_id = b.type_id;";
			//Console.WriteLine(getMoonData);
			if (!TryRunQuery(getMoonData, out List<List<object>> result))
				return false;

			if (!TryRunQuery(isTatara, out List<List<object>> tataraResult))
				return false;

			moon = new MoonInformation(systemMoon, (long)tataraResult[0][0] == 1, result);
			return true;
		}

		/// <summary>
		/// Creates a string show what ore the moon contains
		/// </summary>
		/// <param name="moon"></param>
		/// <returns></returns>
		string GenerateMoonStats(MoonInformation moon)
		{
			string output = "";
			//output += $"{moon.System} {ToRoman(moon.Planet)} - Moon {moon.Moon}\n";

			if (moon.Exceptional > 0)
				output += $"{Math.Round(moon.Exceptional * 100, 0)}% - Exceptional -- " +
					$"{(moon.Loparite == 0 ? "" : Math.Round(moon.Loparite * 100, 0) + "% Loparite ")}" +
					$"{(moon.Monazite == 0 ? "" : Math.Round(moon.Monazite * 100, 0) + "% Monazite ")}" +
					$"{(moon.Xenotime == 0 ? "" : Math.Round(moon.Xenotime * 100, 0) + "% Xenotime ")}" +
					$"{(moon.Ytterbite == 0 ? "" : Math.Round(moon.Ytterbite * 100, 0) + "% Ytterbite")}\n";

			if (moon.Rare > 0)
				output += $"{Math.Round(moon.Rare * 100, 0)}% - Rare -- " +
					$"{(moon.Carnotite == 0 ? "" : Math.Round(moon.Carnotite * 100, 0) + "% Carnotite ")}" +
					$"{(moon.Cinnabar == 0 ? "" : Math.Round(moon.Cinnabar * 100, 0) + "% Cinnabar ")}" +
					$"{(moon.Pollucite == 0 ? "" : Math.Round(moon.Pollucite * 100, 0) + "% Pollucite ")}" +
					$"{(moon.Zircon == 0 ? "" : Math.Round(moon.Zircon * 100, 0) + "% Zircon")}\n";

			if (moon.Uncommon > 0)
				output += $"{Math.Round(moon.Uncommon * 100, 0)}% - Uncommon -- " +
					$"{(moon.Chromite == 0 ? "" : Math.Round(moon.Chromite * 100, 0) + "% Chromite ")}" +
					$"{(moon.Otavite == 0 ? "" : Math.Round(moon.Otavite * 100, 0) + "% Otavite ")}" +
					$"{(moon.Sperrylite == 0 ? "" : Math.Round(moon.Sperrylite * 100, 0) + "% Sperrylite ")}" +
					$"{(moon.Vanadinite == 0 ? "" : Math.Round(moon.Vanadinite * 100, 0) + "% Vanadinite")}\n";

			if (moon.Common > 0)
				output += $"{Math.Round(moon.Common * 100, 0)}% - Common -- " +
					$"{(moon.Cobaltite == 0 ? "" : Math.Round(moon.Cobaltite * 100, 0) + "% Cobaltite ")}" +
					$"{(moon.Euxenite == 0 ? "" : Math.Round(moon.Euxenite * 100, 0) + "% Euxenite ")}" +
					$"{(moon.Scheelite == 0 ? "" : Math.Round(moon.Scheelite * 100, 0) + "% Scheelite ")}" +
					$"{(moon.Titanite == 0 ? "" : Math.Round(moon.Titanite * 100, 0) + "% Titanite")}\n";

			if (moon.Ubiquitous > 0)
				output += $"{Math.Round(moon.Ubiquitous * 100, 0)}% - Ubiquitous -- " +
					$"{(moon.Bitumens == 0 ? "" : Math.Round(moon.Bitumens * 100, 0) + "% Bitumens ")}" +
					$"{(moon.Coesite == 0 ? "" : Math.Round(moon.Coesite * 100, 0) + "% Coesite ")}" +
					$"{(moon.Sylvite == 0 ? "" : Math.Round(moon.Sylvite * 100, 0) + "% Sylvite ")}" +
					$"{(moon.Zeolites == 0 ? "" : Math.Round(moon.Zeolites * 100, 0) + "% Zeolites")}\n";

			if (moon.HighSec > 0)
				output += $"{Math.Round(moon.HighSec * 100, 0)}% - HighSec -- " +
					$"{(moon.Veldspar == 0 ? "" : Math.Round(moon.Veldspar * 100, 0) + "% Veldspar ")}" +
					$"{(moon.Scordite == 0 ? "" : Math.Round(moon.Scordite * 100, 0) + "% Scordite ")}" +
					$"{(moon.Pyroxeres == 0 ? "" : Math.Round(moon.Pyroxeres * 100, 0) + "% Pyroxeres ")}" +
					$"{(moon.Plagioclase == 0 ? "" : Math.Round(moon.Plagioclase * 100, 0) + "% Plagioclase ")}" +
					$"{(moon.Omber == 0 ? "" : Math.Round(moon.Omber * 100, 0) + "% Omber ")}" +
					$"{(moon.Kernite == 0 ? "" : Math.Round(moon.Kernite * 100, 0) + "% Kernite ")}" +
					$"{(moon.Jaspet == 0 ? "" : Math.Round(moon.Jaspet * 100, 0) + "% Jaspet ")}" +
					$"{(moon.Hemorphite == 0 ? "" : Math.Round(moon.Hemorphite * 100, 0) + "% Hemorphite ")}" +
					$"{(moon.Hedbergite == 0 ? "" : Math.Round(moon.Hedbergite * 100, 0) + "% Hedbergite")}\n";

			if (moon.NullSec > 0)
				output += $"{Math.Round(moon.NullSec * 100, 0)}% - NullSec -- " +
					$"{(moon.Gneiss == 0 ? "" : Math.Round(moon.Gneiss * 100, 0) + "% Gneiss ")}" +
					$"{(moon.Ochre == 0 ? "" : Math.Round(moon.Ochre * 100, 0) + "% Ochre ")}" +
					$"{(moon.Spodumain == 0 ? "" : Math.Round(moon.Spodumain * 100, 0) + "% Spodumain ")}" +
					$"{(moon.Crokite == 0 ? "" : Math.Round(moon.Crokite * 100, 0) + "% Crokite ")}" +
					$"{(moon.Bistot == 0 ? "" : Math.Round(moon.Bistot * 100, 0) + "% Bistot ")}" +
					$"{(moon.Arkonor == 0 ? "" : Math.Round(moon.Arkonor * 100, 0) + "% Arkonor")}\n";

			return output;
		}

		/// <summary>
		/// Return all calendar events that is a moon pull
		/// </summary>
		/// <returns></returns>
		async Task<List<(MoonInformation, DateTime)>> GetCalendarMoons()
		{
			IApiResponse response = await api.Path("/characters/{character_id}/calendar/").Get(("character_id", 96037287));
			List<CalendarEvent> events = JsonConvert.DeserializeObject<List<CalendarEvent>>(response.FirstPage); //response.ToType<List<CalendarEvent>>().FirstPage;

			List<(MoonInformation, DateTime)> moons = new List<(MoonInformation, DateTime)>();
			foreach (CalendarEvent @event in events)
			{
				try
				{
					SystemMoon moon = moonParser.Parse(@event.Title.Split(' '));
					if (!TryGetMoon(moon, out MoonInformation moonInformation))
						break;

					DateTime dateTime = DateTime.Parse(@event.EventDate);
					moons.Add((moonInformation, dateTime));
				}
				catch (Exception)
				{

				}
			}

			return moons;
		}

		public EmbedBuilder PrettyMoon(MoonInformation moon)
		{
			//SystemMoon systemMoon = GetMoon("F-9PXR IV - Moon 4".Split(' '));
			//TryGetMoon(systemMoon, out MoonInformation moon);

			int refineryId = moon.IsTatara ? 35836 : 35835;

			EmbedBuilder builder = new EmbedBuilder();
			builder.WithAuthor($"{moon.Name}",
								"https://image.eveonline.com/Type/14_64.png",
								$"http://evemaps.dotlan.net/system/{moon.System}");
			builder.WithThumbnailUrl($"https://image.eveonline.com/Render/{refineryId}_64.png");

			switch (moon.Rarity)
			{
				case "R4":
					builder.WithColor(new Color(255, 242, 204));
					break;
				case "R8":
					builder.WithColor(new Color(255, 229, 153));
					break;
				case "R16":
					builder.WithColor(new Color(255, 217, 102));
					break;
				case "R32":
					builder.WithColor(new Color(224, 204, 204));
					break;
				case "R64":
					builder.WithColor(new Color(234, 153, 153));
					break;
				default:
					builder.WithColor(new Color(207, 226, 243));
					break;
			}

			AddMoonStats(moon, builder);
			return builder;
		}

		async Task UpdateCalendarMoons()
		{
			List<(MoonInformation, DateTime)> moons = await GetCalendarMoons();
			foreach ((MoonInformation, DateTime) moon in moons)
			{
				EmbedBuilder embed = PrettyMoon(moon.Item1);
				if (NotificationModule.GetNotification(moon.Item2 - new TimeSpan(0, 10, 0), new TimeSpan(0, 1, 0)) == null)
					await NotificationModule.AddNotification(moon.Item2 - new TimeSpan(0, 10, 0), Config.MoonPingMessage, new List<string>() { $"<#{Config.MoonPingChannelID}>" }, embed);
			}
		}

		bool TryRunQuery(string query, out List<List<object>> result)
		{
			using (MySqlConnection conn = new MySqlConnection(Config.ConnectionString))
			{
				conn.Open();

				var cmd = new MySqlCommand(query, conn);
				DbDataReader reader = cmd.ExecuteReader();

				result = new List<List<object>>();
				while (reader.Read())
				{
					List<object> row = new List<object>();
					for (int i = 0; i < reader.FieldCount; i++)
						row.Add(reader.GetValue(i));

					result.Add(row);
				}

				if (!reader.IsClosed)
					reader.Close();
			}

			return result.Count > 0;
		}
	}
}

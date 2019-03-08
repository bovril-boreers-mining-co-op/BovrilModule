﻿using CsvHelper;
using Discord.WebSocket;
using NModule;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;
using YahurrFramework;
using YahurrFramework.Attributes;
using YahurrFramework.Enums;
using System.Data;

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

		MoonParser moonParser;
		MySqlConnection dbConnection;

		NotificationModule notificationModule;

		protected override async Task Init()
		{
			notificationModule = await GetModuleAsync<NotificationModule>();
			moonParser = new MoonParser();

			await LogAsync(LogLevel.Message, "Connecting to moon database...");

			try
			{
				dbConnection = new MySqlConnection(Config.ConnectionString);
				await dbConnection.OpenAsync();
			}
			catch (Exception)
			{
			}

			int retries = 0;
			while (dbConnection.State != ConnectionState.Open && retries <= 5)
			{
				await LogAsync(LogLevel.Warning, "Unable to connect to moon database, retrying in 5 seconds...");
				await Task.Delay(5000);

				try
				{
					await dbConnection.OpenAsync();
				}
				catch (Exception)
				{
				}

				retries++;
			}

			if (dbConnection.State != ConnectionState.Open)
				throw new Exception("Unable to connect to moon db");

			await LogAsync(LogLevel.Message, "Done.");
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
				notification = notificationModule.Parse(input, author);

				if (notification.Channels == null || notification.Channels.Count == 0)
					throw new Exception("No channels specified");

				if (notification.Time == DateTime.Now)
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

			notification.Time -= new TimeSpan(0, 10, 0);

			notification.Message = string.Format(Config.MoonMessage, moon.Name);
			notification.Message += $"```{GenerateMoonStats(moon)}```";
			await notificationModule.AddNotification(notification);
		}

		[Example("!moon B-7FDU 3-1")]
		[Summary("Get moon contents.")]
		[Command("moon")]
		public async Task GetMoonStats(params string[] input)
		{
			SystemMoon systemMoon = moonParser.Parse(input);
			if (TryGetMoon(systemMoon, out MoonInformation moon))
			{
				string moonStats = GenerateMoonStats(moon);

				if (!string.IsNullOrWhiteSpace(moonStats))
					await RespondAsync($"\n**{moon.Name}:**\n" +
										$"```" +
										$"{moonStats}" +
										$"```");
				else
					await RespondAsync($"```Moon has not been scanned.```");
			}
			else
				await RespondAsync("```Moon not found or it has not been scanned. If this is a public moon please contact Prople Dudlestreis.```");
		}

		[IgnoreHelp]
		[Command("parse", "moon")]
		public async Task ParseMoon(params string[] input)
		{
			try
			{
				SystemMoon moon = moonParser.Parse(input);

				await RespondAsync(moon.Name);
			}
			catch (Exception e)
			{
				await RespondAsync(e.Message);
			}
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
			string getMoonID = $"select * from mapdenormalize where " +
								$"itemName = '{systemMoon.Name}';";

			moon = null;
			//if (!TryRunQuery(getMoonID, out List<object> infoList))
				//return false;

			//string getMoonData = $"select * from moondata where MoonID = {infoList[0]}";
			string getMoonData = $"select * from PublicMoonData where GameProper = '{systemMoon.Name}';";
			if (!TryRunQuery(getMoonData, out List<object> dataList))
				return false;

			moon = new MoonInformation(systemMoon, dataList);
			return !(moon is null);
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
		/// Converts 08 to 8
		/// </summary>
		/// <param name="planetMoon"></param>
		/// <returns></returns>
		string SimplifyPlanetMoon(string planetMoon)
		{
			int separatorIndex = planetMoon.IndexOf('-');

			if (separatorIndex == -1)
				return planetMoon;

			int.TryParse(planetMoon.Substring(0, separatorIndex), out int planet);
			int.TryParse(planetMoon.Substring(separatorIndex + 1), out int moon);

			return $"{planet}-{moon}";
		}

		/// <summary>
		/// https://stackoverflow.com/questions/7040289/converting-integers-to-roman-numerals
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public static string ToRoman(int number)
		{
			if ((number < 0) || (number > 3999)) throw new ArgumentOutOfRangeException("insert value betwheen 1 and 3999");
			if (number < 1) return string.Empty;
			if (number >= 1000) return "M" + ToRoman(number - 1000);
			if (number >= 900) return "CM" + ToRoman(number - 900);
			if (number >= 500) return "D" + ToRoman(number - 500);
			if (number >= 400) return "CD" + ToRoman(number - 400);
			if (number >= 100) return "C" + ToRoman(number - 100);
			if (number >= 90) return "XC" + ToRoman(number - 90);
			if (number >= 50) return "L" + ToRoman(number - 50);
			if (number >= 40) return "XL" + ToRoman(number - 40);
			if (number >= 10) return "X" + ToRoman(number - 10);
			if (number >= 9) return "IX" + ToRoman(number - 9);
			if (number >= 5) return "V" + ToRoman(number - 5);
			if (number >= 4) return "IV" + ToRoman(number - 4);
			if (number >= 1) return "I" + ToRoman(number - 1);
			throw new ArgumentOutOfRangeException("something bad happened");
		}

		bool TryRunQuery(string query, out List<object> rows)
		{
			rows = new List<object>();
			var cmd = new MySqlCommand(query, dbConnection);
			var reader = cmd.ExecuteReader();

			if (!reader.HasRows)
			{
				reader.Close();
				return false;
			}

			reader.Read();
			for (int i = 0; i < reader.FieldCount; i++)
				rows.Add(reader[i]);

			reader.Close();
			return true;
		}
	}
}
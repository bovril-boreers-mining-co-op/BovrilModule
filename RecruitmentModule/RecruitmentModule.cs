using Discord;
using Discord.WebSocket;
using EveOpenApi;
using EveOpenApi.Api;
using EveOpenApi.Enums;
using MySql.Data.MySqlClient;
using NModule;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;
using YahurrFramework.Enums;

namespace RecruitmentModule
{
	[Config(typeof(RecruitmentConfig))]
	[RequiredModule(typeof(JobQueueModule))]
	public class RecruitmentModule : YModule
	{
		public new RecruitmentConfig Config
		{
			get
			{
				return (RecruitmentConfig)base.Config;
			}
		}

		private JobQueueModule JobQueueModule { get; set; }

		private ApiConfig ApiConfig { get; } = new ApiConfig()
		{
			UserAgent = "Bovril discord authentication;Prople Dudlestreis;henstr@hotmail.com",
			DefaultUser = "Prople Dudlestreis",
		};

		private API ESI { get; set; }

		IGuild guild;

		protected override async Task Init()
		{
			if (!File.Exists("Files/EveLogin.json"))
				throw new FileNotFoundException("Eve login file not provided.");

			await LogAsync(LogLevel.Message, "Starting server...");
			await Task.Factory.StartNew(
				SocketServer,
				CancellationToken.None,
				TaskCreationOptions.LongRunning,
				TaskScheduler.Default
			);
			await LogAsync(LogLevel.Message, "Done.");

			guild = Guild;

			EveLogin login = await EveLogin.FromFile("Files/EveLogin.json");
			ESI = API.CreateEsi(EsiVersion.Latest, Datasource.Tranquility, login, config: ApiConfig);
			JobQueueModule = await GetModuleAsync<JobQueueModule>();
		}

		protected override async Task Done()
		{
			JobQueueModule.RegisterJob<NameCheckerJob>(ProcessJob);

			if (JobQueueModule.GetJob<NameCheckerJob>(_ => true) == null)
				await JobQueueModule.AddJob(new NameCheckerJob("RecruitmentModule", GetNameJobStart()));
		}

		protected override async Task UserJoined(SocketGuildUser user)
		{
			ITextChannel channel = (ITextChannel)user.Guild.GetChannel(Config.LogChannel);
			await channel.SendMessageAsync($"{user.Username} has joined the server.");

			if (string.IsNullOrEmpty(Config.WelcomeMessage))
				await user.SendMessageAsync(string.Format(Config.WelcomeMessage, user.Username));
		}

		DateTime GetNameJobStart()
		{
			DateTime time = DateTime.Today;

			while (time < DateTime.UtcNow)
				time += new TimeSpan(Config.NameCheckInterval, 0, 0);

			return time;
		}

		async Task ProcessJob(NameCheckerJob job)
		{
			await UpdateNames();

			// Requeue the command to ensure loop.
			await JobQueueModule.AddJob(new NameCheckerJob("RecruitmentModule", GetNameJobStart()));
		}

		async Task SocketServer()
		{
			IPAddress ipAdress = IPAddress.Parse("127.0.0.1");
			IPEndPoint localEndPoint = new IPEndPoint(ipAdress, 55766);

			Socket listener = new Socket(ipAdress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			listener.Bind(localEndPoint);
			listener.Listen(100);

			while (true)
			{
				Socket socket = await listener.AcceptAsync();
				string response = await ReveiceMessage(socket);

				await ProcessRequest(response);
			}
		}

		async Task<string> ReveiceMessage(Socket socket)
		{
			using (NetworkStream stream = new NetworkStream(socket))
			using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, 1024))
			{
				return await reader.ReadToEndAsync();
			}
		}

		[IgnoreHelp]
		[Command("ttnamecheck")]
		public async Task TestCommand()
		{
			await UpdateNames();
		}

		[IgnoreHelp]
		[Command("tpipecheck")]
		public async Task PipeCheck()
		{
			IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Loopback, 55766);

			Socket client = new Socket(IPAddress.Loopback.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			await client.ConnectAsync(localEndPoint);

			Memory<byte> data = Encoding.ASCII.GetBytes("namecheck");

			await client.SendAsync(data, SocketFlags.None);
			client.Shutdown(SocketShutdown.Both);
			client.Close();
		}

		async Task UpdateNames()
		{
			//await LogMessage("Starting name update...");
			ApiPath path = ESI.Path("/corporations/{corporation_id}/members/");
			ApiResponse respose = await path.Get(("corporation_id", 98270640));
			List<string> corpUsers = respose.ToType<List<string>>().Response;

			Dictionary<ulong, ulong> dbUser = await GetDbUsers();
			foreach (IGuildUser user in await guild.GetUsersAsync())
			{
				bool userAuthed = dbUser.TryGetValue(user.Id, out ulong eveId);
				if (userAuthed)
					await VerifyName(user, eveId);

				await VerifyRole(user, guild.GetRole(Config.AuthedRole), userAuthed);

				// Check if user has correct corp roles.
				bool corpStatus = corpUsers.Contains(eveId.ToString());
				if (userAuthed)
					await VerifyRole(user, guild.GetRole(Config.CorpRole), corpStatus);
			}
			//await LogMessage("Done.");
		}

		/// <summary>
		/// Retrive all eve id's from the database.
		/// </summary>
		/// <returns></returns>
		async Task<Dictionary<ulong, ulong>> GetDbUsers()
		{
			Dictionary<ulong, ulong> dbUser = new Dictionary<ulong, ulong>();
			using (MySqlConnection conn = new MySqlConnection(Config.ConnectionString))
			{
				conn.Open();

				MySqlCommand cmd = new MySqlCommand("select * from users;", conn);
				DbDataReader reader = await cmd.ExecuteReaderAsync();

				while (await reader.ReadAsync())
				{
					ulong eveID = ulong.Parse(reader["eve_id"].ToString());
					ulong discordID = ulong.Parse(reader["discord_id"].ToString());

					dbUser.Add(discordID, eveID);
				}

				if (!reader.IsClosed)
					reader.Close();
			}

			return dbUser;
		}

		/// <summary>
		/// Verify that a discord users nickname is the same as his EVE name. Change it if there is a mismatch.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="eveId"></param>
		/// <returns></returns>
		async Task VerifyName(IGuildUser user, ulong eveId)
		{
			ApiResponse response = await ESI.Path("/characters/{character_id}/").Get(("character_id", eveId));

			if (response is ApiError)
				return;

			ApiResponse<dynamic> casted = response.ToType<dynamic>();

			string userName = string.IsNullOrEmpty(user.Nickname) ? user.Username : user.Nickname;
			string eveName = casted.Response["name"].Value.ToString();

			if (userName != eveName)
			{
				await user.ModifyAsync(x => x.Nickname = eveName);

				ITextChannel channel = (ITextChannel)await Guild.GetChannelAsync(Config.LogChannel);
				await channel.SendMessageAsync($"Updated nickname for {userName} to {eveName}");
			}
		}

		async Task VerifyRole(IGuildUser user, IRole role, bool state)
		{
			bool hasRole = user.RoleIds.Any(a => a == role.Id);

			if (state && !hasRole)
				await AddRole(user, role);
			else if (!state && hasRole)
				await RemoveRole(user, role);
		}

		async Task RemoveRole(IGuildUser user, IRole role)
		{
			await user.RemoveRoleAsync(role);

			string userName = string.IsNullOrEmpty(user.Nickname) ? user.Username : user.Nickname;
			await LogMessage($"{userName} has been removed from the following roles: {role.Name}");
		}

		async Task AddRole(IGuildUser user, IRole role)
		{
			await user.AddRoleAsync(role);

			string userName = string.IsNullOrEmpty(user.Nickname) ? user.Username : user.Nickname;
			await LogMessage($"{userName} has been added to the role {role.Name}");
		}

		async Task LogMessage(string message)
		{
			ITextChannel channel = (ITextChannel)await guild.GetChannelAsync(Config.LogChannel);
			await channel.SendMessageAsync(message);
		}

		async Task ProcessRequest(string content)
		{
			content = content.ToLower();
			switch (content)
			{
				case "namecheck":
					await UpdateNames();
					break;
				default:
					break;
			}
		}
	}
}

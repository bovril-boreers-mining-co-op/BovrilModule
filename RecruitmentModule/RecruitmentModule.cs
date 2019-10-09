using Discord;
using Discord.Rest;
using Discord.WebSocket;
using EveOpenApi;
using EveOpenApi.Api;
using EveOpenApi.Api.Configs;
using EveOpenApi.Authentication;
using EveOpenApi.Enums;
using EveOpenApi.Interfaces;
using MySql.Data.MySqlClient;
using NModule;
using Renci.SshNet.Security.Cryptography;
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

		private IAPI ESI { get; set; }

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

			ILogin login = await new LoginBuilder()
				.WithCredentials("6715ab2423344bb396a0629e0703e75c", "http://localhost:8080")
				.FromFile("Files/CorpMembersToken.txt")
				.BuildEve();

			IApiConfig apiConfig = new EsiConfig()
			{
				UserAgent = "Prople Dudlestreis;henstr@hotmail.com",
				DefaultUser = "Prople Dudlestreis"
			};
			ESI = new ApiBuilder(apiConfig, login).Build();
			/*EveLogin login = await EveLogin.FromFile("Files/EveLogin.json");
			ESI = API.CreateEsi(EsiVersion.Latest, Datasource.Tranquility, login, config: ApiConfig);*/
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

		/// <summary>
		/// Check all names
		/// </summary>
		/// <param name="job"></param>
		/// <returns></returns>
		async Task ProcessJob(NameCheckerJob job)
		{
			// Failsafe
			try
			{
				await VerifyUsers();
				await StoreAuditLog();
			}
			catch (Exception e)
			{
				//await LogMessage($"Recruitment loop failed with exception: {e.Message}");
			}

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
		[Command("namecheck")]
		public async Task NameCheck()
		{
			await RespondAsync("Starting user verification process please wait.", false, false);
			await VerifyUsers();
			await RespondAsync("User verification process complete.", false, false);
		}

		[IgnoreHelp]
		[Command("audit", "store")]
		public async Task TestCommand()
		{
			await RespondAsync("Storing audit log.", false, false);
			await StoreAuditLog();
			await RespondAsync("Done.", false, false);
		}

		[IgnoreHelp]
		[Command("pipecheck")]
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

		/// <summary>
		/// Verify user roles
		/// </summary>
		/// <returns></returns>
		async Task VerifyUsers()
		{
			//await LogMessage("Starting name update...");
			IApiPath path = ESI.Path("/corporations/{corporation_id}/members/");
			IApiResponse respose = await path.Get(("corporation_id", 98270640));
			List<string> corpUsers = respose.ToType<List<string>>().FirstPage;

			Dictionary<ulong, ulong> dbUser = await GetAuthedUsers();
			foreach (IGuildUser user in await guild.GetUsersAsync())
			{
				bool userAuthed = dbUser.TryGetValue(user.Id, out ulong eveId);
				if (userAuthed)
					await VerifyName(user, eveId);

				await VerifyRole(user, guild.GetRole(Config.AuthedRole), userAuthed);

				// Check if user has correct corp roles.
				bool corpStatus = corpUsers.Contains(eveId.ToString());

				// Manual overide for tablot our beloved CEO
				if (user.Id == 164887181607960576)
					corpStatus = true;

				await VerifyRole(user, guild.GetRole(Config.CorpRole), corpStatus);
			}
		}

		async Task StoreAuditLog()
		{
			IEnumerable<IAuditLogEntry> auditLog = await guild.GetAuditLogsAsync();
			foreach (IAuditLogEntry entry in auditLog)
			{
				await StoreAuditEntry(entry);
			}
		}

		/// <summary>
		/// Retrive all eve id's from the database.
		/// </summary>
		/// <returns></returns>
		async Task<Dictionary<ulong, ulong>> GetAuthedUsers()
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

		async Task StoreAuditEntry(IAuditLogEntry entry)
		{
			using (MySqlConnection conn = new MySqlConnection(Config.AuditConnectionString))
			{
				conn.Open();

				MySqlCommand exists = new MySqlCommand($"select exists(select * from discord where id = {entry.Id});", conn);
				object entryExists = await exists.ExecuteScalarAsync();

				if ((long)entryExists != 1)
				{
					object data = GetAuditData(entry.Data);
					string query = @$"insert into discord values ({entry.Id},{entry.User.Id},'{entry.CreatedAt.ToString("yyyy-MM-dd hh:mm:ss")}',{(int)entry.Action},'{entry.Reason}','{data}');";
					MySqlCommand cmd = new MySqlCommand(query, conn); //I really hope alice wont try to do an sql injection
					await cmd.ExecuteNonQueryAsync();
				}
			}
		}

		/// <summary>
		/// Verify that a discord users nickname is the same as his EVE name. Change it if there is a mismatch.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="eveId"></param>
		/// <returns></returns>
		async Task VerifyName(IGuildUser user, ulong eveId)
		{
			IApiResponse response = await ESI.Path("/characters/{character_id}/").Get(("character_id", eveId));

			if (response is ApiError)
				return;

			IApiResponse<dynamic> casted = response.ToType<dynamic>();

			string userName = string.IsNullOrEmpty(user.Nickname) ? user.Username : user.Nickname;
			string eveName = casted.FirstPage["name"].Value.ToString();

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
					await VerifyUsers();
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Convert to switch when C# 8.0 is out
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		object GetAuditData(IAuditLogData data)
		{
			if (data.GetType() == typeof(BanAuditLogData))
				return (data as BanAuditLogData).Target.Id;
			if (data.GetType() == typeof(ChannelCreateAuditLogData))
				return (data as ChannelCreateAuditLogData).ChannelId;
			if (data.GetType() == typeof(ChannelDeleteAuditLogData))
				return (data as ChannelDeleteAuditLogData).ChannelId;
			if (data.GetType() == typeof(ChannelUpdateAuditLogData))
				return (data as ChannelUpdateAuditLogData).ChannelId;
			if (data.GetType() == typeof(EmoteCreateAuditLogData))
				return (data as EmoteCreateAuditLogData).EmoteId;
			if (data.GetType() == typeof(EmoteDeleteAuditLogData))
				return (data as EmoteDeleteAuditLogData).EmoteId;
			if (data.GetType() == typeof(KickAuditLogData))
				return (data as KickAuditLogData).Target.Id;
			if (data.GetType() == typeof(MemberRoleAuditLogData))
				return (data as MemberRoleAuditLogData).Target.Id;
			if (data.GetType() == typeof(MemberUpdateAuditLogData))
				return (data as MemberUpdateAuditLogData).Target.Id;
			if (data.GetType() == typeof(PruneAuditLogData))
				return (data as PruneAuditLogData).MembersRemoved;
			if (data.GetType() == typeof(RoleCreateAuditLogData))
				return (data as RoleCreateAuditLogData).RoleId;
			if (data.GetType() == typeof(RoleDeleteAuditLogData))
				return (data as RoleDeleteAuditLogData).RoleId;
			if (data.GetType() == typeof(RoleUpdateAuditLogData))
				return (data as RoleUpdateAuditLogData).RoleId;
			if (data.GetType() == typeof(UnbanAuditLogData))
				return (data as UnbanAuditLogData).Target.Id;

			return null;
		}
	}
}

using Discord;
using Discord.WebSocket;
using EveOpenApi.Api;
using Modules.Jobs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;
using YahurrFramework.Enums;

namespace Modules
{
	[Config(typeof(RecruitmentModuleConfig))]
	[RequiredModule(typeof(LogModule), typeof(RoleModule), typeof(DatabaseModule), typeof(ApiModule), typeof(JobModule))]
	public class RecruitmentModule : YModule
	{
		public new RecruitmentModuleConfig Config
		{
			get
			{
				return (RecruitmentModuleConfig)base.Config;
			}
		}

		private LogModule LogModule { get; set; }

		private RoleModule RoleModule { get; set; }

		private DatabaseModule DatabaseModule { get; set; }

		private ApiModule ApiModule { get; set; }

		private JobModule JobModule { get; set; }

		CancellationTokenSource authLoopToken;

		protected override async Task Init()
		{
			await LogAsync(LogLevel.Message, $"Initializing {this.GetType().Name}...");

			authLoopToken = new CancellationTokenSource();

			LogModule = await GetModuleAsync<LogModule>();
			RoleModule = await GetModuleAsync<RoleModule>();
			DatabaseModule = await GetModuleAsync<DatabaseModule>();
			ApiModule = await GetModuleAsync<ApiModule>();
			JobModule = await GetModuleAsync<JobModule>();

			await JobModule.RegisterJobAsync<CheckupJob>(CheckupJobMethod);

			if (Config.NameVerificationLoop)
			{
				await JobModule.RegisterJobAsync<VerifyJob>(VerifyJobMethod);
				if (!await JobModule.JobExistsAsync<VerifyJob>(x => x.Creator == "RecruitmentModule"))
					await JobModule.AddJobAsync(new VerifyJob("RecruitmentModule", new TimeSpan(Config.NameVerificationInterval, 0, 0)), RoundDateTime(DateTime.UtcNow));
			}

			await LogAsync(LogLevel.Message, "Starting auth loop...");
			await StartSocketServer(authLoopToken.Token);
		}

		protected override async Task UserJoined(SocketGuildUser user)
		{
			SetContext(new MethodContext(user.Guild, null, null));
			if (!string.IsNullOrEmpty(Config.WelcomeMessage))
				await user.SendMessageAsync(Config.WelcomeMessage);

			await LogModule.LogMessage($"{user.Username} has joined the server.", "recruitment");
		}

		protected override Task UserLeft(SocketGuildUser user)
		{
			SetContext(new MethodContext(user.Guild, null, null));
			return LogModule.LogMessage($"{user.Username} has left the server.", "recruitment");
		}

		#region Commands

		/// <summary>
		/// Verify recreuitment roles for all users.
		/// </summary>
		/// <returns></returns>
		[Command("namecheck")]
		public async Task Namecheck()
		{
			await RespondAsync("Starting namecheck....", false, false);
			await VerifyUsers(Guild);
			await RespondAsync("Done.", false, false);
		}

		/// <summary>
		/// Manually auth a user
		/// </summary>
		/// <returns></returns>
		[Summary("Manually auth a user")]
		[Command("auth")]
		public async Task Auth(
			[Summary("Discord user ID, Enable developer mode and rick click user.")]ulong discordId,
			[Summary("Eve user ID, Go to thei ZKill page and copy the ID from the URL.")]int eveId)
		{
			IGuildUser user = await Guild.GetUserAsync(discordId);
			Dictionary<ulong, int> authedUsers = await GetAuthedUsers();

			if (authedUsers.TryGetValue(discordId, out int authedChar))
			{
				await RespondAsync($"User already authed to {authedChar}, if bot does not update roles run !namecheck or !auth loop restart. If that does not work pray Prople is online.", false, false);
				return;
			}

			string name = string.IsNullOrEmpty(user.Nickname) ? user.Username : user.Nickname;
			await RespondAsync($"Authenticating {name}...", false, false);
			await DatabaseModule.RunQueryAsync(Config.RecruitmentDatabase, $"INSERT INTO users VALUES ({eveId},{discordId})");
			await RespondAsync("Done, run !namecheck to update roles.", false, false);
		}

		/// <summary>
		/// Manually verify a user
		/// </summary>
		/// <returns></returns>
		[Summary("Manually verify a user")]
		[Command("verify")]
		public async Task Verify(
			[Summary("Discord user ID, Enable developer mode and rick click user.")] ulong discordId)
		{
			IGuildUser user = await Guild.GetUserAsync(discordId);

			await RespondAsync("Starting user verification....", false, false);
			await VerifyUser(Guild, user);
			await RespondAsync("Done.", false, false);
		}

		/// <summary>
		/// Check if a character is authed
		/// </summary>
		/// <param name="discordId"></param>
		/// <returns></returns>
		[Summary("Check if a user is registerd in our DB.")]
		[Command("authed")]
		public async Task IsAuthed(
			[Summary("Discord user ID, Enable developer mode and rick click user.")] ulong discordId)
		{
			IGuildUser user = await Guild.GetUserAsync(discordId);
			Dictionary<ulong, int> authedUsers = await GetAuthedUsers();
			bool userAuthed = authedUsers.TryGetValue(discordId, out _);

			string name = string.IsNullOrEmpty(user.Nickname) ? user.Username : user.Nickname;
			await RespondAsync($"{name} is {(userAuthed ? "" : "not ")}authed.", false, false);
		}

		/// <summary>
		/// See who owns a character and all his alts.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		[Summary("See who owns a character and all his alts.")]
		[Command("main")]
		public async Task MainCheck(params string[] user)
		{
			IGuildUser guildUser = GetUser(string.Join(' ', user), true);
			Dictionary<ulong, int> authedUsers = await GetAuthedUsers();
			
			if (!authedUsers.TryGetValue(guildUser.Id, out int eveID))
			{
				await RespondAsync($"{guildUser} is not authed", false, false);
				return;
			}

			IApiResponse<SeatData<SeatUser>> seatUser = await ApiModule.Seat.Path("/users/{user_id}").Get<SeatData<SeatUser>>(("user_id", eveID));
			IApiResponse<SeatData<SeatGroup>> seatGroup = await ApiModule.Seat.Path("/users/groups/{group_id}").Get<SeatData<SeatGroup>>(("group_id", seatUser.FirstPage.Data.GroupID));

			StringBuilder reply = new StringBuilder();
			SeatGroupUser main = seatGroup.FirstPage.Data.Users.Single(x => x.CharacterID == long.Parse(seatGroup.FirstPage.Data.MainCharacter));
			reply.Append($"{main.Name}:\n");

			foreach (SeatGroupUser groupUser in seatGroup.FirstPage.Data.Users)
			{
				if (groupUser.CharacterID == main.CharacterID)
					continue;

				reply.Append($"	{groupUser.Name}\n");
			}

			await RespondAsync($"```{reply}```", false, false);
		}

		/// <summary>
		/// Restart the auth loop.
		/// </summary>
		/// <returns></returns>
		[Summary("Restart the auth loop.")]
		[Command("auth", "loop", "restart")]
		public async Task AuthLoopRestart()
		{
			await RespondAsync("Restarting auth loop...", false, false);
			authLoopToken.Cancel();

			authLoopToken = new CancellationTokenSource();
			await StartSocketServer(authLoopToken.Token);
			await RespondAsync("Done.", false, false);
		}

		/// <summary>
		/// Get how many days the character has been in corp.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		[Summary("Get how mant days a character has been in corp.")]
		[Command("days")]
		public async Task GetDaysInCorp(params string[] user)
		{
			IGuildUser guildUser = GetUser(string.Join(' ', user), true);
			Dictionary<ulong, int> authedUsers = await GetAuthedUsers();

			if (!authedUsers.TryGetValue(guildUser.Id, out int eveID))
			{
				await RespondAsync($"{guildUser} is not authed", false, false);
				return;
			}

			await RespondAsync($"{await GetDaysInCorp(eveID)}", false, false);
		}

		/// <summary>
		/// Check if the bot can find a user in current guild.
		/// </summary>
		/// <param name="discordId"></param>
		/// <returns></returns>
		[Summary("Check if the bot can find a user in current guild.")]
		[Command("find", "user")]
		public async Task FindUser(ulong discordId)
		{
			foreach (IGuildUser user in await Guild.GetUsersAsync())
			{
				if (user.Id == discordId)
				{
					await RespondAsync("User found in server.", false, false);
					return;
				}
			}

			await RespondAsync("User not found in server.", false, false);
		}

		/// <summary>
		/// Export discord users as TSV
		/// </summary>
		/// <returns></returns>
		[Command("export", "users")]
		public async Task ExportUsers()
		{
			if (Guild == null)
				return;

			StringBuilder reply = new StringBuilder("Name\n");
			IReadOnlyCollection<IUser> users = await Guild.GetUsersAsync();
			foreach (IUser user in users)
			{
				SocketGuildUser guildUser = user as SocketGuildUser;

				if (guildUser == null)
					continue;

				reply.Append($"{(string.IsNullOrEmpty(guildUser.Nickname) ? guildUser.Username : guildUser.Nickname)}\n");
			}

			using (StreamWriter writer = File.CreateText("Files/Users.tsv"))
			{
				await writer.WriteAsync(reply);
			}

			await Channel.SendFileAsync("Files/Users.tsv");
			File.Delete("Files/Users.tsv");
		}

		#endregion

		/// <summary>
		/// Start the authentication loop that communitcated with the auth website.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		Task<Task> StartSocketServer(CancellationToken token)
		{
			return Task.Factory.StartNew(
				() => SocketServer(Guild),
				token,
				TaskCreationOptions.LongRunning,
				TaskScheduler.Default
			);
		}

		/// <summary>
		/// Loop for receiving commands from the bovril auth website.
		/// </summary>
		/// <returns></returns>
		async Task SocketServer(IGuild guild)
		{
			// Website is hosted on he same network.
			IPAddress ipAdress = IPAddress.Parse("127.0.0.1");
			IPEndPoint localEndPoint = new IPEndPoint(ipAdress, 55766);

			Socket listener = new Socket(ipAdress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			listener.Bind(localEndPoint);
			listener.Listen(100);

			while (true)
			{
				Socket socket = await listener.AcceptAsync();
				string command = await ReveiceMessage(socket);

				await LogModule.LogMessage($"Received command '{command}' from auth server.", "debug");
				await ProcessRequest(command, guild);
			}
		}

		/// <summary>
		/// Get message sent over socket and convert it to a UTF-8 string.
		/// </summary>
		/// <param name="socket"></param>
		/// <returns></returns>
		async Task<string> ReveiceMessage(Socket socket)
		{
			using (NetworkStream stream = new NetworkStream(socket))
			using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, 1024))
			{
				return await reader.ReadToEndAsync();
			}
		}

		/// <summary>
		/// Potential for more commands later down the line.
		/// </summary>
		/// <param name="content"></param>
		/// <returns></returns>
		async Task ProcessRequest(string content, IGuild guild)
		{
			content = content.ToLower();
			switch (content)
			{
				case "namecheck":
					await VerifyUsers(guild);
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Verify user roles
		/// </summary>
		/// <returns></returns>
		async Task VerifyUsers(IGuild guild)
		{
			IRole authedRole = guild.GetRole(Config.AuthedRole);

			Dictionary<ulong, int> dbUsers = await GetAuthedUsers();
			foreach (IGuildUser user in await guild.GetUsersAsync())
			{
				bool userAuthed = dbUsers.TryGetValue(user.Id, out int eveId);
				bool hasRole = await RoleModule.HasRoleAsync(user, authedRole.Name);

				await UpdateRole(user, authedRole, userAuthed, hasRole);

				if (userAuthed)
				{
					await VerifyName(user, eveId);

					await VerifyAlliance(user, guild.GetRole(Config.AllianceRole), eveId);
					await VerifyCorp(user, guild.GetRole(Config.CorpRole), guild.GetRole(Config.LegacyRole), eveId);
				}
			}

			await RoleModule.VerifyRoles();
		}

		/// <summary>
		/// Verify a specific user.
		/// </summary>
		/// <param name="guild"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		async Task VerifyUser(IGuild guild, IGuildUser user)
		{
			IRole authedRole = guild.GetRole(Config.AuthedRole);
			Dictionary<ulong, int> dbUsers = await GetAuthedUsers();

			bool userAuthed = dbUsers.TryGetValue(user.Id, out int eveId);
			bool hasRole = await RoleModule.HasRoleAsync(user, authedRole.Name);

			await UpdateRole(user, authedRole, userAuthed, hasRole);

			if (userAuthed)
			{
				await VerifyName(user, eveId);

				await VerifyAlliance(user, guild.GetRole(Config.AllianceRole), eveId);
				await VerifyCorp(user, guild.GetRole(Config.CorpRole), guild.GetRole(Config.LegacyRole), eveId);
			}
		}

		/// <summary>
		/// Retrive all eve id's from the database.
		/// </summary>
		/// <returns></returns>
		async Task<Dictionary<ulong, int>> GetAuthedUsers()
		{
			Dictionary<ulong, int> dbUser = new Dictionary<ulong, int>();
			List<DatabaseRow> rows = await DatabaseModule.RunQueryAsync(Config.RecruitmentDatabase, "select * from users;");
			foreach (DatabaseRow row in rows)
				dbUser.Add((ulong)row.GetData<long>(1), row.GetData<int>(0));

			return dbUser;
		}

		/// <summary>
		/// Verify that a discord users nickname is the same as his EVE name. Change it if there is a mismatch.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="eveId"></param>
		/// <returns></returns>
		async Task VerifyName(IGuildUser user, int eveId)
		{
			IApiResponse response = await ApiModule.Esi.Path("/characters/{character_id}/").Get(("character_id", eveId));

			if (response is ApiError)
				return;

			dynamic casted = JsonConvert.DeserializeObject<dynamic>(response.FirstPage);

			string userName = string.IsNullOrEmpty(user.Nickname) ? user.Username : user.Nickname;
			string eveName = casted["name"].Value.ToString();

			if (userName != eveName)
			{
				await user.ModifyAsync(x => x.Nickname = eveName);
				await LogModule.LogMessage($"Updated nickname for {userName} to {eveName}", Config.LogChannel);
			}
		}

		/// <summary>
		/// Verify user alliance role.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="allianceRole"></param>
		/// <param name="eveId"></param>
		/// <returns></returns>
		async Task VerifyAlliance(IGuildUser user, IRole allianceRole, int eveId)
		{
			IApiResponse characterInfo = await ApiModule.Esi.Path("/characters/{character_id}/").Get(("character_id", eveId));
			int corpID = JsonConvert.DeserializeObject<dynamic>(characterInfo.FirstPage)["corporation_id"];

			IApiResponse allianceCorpsResponse = await ApiModule.Esi.Path("/alliances/{alliance_id}/corporations/").Get(("alliance_id", Config.AllianceID));
			List<int> allianceCorps = allianceCorpsResponse.ToType<List<int>>().FirstPage;

			bool inAlliance = allianceCorps.Contains(corpID);
			bool hasRole = await RoleModule.HasRoleAsync(user, allianceRole.Name);

			await UpdateRole(user, allianceRole, inAlliance, hasRole);
		}

		/// <summary>
		/// Verify user corp role.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="corpRole"></param>
		/// <param name="eveId"></param>
		/// <returns></returns>
		async Task VerifyCorp(IGuildUser user, IRole corpRole, IRole legacyRole, int eveId)
		{
			IApiResponse corpusersReponse = await ApiModule.Esi.Path("/corporations/{corporation_id}/members/").Get(("corporation_id", Config.CorporationID));
			List<int> corpUsers = corpusersReponse.ToType<List<int>>().FirstPage;

			bool inCorp = corpUsers.Contains(eveId);
			bool hasCorpRole = await RoleModule.HasRoleAsync(user, corpRole.Name);
			bool hasLegacyRole = await RoleModule.HasRoleAsync(user, legacyRole.Name);

			await UpdateRole(user, corpRole, inCorp, hasCorpRole);

			// Give char legacy role if he has been in corp for six months
			if (!inCorp && hasCorpRole)
			{
				int days = await GetDaysInCorp(eveId);

				if (days >= 30 * 6)// Min of six months in corp.
				{
					await LogModule.LogMessage($"{user.Nickname} has been in corp for {days} days and has been moved to #Legacy.", Config.LogChannel);
					await UpdateRole(user, legacyRole, true, hasLegacyRole);
				}
			}

			if (inCorp && !hasCorpRole)
			{
				string name = (Message.Author as IGuildUser).Nickname ?? Message.Author.Username;

				if (!await JobModule.JobExistsAsync<CheckupJob>(x => x.Character == name))
					await JobModule.AddJobAsync(new CheckupJob("RecruitmentModule", name));
			}
		}

		/// <summary>
		/// Update a role to its appropriate status. Only modify the role if it needs a change.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="role"></param>
		/// <param name="shouldHave"></param>
		/// <param name="have"></param>
		/// <returns></returns>
		Task UpdateRole(IGuildUser user, IRole role, bool shouldHave, bool have)
		{
			if (shouldHave && !have)
				return RoleModule.ModifyRoleAsync(user, RoleAction.Add, role.Name);
			else if (!shouldHave && have)
				return RoleModule.ModifyRoleAsync(user, RoleAction.Remove, role.Name);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Get character days in corp.
		/// </summary>
		/// <param name="eveId"></param>
		/// <returns></returns>
		async Task<int> GetDaysInCorp(int eveId)
		{
			IApiResponse<List<EsiCharacterCorporationHistory>> corpHistoryResponse = await ApiModule.Esi.Path("/characters/{character_id}/corporationhistory/").Get<List<EsiCharacterCorporationHistory>>(("character_id", eveId));
			List<EsiCharacterCorporationHistory> corpHistory = corpHistoryResponse.FirstPage;

			if (corpHistory[0].CorporationID != Config.CorporationID)
				return 0;

			return (DateTime.UtcNow - corpHistory[0].Date).Days;
		}

		/// <summary>
		/// Round date time to neares hour, 1 = up 0 = down
		/// </summary>
		/// <param name="dt"></param>
		/// <param name="dir"></param>
		/// <returns></returns>
		DateTime RoundDateTime(DateTime dt)
		{
			return dt.AddSeconds(-dt.Second).AddMinutes(60 - dt.Minute).AddHours(-Config.NameVerificationInterval);
		}

		/// <summary>
		/// Method to inform recruites when there is time for a checkup.
		/// </summary>
		/// <param name="job"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		Task CheckupJobMethod(CheckupJob job, MethodContext context)
		{
			return LogModule.LogMessage($"{job.Character} has been in corp for 30 days. Time for a checkup.", Config.CheckupLogChannel);
		}

		/// <summary>
		/// Method for running the VerifyUsers method.
		/// </summary>
		/// <param name="job"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		Task VerifyJobMethod(VerifyJob job, MethodContext context)
		{
			return VerifyUsers(context.Guild);
		}
	}
}

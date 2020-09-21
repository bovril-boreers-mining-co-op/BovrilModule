using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Modules.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;
using YahurrFramework.Enums;

namespace Modules
{
	[RequiredModule(typeof(DatabaseModule), typeof(LogModule), typeof(JobModule))]
	[Config(typeof(RoleModuleConfig))]
	public class RoleModule : YModule
	{
		public new RoleModuleConfig Config
		{
			get
			{
				return (RoleModuleConfig)base.Config;
			}
		}

		private DatabaseModule DatabaseModule { get; set; }

		private LogModule LogModule { get; set; }

		private JobModule JobModule { get; set; }

		protected override async Task Init()
		{
			await LogAsync(LogLevel.Message, $"Initializing {this.GetType().Name}...");

			DatabaseModule = await GetModuleAsync<DatabaseModule>();
			LogModule = await GetModuleAsync<LogModule>();
			JobModule = await GetModuleAsync<JobModule>();

			if (Config.RoleCheckLoop)
			{
				await JobModule.RegisterJobAsync<VerifyRoleJob>(VerifyRoleJob);
				if (!await JobModule.JobExistsAsync<VerifyRoleJob>(x => x.Creator == "RoleModule"))
					await JobModule.AddJobAsync(new VerifyRoleJob("RoleModule", new TimeSpan(Config.RoleCheckInterval, 0, 0)), RoundDateTime(DateTime.UtcNow));
			}
		}

		protected override async Task ReactionAdded(IUserMessage message, ISocketMessageChannel channel, SocketReaction reaction)
		{
			List<DatabaseRow> rows = await DatabaseModule.RunQueryAsync(Config.RoleDatabase, $"SELECT * FROM reactionMessages where message_id = {message.Id} LIMIT 1;");

			// Ignore if a bot is reacting or reaction is not on a reaction message
			if (reaction.User.Value.IsBot || rows.Count == 0)
				return;

			string emote = reaction.Emote.Name.GetHashCode().ToString();
			ulong roleID = await GetRoleID(message.Id, emote);

			// Get and set the context for this call because it comes from an event.
			IGuildUser user = await (message.Channel as IGuildChannel).GetUserAsync(reaction.User.Value.Id);
			SetContext(new MethodContext(user.Guild, channel, message));

			await ModifyRoleAsync(user, RoleAction.Add, new string[] { roleID.ToString() });
			await VerifyRoles();
		}

		protected override async Task ReactionRemoved(IUserMessage message, ISocketMessageChannel channel, SocketReaction reaction)
		{
			List<DatabaseRow> rows = await DatabaseModule.RunQueryAsync(Config.RoleDatabase, $"SELECT * FROM reactionMessages where message_id = {message.Id} LIMIT 1;");

			// Ignore if a bot is reacting or reaction is not on a reaction message
			if (reaction.User.Value.IsBot || rows.Count == 0)
				return;

			string emote = reaction.Emote.Name.GetHashCode().ToString();
			ulong roleID = await GetRoleID(message.Id, emote);

			// Get and set the context for this call because it comes from an event.
			IGuildUser user = await(message.Channel as IGuildChannel).GetUserAsync(reaction.User.Value.Id);
			SetContext(new MethodContext(user.Guild, channel, message));

			await ModifyRoleAsync(user, RoleAction.Remove, new string[] { roleID.ToString() });
			await VerifyRoles();
		}

		#region Commands

		/// <summary>
		/// Register a role selection message.
		/// </summary>
		/// <param name="msgId">Id of message you typed to be used for context.</param>
		/// <param name="roles">List of role name or ids to be selectable.</param>
		/// <returns></returns>
		[Command("register", "msg")]
		[Summary("Register a role selection message.")]
		public async Task RegisterMsg(
			[Summary("ID of message you want to use a description for what theese roles do.")]ulong msgId,
			[Summary("ID or names for the roles you want selectable.")]params string[] roles)
		{
			IUserMessage msg = (IUserMessage)await Channel.GetMessageAsync(msgId);
			List<IRole> guildRoles = await GetRoles(roles);
			List<IEmote> emotes = msg.Reactions.Select(x => x.Key).ToList();

			if (roles.Length > emotes.Count)
			{
				await RespondAsync("Too many input roles, each reaction must have one role.", false, false);
				return;
			}

			if (roles.Length < emotes.Count)
			{
				await RespondAsync("Not enough input roles, each reaction must have one role.", false, false);
				return;
			}

			// Go through all reactions on target message and add the bots own reaction and sync DB
			for (int i = 0; i < emotes.Count; i++)
			{
				IEmote emote = emotes[i];
				IRole role = guildRoles[i];

				await DatabaseModule.RunNonQueryAsync(Config.RoleDatabase, $"INSERT INTO reactionMessages VALUES ({msg.Id}, '{emote.Name.GetHashCode()}', {role.Id});");
				await msg.AddReactionAsync(emote);
			}

			// Delete traces of this command being talked about.
			await Message.DeleteAsync();
		}

		[Command("add", "role")]
		[Summary("Add roles to yourself.")]
		public async Task AddRole(
			[Summary("ID or names of the roles you want.")]params string[] roles)
		{
			if (!(Message.Author is IGuildUser user))
				return;

			await ModifyRoleAsync(user, RoleAction.AddManual, roles);
			await VerifyRoles();
			await RespondAsync("Roles updated!", false, false);
		}

		[IgnoreHelp]
		[Command("add", "roles")]
		[Summary("Add roles to yourself.")]
		public Task AddRoles(
			[Summary("ID or names of the roles you want.")]params string[] roles)
		 => AddRole(roles);

		[Command("remove", "role")]
		[Summary("Remove roles from yourself.")]
		public async Task RemoveRole(
			[Summary("ID or names of the roles you want to remove.")]params string[] roles)
		{
			if (!(Message.Author is IGuildUser user))
				return;

			await ModifyRoleAsync(user, RoleAction.RemoveManual, roles);
			await VerifyRoles();
			await RespondAsync("Roles updated!", false, false);
		}

		[IgnoreHelp]
		[Command("remove", "roles")]
		[Summary("Remove roles from yourself.")]
		public Task RemoveRoles(
			[Summary("ID or names of the roles you want to remove.")]params string[] roles)
		 => RemoveRole(roles);

		/// <summary>
		/// Loop through all guild users and check if they have all assigned roles
		/// </summary>
		/// <returns></returns>
		[Command("rolecheck")]
		public async Task Rolecheck()
		{
			await RespondAsync("Starting role verification...", false, false);

			await VerifyRoles();

			await RespondAsync("Done.", false, false);
		}

		/// <summary>
		/// Syncronize the discord roles and the databse roles.
		/// </summary>
		/// <returns></returns>
		[Summary("Syncronize the discord roles and the databse roles.")]
		[Command("roles", "sync")]
		public async Task SyncRolesCommand()
		{
			await RespondAsync("Starting role sync...", false, false);

			await SyncRoles();

			await RespondAsync("Done.", false, false);
		}

		#endregion

		/// <summary>
		/// Loop through all guild users and check if they have all assigned roles
		/// </summary>
		/// <returns></returns>
		public async Task VerifyRoles()
		{
			foreach (IGuildUser user in await Guild.GetUsersAsync())
			{
				// Get assigned roles for a user.
				List<DatabaseRow> rows = await DatabaseModule.RunQueryAsync(Config.RoleDatabase, $"SELECT role_id FROM userRoles WHERE user_id = {user.Id};");
				List<ulong> roles = rows.Select(x => (ulong)x.Data[0]).ToList();

				// Check all roles user currently has.
				foreach (ulong role in user.RoleIds)
				{
					// Skip roles not configured to be managed.
					if (!Config.RolesToManage.Any(x => x.RoleID == role))
						continue;

					bool hasRole = user.RoleIds.Any(x => x == role);

					// Remove role from user if he does not have permission to have it.
					if (hasRole && !roles.Contains(role))
						await RemoveActualRole(user, role);

					// Add any roles to user if he does not have it.
					if (!hasRole && roles.Contains(role))
						await AddActualRole(user, role);
				}

				// Check all assigned roles.
				foreach (ulong role in roles)
				{
					// Skip roles not configured to be managed.
					if (!Config.RolesToManage.Any(x => x.RoleID == role))
						continue;

					bool hasRole = user.RoleIds.Any(x => x == role);

					// Remove role from user if he does not have permission to have it.
					if (hasRole && !roles.Contains(role))
						await RemoveActualRole(user, role);

					// Add any roles to user if he does not have it.
					if (!hasRole && roles.Contains(role))
						await AddActualRole(user, role);
				}
			}
		}

		/// <summary>
		/// Syncronize the discord roles and the databse roles.
		/// </summary>
		/// <returns></returns>
		public async Task SyncRoles()
		{
			foreach (IGuildUser user in await Guild.GetUsersAsync())
			{
				// Get assigned roles for a user.
				List<DatabaseRow> rows = await DatabaseModule.RunQueryAsync(Config.RoleDatabase, $"SELECT role_id FROM userRoles WHERE user_id = {user.Id};");
				List<ulong> roles = rows.Select(x => (ulong)x.Data[0]).ToList();

				foreach (ulong role in user.RoleIds)
				{
					// Skip roles not configured to be managed.
					if (!Config.RolesToManage.Any(x => x.RoleID == role))
						continue;

					IRole guildRole = Guild.GetRole(role);
					bool hasRole = user.RoleIds.Any(x => x == role);
					await ModifyRoleAsync(user, hasRole ? RoleAction.Add : RoleAction.Remove, guildRole.Name);
				}
			}
		}

		/// <summary>
		/// Change a role status for a user. (Will ignore any roles that are not manages)
		/// </summary>
		/// <param name="user"></param>
		/// <param name="action"></param>
		/// <param name="roles"></param>
		/// <returns></returns>
		public async Task ModifyRoleAsync(IGuildUser user, RoleAction action, params string[] roles)
		{
			// TODO: Fix manual add settings.
			foreach (IRole role in await GetRoles(roles))
			{
				// Skip roles that are not managed.
				if (!Config.RolesToManage.Any(x => x.RoleID == role.Id))
				{
					await LogAsync(LogLevel.Warning, "Trying to modify role that has not been configured to be manages.");
					continue;
				}

				// Take appropriate action based on input.
				switch (action)
				{
					case RoleAction.Add:
						await DatabaseModule.RunNonQueryAsync(Config.RoleDatabase, $"INSERT INTO userRoles VALUES ({user.Id}, {role.Id});");
						break;
					case RoleAction.Remove:
						await DatabaseModule.RunNonQueryAsync(Config.RoleDatabase, $"DELETE FROM userRoles where user_id = {user.Id} AND role_id = {role.Id}");
						break;
					case RoleAction.AddManual:
						ManagedRole managedRole = Config.RolesToManage.First(x => x.RoleID == role.Id);

						if (managedRole.ManualAdd)
							await DatabaseModule.RunNonQueryAsync(Config.RoleDatabase, $"INSERT INTO userRoles VALUES ({user.Id}, {role.Id});");
						break;
					case RoleAction.RemoveManual:
						managedRole = Config.RolesToManage.First(x => x.RoleID == role.Id);

						if (managedRole.ManualAdd)
							await DatabaseModule.RunNonQueryAsync(Config.RoleDatabase, $"DELETE FROM userRoles where user_id = {user.Id} AND role_id = {role.Id}");
						break;
					default:
						break;
				}
			}
		}

		/// <summary>
		/// Check if user has role.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="role"></param>
		/// <returns></returns>
		public async Task<bool> HasRoleAsync(IGuildUser user, string role)
		{
			IRole guildRole = (await GetRoles(new string[] { role }))[0];

			string hasRoleQuery = $"SELECT EXISTS(SELECT * FROM userRoles WHERE user_id = {user.Id} AND role_ID = {guildRole.Id});";
			List<DatabaseRow> hasRole = DatabaseModule.RunQuery(Config.RoleDatabase, hasRoleQuery);

			return hasRole[0].GetData<long>(0) == 1;
		}

		/// <summary>
		/// Add a role to a user.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="role"></param>
		/// <returns></returns>
		async Task AddActualRole(IGuildUser user, ulong role)
		{
			IRole roleInstance = Guild.GetRole(role);
			await user.AddRoleAsync(roleInstance);
			await LogModule.LogMessage($"Role '{roleInstance.Name}' added to {UserOrNickname(user)}", Config.LogChannel);
		}

		/// <summary>
		/// Remove a role from a user
		/// </summary>
		/// <param name="user"></param>
		/// <param name="role"></param>
		/// <returns></returns>
		async Task RemoveActualRole(IGuildUser user, ulong role)
		{
			IRole roleInstance = Guild.GetRole(role);
			await user.RemoveRoleAsync(roleInstance);
			await LogModule.LogMessage($"Role '{roleInstance.Name}' removed from {UserOrNickname(user)}", Config.LogChannel);
		}

		/// <summary>
		/// Returns the user nickname if he has any else return username
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		string UserOrNickname(IGuildUser user)
		{
			return string.IsNullOrEmpty(user.Nickname) ? user.Username : user.Nickname;
		}

		/// <summary>
		/// Get all roles that can be managed from a list of role names or role ids.
		/// </summary>
		/// <param name="roles"></param>
		/// <returns></returns>
		async Task<List<IRole>> GetRoles(string[] roles)
		{
			List<IRole> roleList = new List<IRole>();

			foreach (string role in roles)
			{
				// If inputted the role id use that.
				if (ulong.TryParse(role, out ulong roleID))
				{
					roleList.Add(Guild.GetRole(roleID));
					continue;
				}

				string formattedRole = role.Replace("_", " ").ToLower();
				IRole roleInstance = Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == formattedRole);

				// Skip this role if program is not configured to manage it.
				if (roleInstance is null)
					continue;

				// Warn operator if the program attempts to manage a role it has not been configured to.
				if (!Config.RolesToManage.Any(x => x.RoleID == roleInstance.Id))
				{
					await LogAsync(LogLevel.Warning, $"Role module was asked to manage role {roleInstance.Name} but has not been configured to manage it. Please add it to RolesToManage under the RoleModule config.");
					continue;
				}

				roleList.Add(roleInstance);
			}

			return roleList;
		}

		/// <summary>
		/// Resolve reaction message emote to get role id.
		/// </summary>
		/// <param name="msgId"></param>
		/// <param name="emote"></param>
		/// <returns></returns>
		async Task<ulong> GetRoleID(ulong msgId, string emote)
		{
			List<DatabaseRow> rows = await DatabaseModule.RunQueryAsync(Config.RoleDatabase, $"SELECT role_id FROM reactionMessages where message_id = {msgId} AND emote = '{emote}' LIMIT 1;");
			return (ulong)rows[0].Data[0];
		}

		async Task VerifyRoleJob(VerifyRoleJob job, MethodContext context)
		{
			SetContext(context);
			await VerifyRoles();
		}

		/// <summary>
		/// Round date time to neares hour, 1 = up 0 = down
		/// </summary>
		/// <param name="dt"></param>
		/// <param name="dir"></param>
		/// <returns></returns>
		DateTime RoundDateTime(DateTime dt)
		{
			return dt.AddSeconds(-dt.Second).AddMinutes(60 - dt.Minute).AddHours(-Config.RoleCheckInterval);
		}
	}
}

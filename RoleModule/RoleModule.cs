using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;

namespace RoleModule
{
	[Config(typeof(RoleConfig))]
	public class RoleModule : YModule
	{
		public new RoleConfig Config
		{
			get
			{
				return (RoleConfig)base.Config;
			}
		}

		[Summary("Show a list of all roles.")]
		[Command("roles")]
		public async Task SeeRoles()
		{
			await RespondAsync($"{RolesToString()}");
		}

		[Summary("Add role.")]
		[Command("role")]
		public async Task AddRole(params string[] role)
		{
			(string name, string desc, ulong id) = await GetConfigRole(string.Join(' ', role));
			if (string.IsNullOrEmpty(name))
				return;

			IRole discordRole = Guild?.GetRole(id);
			if (discordRole == null)
			{
				await RespondAsync($"Role id not found. Be sure to shame Mineral and Prople for their bad config");
				return;
			}

			await (Message.Author as SocketGuildUser)?.AddRoleAsync(discordRole);
			await RespondAsync("Role added!");
		}

		[Summary("Remove role.")]
		[Command("role", "remove")]
		public async Task RemoveRole(params string[] role)
		{
			(string name, string desc, ulong id) = await GetConfigRole(string.Join(' ', role));
			if (string.IsNullOrEmpty(name))
				return;

			IRole discordRole = Guild?.GetRole(id);
			if (discordRole == null)
			{
				await RespondAsync($"Role id not found. Be sure to shame Mineral and Prople for their bad config");
				return;
			}

			SocketGuildUser guildUser = Message.Author as SocketGuildUser;
			if (!guildUser.Roles.Any(a => a.Name == name))
			{
				await RespondAsync($"You dont have that role you silly");
				return;
			}

			await (Message.Author as SocketGuildUser)?.RemoveRoleAsync(discordRole);
			await RespondAsync("Role removed!");
		}

		async Task<(string name, string desc, ulong id)> GetConfigRole(string name)
		{
			var role = Config.Roles.Find(a => a.Item1 == name);

			if (string.IsNullOrEmpty(role.Item1))
			{
				await RespondAsync($"Role '{name}' not found.");
				return default;
			}

			return role;
		}

		string RolesToString()
		{
			string output = "List of all roles.\n";
			for (int i = 0; i < Config.Roles.Count; i++)
			{
				(string name, string desc, ulong id) = Config.Roles[i];
				output += $"__**!role {name}**__\n";
				output += $"{desc}\n";
				output += $"Use `!role remove {name}` to remove yourself.\n\n";
			}

			return output;
		}
	}
}

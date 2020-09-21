using Discord;
using EsiModule.Config;
using EveOpenApi;
using EveOpenApi.Api;
using EveOpenApi.Api.Configs;
using EveOpenApi.Authentication;
using EveOpenApi.Authentication.Interfaces;
using EveOpenApi.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;

namespace Modules
{
	[Config(typeof(ApiModuleConfig))]
	public class ApiModule : YModule
	{
		public new ApiModuleConfig Config
		{
			get
			{
				return (ApiModuleConfig)base.Config;
			}
		}

		private IOauthLogin EsiLogin { get; set; }

		public IAPI Esi
		{
			get
			{
				return apis["Esi"];
			}
		}

		public IAPI Seat
		{
			get
			{
				return apis["Seat"];
			}
		}

		Dictionary<string, IAPI> apis = new Dictionary<string, IAPI>();

		protected override async Task Init()
		{
			await LogAsync(YahurrFramework.Enums.LogLevel.Message, $"Initializing {this.GetType().Name}...");

			await CreateEsi();
			await CreateSeat();
		}

		#region Commands

		/// <summary>
		/// Pull an ESI path for information.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="p"></param>
		/// <returns></returns>
		[Command("esi", "get")]
		public async Task EsiPath(string path, params string[] p)
		{
			IGuildUser user = Message.Author as IGuildUser;
			string nickName = string.IsNullOrEmpty(user.Nickname) ? user.Username : user.Nickname;
			IApiResponse response = await Esi.Path(path).SetUsers(nickName).Get(p);

			if (response.FirstPage.Length >= 2000)
			{
				File.WriteAllText("Files/ESI.txt", response.FirstPage);
				await Channel.SendFileAsync("Files/ESI.txt", "Response was longer than 2000 characters.");
				File.Delete("Files/ESI.txt");
			}
			else
			{
				await RespondAsync(response.FirstPage, false, false);
			}
		}

		/// <summary>
		/// Pull an ESI path for information.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="p"></param>
		/// <returns></returns>
		[Command("esi", "post")]
		public async Task EsiPathPost(string path, params string[] p)
		{
			IGuildUser user = Message.Author as IGuildUser;
			string nickName = string.IsNullOrEmpty(user.Nickname) ? user.Username : user.Nickname;
			IApiResponse response = await Esi.Path(path).SetUsers(nickName).Post(p);

			if (response.FirstPage.Length >= 2000)
			{
				File.WriteAllText("Files/ESI.txt", response.FirstPage);
				await Channel.SendFileAsync("Files/ESI.txt", "Response was longer than 2000 characters.");
				File.Delete("Files/ESI.txt");
			}
			else
			{
				await RespondAsync(response.FirstPage, false, false);
			}
		}

		/// <summary>
		/// Add an ESI token.
		/// </summary>
		/// <param name="scope"></param>
		/// <returns></returns>
		[Command("esi", "tokens", "add")]
		[Summary("Add token with scope.")]
		public async Task EsiAddToken(
			[Summary("Scopes for this token.")] params string[] scope)
		{
			string url = await AddToken((Scope)string.Join(' ', scope));
			await RespondAsync(url, false, false);
		}

		/// <summary>
		/// Get all tokens for all users
		/// </summary>
		/// <returns></returns>
		[Command("esi", "tokens", "get")]
		[Summary("Get all tokens for all users.")]
		public Task EsiGetTokens()
		{
			string response = "";

			IList<string> users = EsiLogin.GetUsers();
			for (int u = 0; u < users.Count; u++)
			{
				string user = users[u];

				response += $"{u}: {user}:\n";
				IList<IToken> tokens = EsiLogin.GetTokens(user);
				for (int t = 0; t < tokens.Count; t++)
				{
					response += $"	{t}: {tokens[t].Scope}\n";
				}
			}

			if (string.IsNullOrEmpty(response))
				return RespondAsync($"No users authed.", false, false);
			else
				return RespondAsync($"```{response}```", false, false);
		}

		/// <summary>
		/// Get token information.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		[Command("esi", "tokens", "info")]
		[Summary("Get token information.")]
		public Task EsiGetTokenInfo(int user, int token)
		{
			IList<string> users = EsiLogin.GetUsers();

			if (user < 0 || user >= users.Count)
				return RespondAsync("User index out of bounds.", false, false);

			IList<IToken> tokens = EsiLogin.GetTokens(users[user]);

			if (token < 0 || token >= users.Count)
				return RespondAsync("Token index out of bounds.", false, false);

			IOauthToken oauthToken = tokens[token] as IOauthToken;
			return RespondAsync($"```" +
				$"{DateTime.UtcNow}\n" +
				$"{users[user]}:\n" +
				$"	AccessToken: {oauthToken.AccessToken[^10..^0]}\n" +
				$"	Refresh Token: {oauthToken.RefreshToken[^10..^0]}\n" +
				$"	Expires: {oauthToken.Expires}\n" +
				$"	TokenType: {oauthToken.TokenType}" +
				$"```", false, false);
		}

		#endregion

		/// <summary>
		/// Add a token to the ESI login.
		/// </summary>
		/// <param name="scope"></param>
		/// <returns></returns>
		public Task<string> AddToken(IScope scope)
		{
			// Schedule a save after the token has been added. Timeout is five seconds
			Task.Run(async () =>
			{
				await Task.Delay(6000);
				await SaveAsync("EsiLogin", EsiLogin.ToEncrypted());
			});

			return EsiLogin.GetAuthUrl(scope);
		}

		/// <summary>
		/// Setup ESI Login and API
		/// </summary>
		/// <returns></returns>
		async Task CreateEsi()
		{
			await LogAsync(YahurrFramework.Enums.LogLevel.Message, "Creating ESI API");
			OAuthLoginBuilder builder = new LoginBuilder().Eve;
			builder.WithCredentials(Config.EsiClientID, Config.EsiCallback);

			if (await ExistsAsync("EsiLogin"))
				builder.FromEncrypted(await LoadAsync<string>("EsiLogin"));

			IApiConfig config = new EsiConfig()
			{
				UserAgent = Config.EsiUseAgent,
				DefaultUser = Config.EsiDefaultUser,
			};

			EsiLogin = await builder.Build();
			apis["Esi"] = new ApiBuilder(config, EsiLogin).Build();
		}

		/// <summary>
		/// Setup SeAT API
		/// </summary>
		/// <returns></returns>
		async Task CreateSeat()
		{
			await LogAsync(YahurrFramework.Enums.LogLevel.Message, "Creating SeAT API");

			IKeyLogin login = new KeyLoginBuilder().Build();
			login.AddKey(Config.SeatToken, Config.SeatDefaultUser, (Scope)"");

			IApiConfig config = new SeATConfig()
			{
				SpecURL = "https://seat.bovrilbloodminers.org/docs/api-docs.json",
				UserAgent = Config.EsiUseAgent,
				DefaultUser = Config.SeatDefaultUser
			};

			apis["Seat"] = new ApiBuilder(config, login).Build();
		}
	}
}

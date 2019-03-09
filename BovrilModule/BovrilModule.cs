using Discord.WebSocket;
using NModule;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;

// Mining Ledger Bot
namespace BovrilModule
{
	[Config(typeof(BovrilConfig))]
	[RequiredModule(typeof(NotificationModule))]
	public partial class BovrilModule : YModule
	{
		public new BovrilConfig Config
		{
			get
			{
				return (BovrilConfig)base.Config;
			}
		}

		NotificationModule notificationModule;

		protected override async Task Init()
		{
			notificationModule = await GetModuleAsync<NotificationModule>();
		}

		protected override async Task MessageReceived(SocketMessage message)
		{
			//Remove filtered words
			foreach (var item in Config.FileterdWords)
			{
				Match match = Regex.Match(message.Content, item.regEx);

				if (match.Success)
				{
					await message.DeleteAsync();

					if (!string.IsNullOrEmpty(item.message))
						await RespondAsync(item.message, item.dm);
				}
			}

			//Alice o/ rage boner.
			Match aliceMatch = Regex.Match(message.Content, "^o\\/|\\\\o|o7$");
			if (aliceMatch.Success && message?.Channel.Id != 264791114727424000)
				await RespondAsync("hand slap ya pubbie, keep that shit in high sec!", false);
		}
	}
}

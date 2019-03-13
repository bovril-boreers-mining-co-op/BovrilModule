using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;

namespace KeepStarModule
{
	[Config(typeof(KeepStarConfig))]
	public class KSProgress : YModule
	{
		public new KeepStarConfig Config
		{
			get
			{
				return (KeepStarConfig)base.Config;
			}
		}

		IUserMessage KSMessage;

		protected async override Task Init()
		{
			if (!File.Exists("Files/KeepStar.json"))
			{
				string json = JsonConvert.SerializeObject(new KeepStarProgress(), Formatting.Indented);
				using (StreamWriter writer = File.CreateText("Files/KeepStar.json"))
					await writer.WriteAsync(json);
			}

			ITextChannel channel = await Guild.GetTextChannelAsync(Config.UpdateChannel);
			if (channel != null && await ExistsAsync("UpdateMessage"))
			{
				ulong msgID = await LoadAsync<ulong>("UpdateMessage");
				KSMessage = await channel.GetMessageAsync(msgID) as IUserMessage;
			}
			else if (channel != null)
			{
				KSMessage = await SendToChannel(channel) as IUserMessage;
				await SaveAsync("UpdateMessage", KSMessage.Id);
			}
		}

		[Command("ks")]
		public async Task ShowProgress()
		{
			await SendToChannel(Channel);
		}

		[Command("update", "ks")]
		public async Task UpdateKSMessage()
		{
			string json = File.ReadAllText("Files/KeepStar.json");
			KeepStarProgress progress = JsonConvert.DeserializeObject<KeepStarProgress>(json);

			await KSMessage.ModifyAsync(a =>
			{
				a.Embed = CreateKeepStarEmebed(progress);
			});
		}

		async Task<IMessage> SendToChannel(IMessageChannel channel)
		{
			string json = File.ReadAllText("Files/KeepStar.json");
			KeepStarProgress progress = JsonConvert.DeserializeObject<KeepStarProgress>(json);

			return await channel.SendMessageAsync(embed: CreateKeepStarEmebed(progress));
		}

		Embed CreateKeepStarEmebed(KeepStarProgress progress)
		{
			EmbedBuilder embedBuilder = new EmbedBuilder();
			embedBuilder.WithColor(Color.Gold);
			embedBuilder.WithThumbnailUrl("https://imageserver.eveonline.com/Render/35834_128.png");
			embedBuilder.WithAuthor($"Operation Golden Throne - {progress.Complete * 100}%",
				"https://imageserver.eveonline.com/Corporation/98270640_128.png",
				"https://goonfleet.com/index.php/topic/269069-operation-golden-throne/");
			embedBuilder.WithFooter("Dontations can be made at 1DQ1-A, B-7DFU or JITA 4-4.");

			double totalAssets = Math.Round(progress.AssetHistory[0].Item1 / 1000000000, 1);
			double escrow = Math.Round(progress.BuyOrdersEscrow / 1000000000, 1);

			EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder();
			fieldBuilder.WithName($"Total Assets: {totalAssets.ToString("N1")}b ISK");
			fieldBuilder.WithValue($"Buy orders in escrow: {escrow.ToString("N1")}b ISK");
			embedBuilder.AddField(fieldBuilder);

			fieldBuilder = new EmbedFieldBuilder();
			fieldBuilder.WithName("Currently needed: PI");
			fieldBuilder.WithValue("P2: Viral Agent, Rocket Fuel, Polyaramids...\n⌴");
			embedBuilder.AddField(fieldBuilder);

			double totalPI = Math.Round(progress.TotalPI / 1000000000, 1);
			double p1 = Math.Round(progress.P1 / 1000000000, 1);
			double p2 = Math.Round(progress.P2 / 1000000000, 1);
			double p3 = Math.Round(progress.P3 / 1000000000, 1);
			double p4 = Math.Round(progress.P4 / 1000000000, 1);

			fieldBuilder = new EmbedFieldBuilder();
			fieldBuilder.WithName($"PI: {totalPI.ToString("N1")}b ISK");
			fieldBuilder.WithIsInline(true);
			fieldBuilder.WithValue($"P1: {p1.ToString("N1")}b ISK\n" +
									$"P2: {p2.ToString("N1")}b ISK\n" +
									$"P3: {p3.ToString("N1")}b ISK\n" +
									$"P4: {p4.ToString("N1")}b ISK\n");
			embedBuilder.AddField(fieldBuilder);

			double wallet = Math.Round(progress.Wallet/ 1000000000, 1);
			fieldBuilder = new EmbedFieldBuilder();
			fieldBuilder.WithName($"Wallet: {wallet.ToString("N1")}b ISK");
			fieldBuilder.WithIsInline(true);

			string output = "";
			for (int i = 0; i < progress.AssetHistory.Count; i++)
			{
				float walletHistory = progress.AssetHistory[i].Item1;
				double formated = Math.Round(walletHistory / 1000000000, 1);

				output += $"{progress.AssetHistory[i].Item2}: {formated.ToString("N1")}b ISK\n";
			}
			fieldBuilder.WithValue(output);
			embedBuilder.AddField(fieldBuilder);

			return embedBuilder.Build();
		}
	}
}

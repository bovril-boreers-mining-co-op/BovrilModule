using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules
{
	internal class KillmailEmbedBuilder
	{
		EmbedBuilder embedBuilder;

		public KillmailEmbedBuilder()
		{
			this.embedBuilder = new EmbedBuilder();
			this.embedBuilder.Color = Color.Green;
		}

		/// <summary>
		/// Add victim to the embed.
		/// </summary>
		/// <param name="charName"></param>
		/// <param name="charId"></param>
		/// <param name="corpName"></param>
		/// <param name="corpId"></param>
		/// <param name="allianceName"></param>
		/// <param name="allianceId"></param>
		/// <returns></returns>
		public KillmailEmbedBuilder AddVictim(string charName, int charId, string corpName, int corpId, string allianceName, int allianceId)
		{
			EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder();
			fieldBuilder.Name = "Victim";
			fieldBuilder.Value =
					$"Name: [{charName}](https://zkillboard.com/character/{charId}/)\n" +
					$"Corp: [{corpName}](https://zkillboard.com/corporation/{corpId}/)\n" +
					$"Alliance: [{allianceName}](https://zkillboard.com/alliance/{allianceId}/)\n";

			embedBuilder.WithFields(fieldBuilder);
			return this;
		}

		/// <summary>
		/// Add final blow to embed.
		/// </summary>
		/// <param name="charName"></param>
		/// <param name="charId"></param>
		/// <param name="corpName"></param>
		/// <param name="corpId"></param>
		/// <param name="allianceName"></param>
		/// <param name="allianceId"></param>
		/// <returns></returns>
		public KillmailEmbedBuilder AddFinalBlow(string charName, int charId, string corpName, int corpId, string allianceName, int allianceId, string shipName, int shipId)
		{
			EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder();
			fieldBuilder.Name = "Final Blow";
			fieldBuilder.Value =
					$"Name: [{charName}](https://zkillboard.com/character/{charId}/)\n" +
					$"Corp: [{corpName}](https://zkillboard.com/corporation/{corpId}/)\n" +
					$"Ship: [{shipName}](https://zkillboard.com/ship/{shipId}/)\n" +
					$"Alliance: [{allianceName}](https://zkillboard.com/alliance/{allianceId}/)\n";

			embedBuilder.WithFields(fieldBuilder);
			return this;
		}

		/// <summary>
		/// Add kill details to embed.
		/// </summary>
		/// <param name="awox"></param>
		/// <param name="value"></param>
		/// <param name="killTime"></param>
		/// <param name="killId"></param>
		/// <returns></returns>
		public KillmailEmbedBuilder AddDetails(bool awox, float value, DateTime killTime, ulong killId)
		{
			EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder();
			fieldBuilder.Name = "Details";
			fieldBuilder.Value =
					$"{(awox ? "~**Possible Awox * *~\n" : "")}" +
					$"Value: {value.ToString("0,00")} ISK\n" +
					$"Time: {killTime.ToString("HH:mm")} EVE\n" +
					$"[zKill link](https://zkillboard.com/kill/{killId}/)\n";

			embedBuilder.WithFields(fieldBuilder);
			return this;
		}

		/// <summary>
		/// Add embed title.
		/// </summary>
		/// <param name="shipName"></param>
		/// <param name="systemName"></param>
		/// <param name="killId"></param>
		/// <returns></returns>
		public KillmailEmbedBuilder AddTitle(string shipName, string systemName, ulong killId)
		{
			embedBuilder.WithAuthor(name: $"{shipName} destroyed in {systemName}",
				url: $"https://zkillboard.com/kill/{killId}/",
				iconUrl: "https://i.imgur.com/ZTKc3mr.png");

			return this;
		}

		/// <summary>
		/// Add embed thumbnail.
		/// </summary>
		/// <param name="shipId"></param>
		/// <returns></returns>
		public KillmailEmbedBuilder AddThumbnail(int shipId)
		{
			embedBuilder.WithThumbnailUrl($"https://imageserver.eveonline.com/Render/{shipId}_128.png");
			return this;
		}

		/// <summary>
		/// Add embed footer.
		/// </summary>
		/// <param name="corpId"></param>
		/// <returns></returns>
		public KillmailEmbedBuilder AddFooter(int corpId)
		{
			embedBuilder.WithFooter("", iconUrl: $"https://imageserver.eveonline.com/Corporation/{corpId}_128.png");
			return this;
		}

		public Embed Build()
		{
			return embedBuilder.Build();
		}
	}
}

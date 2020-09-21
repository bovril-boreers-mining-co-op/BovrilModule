using Discord;
using Modules;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules
{
	public class MoonComposition : SystemMoon
	{
		public float MoonOre
		{
			get
			{
				return Ubiquitous + Common + Uncommon + Rare + Exceptional;
			}
		}

		public float TotalOre
		{
			get
			{
				return MoonOre + HighSec + NullSec;
			}
		}

		public bool IsTatara { get; }

		public string Rarity { get; }

		/// <summary>
		/// How many of the rarest ore this moon has.
		/// </summary>
		public int RarityCount { get; }

		#region Ore

		#region Ubiquitous

		public float Bitumens { get; }

		public float Coesite { get; }

		public float Sylvite { get; }

		public float Zeolites { get; }

		public float Ubiquitous
		{
			get
			{
				return Bitumens + Coesite + Sylvite + Zeolites;
			}
		}

		#endregion

		#region Common

		public float Cobaltite { get; }

		public float Euxenite { get; }

		public float Scheelite { get; }

		public float Titanite { get; }

		public float Common
		{
			get
			{
				return Cobaltite + Euxenite + Scheelite + Titanite;
			}
		}

		#endregion

		#region Uncommon

		public float Chromite { get; }

		public float Otavite { get; }

		public float Sperrylite { get; }

		public float Vanadinite { get; }

		public float Uncommon
		{
			get
			{
				return Chromite + Otavite + Sperrylite + Vanadinite;
			}
		}

		#endregion

		#region Rare

		public float Carnotite { get; }

		public float Cinnabar { get; }

		public float Pollucite { get; }

		public float Zircon { get; }

		public float Rare
		{
			get
			{
				return Carnotite + Cinnabar + Pollucite + Zircon;
			}
		}

		#endregion

		#region Exceptional

		public float Loparite { get; }

		public float Monazite { get; }

		public float Xenotime { get; }

		public float Ytterbite { get; }

		public float Exceptional
		{
			get
			{
				return Loparite + Monazite + Xenotime + Ytterbite;
			}
		}

		#endregion

		#region HighSec

		public float Veldspar { get; }

		public float Scordite { get; }

		public float Pyroxeres { get; }

		public float Plagioclase { get; }

		public float Omber { get; }

		public float Kernite { get; }

		public float Jaspet { get; }

		public float Hemorphite { get; }

		public float Hedbergite { get; }

		public float HighSec
		{
			get
			{
				return Veldspar + Scordite + Pyroxeres + Plagioclase + Omber + Kernite + Jaspet + Hemorphite + Hedbergite;
			}
		}

		#endregion

		#region NullSec

		public float Gneiss { get; }

		public float Ochre { get; }

		public float Spodumain { get; }

		public float Crokite { get; }

		public float Bistot { get; }

		public float Arkonor { get; }

		public float NullSec
		{
			get
			{
				return Gneiss + Ochre + Spodumain + Crokite + Bistot + Arkonor;
			}
		}

		#endregion

		#endregion

		public MoonComposition(SystemMoon systemMoon, bool isTatara, List<DatabaseRow> data) : base(systemMoon)
		{
			IsTatara = isTatara;

			for (int i = 0; i < data.Count; i++)
			{
				DatabaseRow row = data[i];

				#region MoonOre

				// Ubiquituous
				if (row.GetData<string>(0) == "Bitumens")
					Bitumens = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Sylvite")
					Sylvite = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Coesite")
					Coesite = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Zeolites")
					Zeolites = (float)row.GetData<decimal>(1);

				// Common
				if (row.GetData<string>(0) == "Cobaltite")
					Cobaltite = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Euxenite")
					Euxenite = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Scheelite")
					Scheelite = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Titanite")
					Titanite = (float)row.GetData<decimal>(1);

				// Uncommon
				if (row.GetData<string>(0) == "Chromite")
					Chromite = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Otavite")
					Otavite = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Sperrylite")
					Sperrylite = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Vanadinite")
					Vanadinite = (float)row.GetData<decimal>(1);

				// Rare
				if (row.GetData<string>(0) == "Carnotite")
					Carnotite = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Cinnabar")
					Cinnabar = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Pollucite")
					Pollucite = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Zircon")
					Zircon = (float)row.GetData<decimal>(1);

				// Exceptional
				if (row.GetData<string>(0) == "Loparite")
					Loparite = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Monazite")
					Monazite = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Xenotime")
					Xenotime = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Ytterbite")
					Ytterbite = (float)row.GetData<decimal>(1);

				// High sec
				if (row.GetData<string>(0) == "Veldspar")
					Veldspar = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Scordite")
					Scordite = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Pyroxeres")
					Pyroxeres = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Plagioclase")
					Plagioclase = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Omber")
					Omber = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Kernite")
					Kernite = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Jaspet")
					Jaspet = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Hemorphite")
					Hemorphite = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Hedbergite")
					Hedbergite = (float)row.GetData<decimal>(1);

				// Null sec
				if (row.GetData<string>(0) == "Gneiss")
					Gneiss = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Ochre" || row.GetData<string>(0) == "Dark Ochre")
					Ochre = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Spodumain")
					Spodumain = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Crokite")
					Crokite = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Bistot")
					Bistot = (float)row.GetData<decimal>(1);
				else if (row.GetData<string>(0) == "Arkonor")
					Arkonor = (float)row.GetData<decimal>(1);

				#endregion

				#region Rarity

				if (Exceptional > 0)
				{
					Rarity = "R64";

					if (Loparite > 0)
						RarityCount++;
					else if (Monazite > 0)
						RarityCount++;
					else if (Xenotime > 0)
						RarityCount++;
					else if (Ytterbite > 0)
						RarityCount++;
				}
				else if (Rare > 0)
				{
					Rarity = "R32";

					if (Carnotite > 0)
						RarityCount++;
					else if (Cinnabar > 0)
						RarityCount++;
					else if (Pollucite > 0)
						RarityCount++;
					else if (Zircon > 0)
						RarityCount++;
				}
				else if (Uncommon > 0)
				{
					Rarity = "R32";

					if (Chromite > 0)
						RarityCount++;
					else if (Otavite > 0)
						RarityCount++;
					else if (Sperrylite > 0)
						RarityCount++;
					else if (Vanadinite > 0)
						RarityCount++;
				}
				else if (Common > 0)
				{
					Rarity = "R16";

					if (Cobaltite > 0)
						RarityCount++;
					else if (Euxenite > 0)
						RarityCount++;
					else if (Scheelite > 0)
						RarityCount++;
					else if (Titanite > 0)
						RarityCount++;
				}
				else if (Ubiquitous > 0)
				{
					Rarity = "R8";

					if (Bitumens > 0)
						RarityCount++;
					else if (Coesite > 0)
						RarityCount++;
					else if (Sylvite > 0)
						RarityCount++;
					else if (Zeolites > 0)
						RarityCount++;
				}

				#endregion
			}
		}

		public Embed PrettyMoon()
		{
			int refineryId = IsTatara ? 35836 : 35835;

			EmbedBuilder builder = new EmbedBuilder();
			builder.WithAuthor($"{Name}",
								"https://image.eveonline.com/Type/14_64.png",
								$"http://evemaps.dotlan.net/system/{System}");
			builder.WithThumbnailUrl($"https://image.eveonline.com/Render/{refineryId}_64.png");

			switch (Rarity)
			{
				case "R4":
					builder.WithColor(new Color(255, 242, 204));
					break;
				case "R8":
					builder.WithColor(new Color(255, 229, 153));
					break;
				case "R16":
					builder.WithColor(new Color(255, 217, 102));
					break;
				case "R32":
					builder.WithColor(new Color(224, 204, 204));
					break;
				case "R64":
					builder.WithColor(new Color(234, 153, 153));
					break;
				default:
					builder.WithColor(new Color(207, 226, 243));
					break;
			}

			AddMoonStats(builder);
			return builder.Build();
		}

		void AddMoonStats(EmbedBuilder embed)
		{
			if (Exceptional > 0)
				embed.AddField($"Exceptional",
					$"{(Monazite == 0 ? "" : Math.Round(Monazite * 100, 0) + "% Monazite \n")}" +
					$"{(Loparite == 0 ? "" : Math.Round(Loparite * 100, 0) + "% Loparite \n")}" +
					$"{(Xenotime == 0 ? "" : Math.Round(Xenotime * 100, 0) + "% Xenotime \n")}" +
					$"{(Ytterbite == 0 ? "" : Math.Round(Ytterbite * 100, 0) + "% Ytterbite\n")}");

			if (Rare > 0)
				embed.AddField($"Rare -- ",
					$"{(Carnotite == 0 ? "" : Math.Round(Carnotite * 100, 0) + "% Carnotite \n")}" +
					$"{(Cinnabar == 0 ? "" : Math.Round(Cinnabar * 100, 0) + "% Cinnabar \n")}" +
					$"{(Pollucite == 0 ? "" : Math.Round(Pollucite * 100, 0) + "% Pollucite \n")}" +
					$"{(Zircon == 0 ? "" : Math.Round(Zircon * 100, 0) + "% Zircon\n")}");

			if (Uncommon > 0)
				embed.AddField($"Uncommon",
					$"{(Chromite == 0 ? "" : Math.Round(Chromite * 100, 0) + "% Chromite \n")}" +
					$"{(Otavite == 0 ? "" : Math.Round(Otavite * 100, 0) + "% Otavite \n")}" +
					$"{(Sperrylite == 0 ? "" : Math.Round(Sperrylite * 100, 0) + "% Sperrylite \n")}" +
					$"{(Vanadinite == 0 ? "" : Math.Round(Vanadinite * 100, 0) + "% Vanadinite\n")}");

			if (Common > 0)
				embed.AddField($"Common",
					$"{(Cobaltite == 0 ? "" : Math.Round(Cobaltite * 100, 0) + "% Cobaltite \n")}" +
					$"{(Euxenite == 0 ? "" : Math.Round(Euxenite * 100, 0) + "% Euxenite \n")}" +
					$"{(Scheelite == 0 ? "" : Math.Round(Scheelite * 100, 0) + "% Scheelite \n")}" +
					$"{(Titanite == 0 ? "" : Math.Round(Titanite * 100, 0) + "% Titanite\n")}");

			if (Ubiquitous > 0)
				embed.AddField($"Ubiquitous",
					$"{(Bitumens == 0 ? "" : Math.Round(Bitumens * 100, 0) + "% Bitumens \n")}" +
					$"{(Coesite == 0 ? "" : Math.Round(Coesite * 100, 0) + "% Coesite \n")}" +
					$"{(Sylvite == 0 ? "" : Math.Round(Sylvite * 100, 0) + "% Sylvite \n")}" +
					$"{(Zeolites == 0 ? "" : Math.Round(Zeolites * 100, 0) + "% Zeolites\n")}");

			if (HighSec > 0)
				embed.AddField($"HighSec",
					$"{(Veldspar == 0 ? "" : Math.Round(Veldspar * 100, 0) + "% Veldspar \n")}" +
					$"{(Scordite == 0 ? "" : Math.Round(Scordite * 100, 0) + "% Scordite \n")}" +
					$"{(Pyroxeres == 0 ? "" : Math.Round(Pyroxeres * 100, 0) + "% Pyroxeres \n")}" +
					$"{(Plagioclase == 0 ? "" : Math.Round(Plagioclase * 100, 0) + "% Plagioclase \n")}" +
					$"{(Omber == 0 ? "" : Math.Round(Omber * 100, 0) + "% Omber \n")}" +
					$"{(Kernite == 0 ? "" : Math.Round(Kernite * 100, 0) + "% Kernite \n")}" +
					$"{(Jaspet == 0 ? "" : Math.Round(Jaspet * 100, 0) + "% Jaspet \n")}" +
					$"{(Hemorphite == 0 ? "" : Math.Round(Hemorphite * 100, 0) + "% Hemorphite \n")}" +
					$"{(Hedbergite == 0 ? "" : Math.Round(Hedbergite * 100, 0) + "% Hedbergite\n")}");

			if (NullSec > 0)
				embed.AddField($"NullSec",
					$"{(Gneiss == 0 ? "" : Math.Round(Gneiss * 100, 0) + "% Gneiss \n")}" +
					$"{(Ochre == 0 ? "" : Math.Round(Ochre * 100, 0) + "% Ochre \n")}" +
					$"{(Spodumain == 0 ? "" : Math.Round(Spodumain * 100, 0) + "% Spodumain \n")}" +
					$"{(Crokite == 0 ? "" : Math.Round(Crokite * 100, 0) + "% Crokite \n")}" +
					$"{(Bistot == 0 ? "" : Math.Round(Bistot * 100, 0) + "% Bistot \n")}" +
					$"{(Arkonor == 0 ? "" : Math.Round(Arkonor * 100, 0) + "% Arkonor\n")}");
		}
	}
}

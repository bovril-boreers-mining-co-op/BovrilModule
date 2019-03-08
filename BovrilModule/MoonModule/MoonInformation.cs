using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace BovrilModule
{
	public class MoonInformation : SystemMoon
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

		[JsonProperty]
		IList<object> data;

		[JsonConstructor]
		public MoonInformation(SystemMoon systemMoon, IList<object> data) : base(systemMoon)
		{
			this.data = data;

			#region OreParsing

			// Ubiquituous
			Bitumens = float.Parse(data[6].ToString());
			Sylvite = float.Parse(data[7].ToString());
			Coesite = float.Parse(data[8].ToString());
			Zeolites = float.Parse(data[9].ToString());

			// Common
			Cobaltite = float.Parse(data[10].ToString());
			Euxenite = float.Parse(data[11].ToString());
			Scheelite = float.Parse(data[12].ToString());
			Titanite = float.Parse(data[13].ToString());

			// Uncommon
			Chromite = float.Parse(data[14].ToString());
			Otavite = float.Parse(data[15].ToString());
			Sperrylite = float.Parse(data[16].ToString());
			Vanadinite = float.Parse(data[17].ToString());

			// Rare
			Carnotite = float.Parse(data[18].ToString());
			Cinnabar = float.Parse(data[19].ToString());
			Pollucite = float.Parse(data[20].ToString());
			Zircon = float.Parse(data[21].ToString());

			// Exceptional
			Loparite = float.Parse(data[22].ToString());
			Monazite = float.Parse(data[23].ToString());
			Xenotime = float.Parse(data[24].ToString());
			Ytterbite = float.Parse(data[25].ToString());

			// High Sec
			Veldspar = float.Parse(data[26].ToString());
			Scordite = float.Parse(data[27].ToString());
			Pyroxeres = float.Parse(data[28].ToString());
			Plagioclase = float.Parse(data[29].ToString());
			Omber = float.Parse(data[30].ToString());
			Kernite = float.Parse(data[31].ToString());
			Jaspet = float.Parse(data[32].ToString());
			Hemorphite = float.Parse(data[33].ToString());
			Hedbergite = float.Parse(data[34].ToString());

			// Null sec
			Gneiss = float.Parse(data[35].ToString());
			Ochre = float.Parse(data[36].ToString());
			Spodumain = float.Parse(data[37].ToString());
			Crokite = float.Parse(data[38].ToString());
			Bistot = float.Parse(data[39].ToString());
			Arkonor = float.Parse(data[40].ToString());

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
}

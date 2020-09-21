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

		[JsonProperty]
		List<List<object>> data;

		[JsonConstructor]
		public MoonInformation(SystemMoon systemMoon, bool isTatara, List<List<object>> data) : base(systemMoon)
		{
			this.data = data;
			IsTatara = isTatara;
			//IsTatara = data[0][2].ToString() == "1";

			for (int i = 0; i < data.Count; i++)
			{
				List<object> row = data[i];

				#region MoonOre

				// Ubiquituous
				if (row[0].ToString() == "Bitumens")
					Bitumens = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Sylvite")
					Sylvite = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Coesite")
					Coesite = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Zeolites")
					Zeolites = float.Parse(row[1].ToString());

				// Common
				if (row[0].ToString() == "Cobaltite")
					Cobaltite = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Euxenite")
					Euxenite = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Scheelite")
					Scheelite = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Titanite")
					Titanite = float.Parse(row[1].ToString());

				// Uncommon
				if (row[0].ToString() == "Chromite")
					Chromite = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Otavite")
					Otavite = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Sperrylite")
					Sperrylite = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Vanadinite")
					Vanadinite = float.Parse(row[1].ToString());

				// Rare
				if (row[0].ToString() == "Carnotite")
					Carnotite = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Cinnabar")
					Cinnabar = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Pollucite")
					Pollucite = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Zircon")
					Zircon = float.Parse(row[1].ToString());

				// Exceptional
				if (row[0].ToString() == "Loparite")
					Loparite = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Monazite")
					Monazite = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Xenotime")
					Xenotime = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Ytterbite")
					Ytterbite = float.Parse(row[1].ToString());

				// High sec
				if (row[0].ToString() == "Veldspar")
					Veldspar = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Scordite")
					Scordite = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Pyroxeres")
					Pyroxeres = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Plagioclase")
					Plagioclase = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Omber")
					Omber = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Kernite")
					Kernite = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Jaspet")
					Jaspet = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Hemorphite")
					Hemorphite = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Hedbergite")
					Hedbergite = float.Parse(row[1].ToString());

				// Null sec
				if (row[0].ToString() == "Gneiss")
					Gneiss = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Ochre" || row[0].ToString() == "Dark Ochre")
					Ochre = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Spodumain")
					Spodumain = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Crokite")
					Crokite = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Bistot")
					Bistot = float.Parse(row[1].ToString());
				else if (row[0].ToString() == "Arkonor")
					Arkonor = float.Parse(row[1].ToString());

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
}

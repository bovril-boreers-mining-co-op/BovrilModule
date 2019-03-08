using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;

namespace BovrilModule
{
	[RequiredModule(typeof(NModule.NotificationModule), typeof(MoonModule))]
	public class LocustModule : YModule
	{
		List<LocustCycle> LocustCycles { get; set; }

		NModule.NotificationModule notificationModule;
		MoonModule moonModule;

		protected override async Task Init()
		{
			notificationModule = await GetModuleAsync<NModule.NotificationModule>();
			moonModule = await GetModuleAsync<MoonModule>();

			if (await ExistsAsync("LocustCycles"))
				LocustCycles = await LoadAsync<List<LocustCycle>>("LocustCycles");
			else
				LocustCycles = new List<LocustCycle>();
		}

		[Command("locust", "add", "moon")]
		public async Task AddMoon(int cycle, string system, string planetMoon)
		{
			if (cycle < 0 || cycle > LocustCycles.Count)
				throw new Exception("Index out of bounds.");

			if (!moonModule.TryGetMoon(system, planetMoon, out MoonInformation moon))
				throw new Exception("Moon not found");

			LocustCycles[cycle].AddMoon(moon);

			await SaveAsync("LocustCycles", LocustCycles);
			await RespondAsync($"Moon '{moon.Name}' added to cycle {cycle}");
		}

		[Command("locust", "add", "cycle")]
		public async Task AddCycle(params string[] region)
		{
			LocustCycles.Add(new LocustCycle(string.Join(" ", region)));

			await SaveAsync("LocustCycles", LocustCycles);
			await RespondAsync($"New locust cycle added with id {LocustCycles.Count - 1}");
		}

		[IgnoreHelp]
		[Command("locust", "list")]
		public async Task ListCycles()
		{
			string output = "";
			for (int i = 0; i < LocustCycles.Count; i++)
			{
				LocustCycle cycle = LocustCycles[i];
				output += $"Locust cycle {i} in {cycle.Region} with {cycle.Moons.Count} moons.\n";
			}

			await RespondAsync($"```{output}```");
		}

		[Command("locust", "list")]
		public async Task ListCycles(int cycle)
		{
			string output = "";

			if (cycle < 0 || cycle > LocustCycles.Count)
				throw new Exception("Index out of bounds.");

			LocustCycle lCycle = LocustCycles[cycle];
			for (int i = 0; i < lCycle.Moons.Count; i++)
			{
				MoonInformation moon = lCycle.Moons[i];
				output += $"{NumberToText(moon.RarityCount)} {moon.Rarity} {moon.Name}.\n";
			}

			await RespondAsync($"```{output}```");
		}

		string NumberToText(int number)
		{
			switch (number)
			{
				case 1:
					return "Single";
				case 2:
					return "Double";
				case 3:
					return "Triple";
				case 4:
					return "Quadruple";
				default:
					throw new Exception("Unsupported number");
			}
		}
	}
}

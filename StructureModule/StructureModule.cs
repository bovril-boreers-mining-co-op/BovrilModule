using EveOpenApi.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;
using YahurrFramework.Enums;

namespace Modules
{
	[Config(typeof(StructureModuleConfig))]
	[RequiredModule(typeof(ApiModule))]
	public class StructureModule : YModule
	{
		public new StructureModuleConfig Config
		{
			get
			{
				return (StructureModuleConfig)base.Config;
			}
		}

		private ApiModule ApiModule { get; set; }

		protected override async Task Init()
		{
			await LogAsync(LogLevel.Message, $"Initializing {this.GetType().Name}...");

			ApiModule = await GetModuleAsync<ApiModule>();
		}

		#region Commands

		/// <summary>
		/// List all structures in corp.
		/// </summary>
		/// <returns></returns>
		[Summary("List all corporation structures.")]
		[Command("structures")]
		public async Task ListStructures()
		{
			StringBuilder reply = new StringBuilder();
			List<EsiCorporationStructure> structures = await GetCorporationStructures();
			for (int i = 0; i < structures.Count; i++)
			{
				EsiCorporationStructure structure = structures[i];
				EsiItem structureItem = await GetItem(structure.TypeId);
				EsiSystem structureSystem = await GetSystem(structure.SystemId);

				reply.Append($"{i}: {structureItem.Name} in {structureSystem.Name}\n");
			}

			await RespondAsync($"```{reply}```", false, false);
		}

		#endregion

		/// <summary>
		/// Get all structures in the current corporation.
		/// </summary>
		/// <returns></returns>
		public async Task<List<EsiCorporationStructure>> GetCorporationStructures()
		{
			var response = await ApiModule.Esi.Path("/corporations/{corporation_id}/structures/").Get<List<EsiCorporationStructure>>(("corporation_id", Config.CorporationID));

			return response.SelectMany(x => x).ToList();
		}

		/// <summary>
		/// Get item information
		/// </summary>
		/// <param name="typeId"></param>
		/// <returns></returns>
		public async Task<EsiItem> GetItem(int typeId)
		{
			return (await ApiModule.Esi.Path("/universe/types/{type_id}/").Get<EsiItem>(("type_id", typeId))).FirstPage;
		}

		/// <summary>
		/// Get system information
		/// </summary>
		/// <param name="systemID"></param>
		/// <returns></returns>
		public async Task<EsiSystem> GetSystem(int systemId)
		{
			return (await ApiModule.Esi.Path("/universe/systems/{system_id}/").Get<EsiSystem>(("system_id", systemId))).FirstPage;
		}
	}
}

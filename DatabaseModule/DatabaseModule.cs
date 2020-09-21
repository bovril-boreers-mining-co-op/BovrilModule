using DatabaseModule.Config;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using System.Linq;
using YahurrFramework;
using YahurrFramework.Attributes;

namespace Modules
{
	[Config(typeof(DatabaseModuleConfig))]
	public class DatabaseModule : YModule
	{
		public new DatabaseModuleConfig Config
		{
			get
			{
				return (DatabaseModuleConfig)base.Config;
			}
		}

		protected override Task Init()
		{
			return LogAsync(YahurrFramework.Enums.LogLevel.Message, $"Initializing {this.GetType().Name}...");
		}

		public async Task<List<DatabaseRow>> RunQueryAsync(string database, string query)
		{
			List<DatabaseRow> result;
			using (MySqlConnection conn = GetConnection(database))
			using (MySqlCommand command = new MySqlCommand(query, conn))
			{
				await conn.OpenAsync();
				using (DbDataReader reader = await command.ExecuteReaderAsync())
				{
					result = await ReaderToListAsync(reader);
				}
			}

			return result;
		}

		public List<DatabaseRow> RunQuery(string database, string query)
		{
			List<DatabaseRow> result;
			using (MySqlConnection conn = GetConnection(database))
			using (MySqlCommand command = new MySqlCommand(query, conn))
			{
				conn.Open();

				using (DbDataReader reader = command.ExecuteReader())
				{
					result = ReaderToList(reader);
				}
			}

			return result;
		}

		public async Task RunNonQueryAsync(string database, string query)
		{
			using (MySqlConnection conn = GetConnection(database))
			using (MySqlCommand command = new MySqlCommand(query, conn))
			{
				await conn.OpenAsync();
				await command.ExecuteNonQueryAsync();
			}
		}

		public async Task RunNonQuery(string database, string query)
		{
			using (MySqlConnection conn = GetConnection(database))
			using (MySqlCommand command = new MySqlCommand(query, conn))
			{
				conn.Open();
				command.ExecuteNonQuery();
			}
		}

		async Task<List<DatabaseRow>> ReaderToListAsync(DbDataReader reader)
		{
			List<DatabaseRow> result = new List<DatabaseRow>();
			while (await reader.ReadAsync())
			{
				List<object> data = new List<object>();

				for (int i = 0; i < reader.FieldCount; i++)
					data.Add(reader[i]);

				result.Add(new DatabaseRow(data));
			}

			return result;
		}

		List<DatabaseRow> ReaderToList(DbDataReader reader)
		{
			List<DatabaseRow> result = new List<DatabaseRow>();
			while (reader.Read())
			{
				List<object> data = new List<object>();

				for (int i = 0; i < reader.FieldCount; i++)
					data.Add(reader[i]);

				result.Add(new DatabaseRow(data));
			}

			return result;
		}

		MySqlConnection GetConnection(string database)
		{
			string connectionString = $"Server={Config.Server}; database={database}; UID={Config.Name}; password={Config.Password};";
			return new MySqlConnection(connectionString);
		}
	}
}

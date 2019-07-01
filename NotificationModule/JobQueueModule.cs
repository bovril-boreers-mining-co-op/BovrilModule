using Discord.WebSocket;
using Newtonsoft.Json;
using NModule.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Enums;

namespace NModule
{
	public class JobQueueModule : YModule
	{
		private JobQueueAsync JobQueue { get; set; }

		JsonSerializerSettings jsonSettings = new JsonSerializerSettings()
		{
			TypeNameHandling = TypeNameHandling.Objects,
			TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
		};

		protected override async Task Init()
		{
			JobQueue = new JobQueueAsync();

			// Load previous jobqueu if it exists.
			if (await ExistsAsync("JobQueue"))
				JobQueue = await LoadAsync("JobQueue", x => JsonConvert.DeserializeObject<JobQueueAsync>(x, jsonSettings));
		}

		public void RegisterJob<T>(Job<T> job) where T : IJob
		{
			JobQueue.RegisterJob(job);
		}

		public T GetJob<T>(Func<T, bool> predicate) where T : IJob
		{
			return JobQueue.GetJob(predicate);
		}

		public List<T> GetJobs<T>()
		{
			return JobQueue.GetJobs<T>();
		}

		public async Task<bool> RemoveJob<T>(T job) where T : IJob
		{
			bool succsess = JobQueue.RemoveJob(job);
			await SaveJobQueue();
			return succsess;
		}

		public async Task<bool> RemoveJob(int job)
		{
			bool succsess = JobQueue.RemoveJob(job);
			await SaveJobQueue();
			return succsess;
		}

		public async Task AddJob<T>(T job) where T : IJob
		{
			JobQueue.AddJob(job);
			await SaveJobQueue();
		}

		async Task SaveJobQueue()
		{
			await SaveAsync("JobQueue", JobQueue, ".json",
				x => JsonConvert.SerializeObject(x, Formatting.None, jsonSettings));
		}
	}
}

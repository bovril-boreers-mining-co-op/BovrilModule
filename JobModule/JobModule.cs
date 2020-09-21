using Discord;
using Discord.WebSocket;
using Modules.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;

namespace Modules
{
	[Config(typeof(JobModuleConfig))]
	public class JobModule : YModule
	{
		public new JobModuleConfig Config
		{
			get
			{
				return (JobModuleConfig)base.Config;
			}
		}

		private JobQueueAsync JobQueue { get; set; }

		bool loaded = false;

		protected override Task Init()
		{
			return InitJobQueueAsync();
		}

		#region Commands

		/// <summary>
		/// List all currently active jobs.
		/// </summary>
		/// <returns></returns>
		[Summary("List all currently active jobs.")]
		[Command("jobs")]
		public Task ListJobs()
		{
			StringBuilder reply = new StringBuilder();
			reply.Append($"Time is: {DateTime.UtcNow}\n");
			reply.Append($"Next: Job createed by '{JobQueue.CurrentWait.Creator}' will trigger on {JobQueue.GetNextCall(JobQueue.CurrentWait)}\n");
			reply.Append($"\nJobs:\n");

			List<IJob> jobs = JobQueue.GetJobs();
			for (int i = 0; i < jobs.Count; i++)
			{
				IJob job = jobs[i];
				reply.Append($"{i}: Job createed by '{job.Creator}' will trigger on {JobQueue.GetNextCall(job)} is {(job.Repeat ? "" : "not ")}repeatable and of type {job.GetType().Name}.\n");
			}

			if (reply.Length <= 1)
				return RespondAsync("No jobs queued", false, false);

			return RespondAsync($"```{reply.ToString()[0..(reply.Length - 1)]}```", false, false);
		}

		/// <summary>
		/// List all registerd job types.
		/// </summary>
		/// <returns></returns>
		[Summary("List all registerd job types.")]
		[Command("jobs", "types", "list")]
		public Task ListJobTypes()
		{
			StringBuilder reply = new StringBuilder();

			List<Type> types = JobQueue.GetRegisterdTypes();
			for (int i = 0; i < types.Count; i++)
			{
				reply.Append($"{i}: {types[i]}\n");
			}

			if (reply.Length <= 0)
				return RespondAsync("No types registerd.", false, false);

			return RespondAsync($"```{reply}```", false, false);
		}

		/// <summary>
		/// Manually trigger a job to execute.
		/// </summary>
		/// <param name="job"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		[Summary("Manually trigger a job to execute.")]
		[Command("jobs", "trigger")]
		public async Task Trigger(
			[Summary("Id of job to trigger.")]int job,
			[Summary("IF you want to treat it like an actual job trigger.")]JobTriggerType type = JobTriggerType.Soft)
		{
			List<IJob> jobs = JobQueue.GetJobs();
			if (job < 0 || job >= jobs.Count)
				await RespondAsync("Job ID out of bounds.", false, false);

			// Ensure channel context remains
			ISocketMessageChannel channel = Channel;
			try
			{
				await RespondAsync("Job found, executing...", false, false);
				await JobQueue.TriggerJob(jobs[job], type == JobTriggerType.Soft);
				await SaveAsync("Jobs", JobQueue.Jobs.AsEnumerable(), ".json", JobSerializer);

				SetContext(new MethodContext(Guild, channel, Message));
				await RespondAsync("Job complete.", false, false);
			}
			catch (Exception)
			{
				SetContext(new MethodContext(Guild, channel, Message));
				throw;
			}
		}

		/// <summary>
		/// Remve a job
		/// </summary>
		/// <param name="job"></param>
		/// <returns></returns>
		[Summary("Remove a job.")]
		[Command("jobs", "remove")]
		public async Task Remove(
			[Summary("Id of job to trigger.")] int job)
		{
			if (JobQueue.RemoveJob(job))
			{
				await SaveAsync("Jobs", JobQueue.Jobs.AsEnumerable(), ".json", JobSerializer);
				await RespondAsync("Job removed.", false, false);
				return;
			}

			await RespondAsync("Unable to remove job.", false, false);
		}

		/// <summary>
		/// Show the current UTC time.
		/// </summary>
		/// <returns></returns>
		[Summary("shows the current UTC time")]
		[Command("time")]
		public Task Time()
		{
			return RespondAsync($"{DateTime.UtcNow}", false, false);
		}

		/// <summary>
		/// Trigger a process queueu update on the job queue.
		/// </summary>
		/// <returns></returns>
		[Summary("Trigger a process queueu update on the job queue.")]
		[Command("jobs", "process")]
		public Task Process()
		{
			JobQueue.Trigger();
			return Task.CompletedTask;
		}

		#endregion

		/// <summary>
		/// Register a job delegate for type. Context is samepeled.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="job"></param>
		public Task RegisterJobAsync<T>(Job<T> job) where T : IJob
		{
			MethodContext context = new MethodContext(Guild, Channel, Message);
			return RegisterJobAsync(job, context);
		}
		
		/// <summary>
		/// Register a job delegate for type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="job"></param>
		/// <param name="context"></param>
		public async Task RegisterJobAsync<T>(Job<T> job, MethodContext context) where T : IJob
		{
			if (!loaded)
				await InitJobQueueAsync();

			JobQueue.RegisterJob(job, context);
		}

		/// <summary>
		/// Add a new job of type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="job"></param>
		public Task AddJobAsync<T>(T job) where T : IJob
		{
			return AddJobAsync(job, DateTime.UtcNow);
		}

		/// <summary>
		/// Add a new job of type with a specific start time.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="job"></param>
		/// <param name="jobStart"></param>
		public async Task AddJobAsync<T>(T job, DateTime jobStart) where T : IJob
		{
			if (!loaded)
				await InitJobQueueAsync();

			JobQueue.AddJob(job, jobStart);
			await SaveAsync("Jobs", JobQueue.Jobs.AsEnumerable(), ".json", JobSerializer);
		}

		/// <summary>
		/// Get job that matches a predicate.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public async Task<T> GetJobAsync<T>(Func<T, bool> predicate) where T : IJob
		{
			if (!loaded)
				await InitJobQueueAsync();

			return JobQueue.GetJob(predicate);
		}

		/// <summary>
		/// Check if any job of type exists that matches predicated
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public async Task<bool> JobExistsAsync<T>(Func<T, bool> predicate) where T : IJob
		{
			return await GetJobAsync(predicate) != null;
		}

		/// <summary>
		/// Get all jobs of type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public async Task<List<T>> GetJobsAsync<T>() where T : IJob
		{
			if (!loaded)
				await InitJobQueueAsync();

			return JobQueue.GetJobs<T>(x => true);
		}

		/// <summary>
		/// Get all jobs of type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public async Task<List<T>> GetJobsAsync<T>(Func<T, bool> predicate) where T : IJob
		{
			if (!loaded)
				await InitJobQueueAsync();

			return JobQueue.GetJobs<T>(predicate);
		}

		/// <summary>
		/// Get the next time this job will fire.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="job"></param>
		/// <returns></returns>
		public DateTime GetNextCall<T>(T job) where T : IJob
		{
			return JobQueue.GetNextCall(job);
		}

		/// <summary>
		/// Remove a job.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="job"></param>
		/// <returns></returns>
		public async Task<bool> Remove<T>(T job) where T : IJob
		{
			bool succsess = JobQueue.RemoveJob(job);
			if (succsess)
				await SaveAsync("Jobs", JobQueue.Jobs.AsEnumerable(), ".json", JobSerializer);

			return succsess;
		}

		async Task InitJobQueueAsync()
		{
			if (!loaded)
			{
				await LogAsync(YahurrFramework.Enums.LogLevel.Message, $"Initializing {this.GetType().Name}...");

				if (await ExistsAsync("Jobs"))
					JobQueue = new JobQueueAsync(await LoadAsync("Jobs", JsonDeserializer));
				else
					JobQueue = new JobQueueAsync();

				loaded = true;
			}
		}

		string JobSerializer(object obj)
		{
			return JsonConvert.SerializeObject(obj, new JsonSerializerSettings()
			{
				TypeNameHandling = TypeNameHandling.Auto,
			});
		}

		IEnumerable<InternalJob> JsonDeserializer(string json)
		{
			return JsonConvert.DeserializeObject<IEnumerable<InternalJob>>(json, new JsonSerializerSettings()
			{
				TypeNameHandling = TypeNameHandling.Auto,
			});
		}
	}
}

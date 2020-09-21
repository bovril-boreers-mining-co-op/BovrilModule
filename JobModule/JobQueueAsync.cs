using Modules;
using Modules.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YahurrFramework;

namespace Modules
{
	/// <summary>
	/// Job delegate
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="item"></param>
	public delegate Task Job<T>(T item, MethodContext context) where T : IJob;

	internal class JobQueueAsync
	{
		public SortedSet<InternalJob> Jobs { get; }

		public IJob CurrentWait
		{
			get
			{
				return currentWait.Job;
			}
		}

		private ConcurrentDictionary<Type, Job<IJob>> JobMethods { get; }

		private ConcurrentDictionary<Type, MethodContext> JobContext { get; }

		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
		InternalJob currentWait;
		bool loopRunning = false;

		public JobQueueAsync()
		{
			JobMethods = new ConcurrentDictionary<Type, Job<IJob>>();
			JobContext = new ConcurrentDictionary<Type, MethodContext>();
			Jobs = new SortedSet<InternalJob>(new JobComparer());
		}

		public JobQueueAsync(IEnumerable<InternalJob> jobs) : this()
		{
			foreach (InternalJob job in jobs)
				Jobs.Add(job);

			// Start job processing loop so it can deal with saved jobs
			// The loop will start in another thread so user can still use RegisterJob
			TryStartProcessLoop();
		}

		/// <summary>
		/// Register job type so it can be used later
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="job"></param>
		public void RegisterJob<T>(Job<T> job, MethodContext context) where T : IJob
		{
			JobMethods[typeof(T)] = (o, c) => job((T)o, c);
			JobContext[typeof(T)] = context;
		}

		/// <summary>
		/// Get all registerd job types.
		/// </summary>
		/// <returns></returns>
		public List<Type> GetRegisterdTypes()
		{
			return JobMethods.Select(x => x.Key).ToList();
		}

		/// <summary>
		/// Add Job of type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="job"></param>
		public void AddJob<T>(T job, DateTime jobStart) where T : IJob
		{
			if (!JobMethods.ContainsKey(typeof(T)))
				throw new Exception($"No job registerd for type {typeof(T)}");

			lock (Jobs)
			{
				Jobs.Add(new InternalJob(job, jobStart));
				TryStartProcessLoop();
				TriggerInterupt();
			}
		}

		/// <summary>
		/// Manually trigger a job to execute
		/// </summary>
		/// <param name="job"></param>
		/// <returns></returns>
		public Task TriggerJob(IJob job, bool soft)
		{
			InternalJob internalJob = Jobs.Single(x => x.Job == job);
			return ExecuteJob(internalJob, soft);
		}

		/// <summary>
		/// Remove job og type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="job"></param>
		/// <returns></returns>
		public bool RemoveJob<T>(T job) where T : IJob
		{
			lock (Jobs)
			{
				if (Jobs.RemoveWhere(x => x.Job == (IJob)job) > 0)
				{
					TriggerInterupt();

					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Remove job at index.
		/// </summary>
		/// <param name="job"></param>
		/// <returns></returns>
		public bool RemoveJob(int job)
		{
			if (job >= Jobs.Count)
				return false;

			lock (Jobs)
			{
				return Jobs.Remove(Jobs.ElementAt(job));
			}
		}

		/// <summary>
		/// Get job of type that fufills predicate
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public T GetJob<T>(Func<T, bool> predicate) where T : IJob
		{
			lock (Jobs)
			{
				InternalJob internalJob = Jobs.FirstOrDefault(x => x.Job is T t && predicate(t));
				return internalJob == null ? default : (T)internalJob.Job;
			}
		}

		/// <summary>
		/// Get a list of all jobs of type.
		/// </summary>
		/// <returns></returns>
		public List<T> GetJobs<T>(Func<T, bool> predicate)
		{
			return Jobs.Where(x => x.Job.GetType() == typeof(T) && predicate((T)x.Job))
				.Select(x => (T)x.Job)
				.ToList();
		}

		/// <summary>
		/// Get a list of all jobs
		/// </summary>
		/// <returns></returns>
		public List<IJob> GetJobs()
		{
			return Jobs.Select(x => x.Job).ToList();
		}

		/// <summary>
		/// Get next time this job will fire.
		/// </summary>
		/// <returns></returns>
		public DateTime GetNextCall(IJob job)
		{
			return Jobs.Single(x => x.Job == job).GetEnd();
		}

		/// <summary>
		/// Trigger an updated to the job process queue.
		/// </summary>
		public void Trigger()
		{
			TriggerInterupt();
		}

		/// <summary>
		/// Process all added jobs, including incomming while waiting
		/// </summary>
		/// <returns></returns>
		async Task ProcessQueue()
		{
			while (true)
			{
				if (Jobs.Count == 0)
					break;

				lock (Jobs)
				{
					currentWait = Jobs.Min;
				}

				bool jobCompleted = await WaitForJobOrInterupt(currentWait);
				if (jobCompleted)
					await TryExecuteJob(currentWait);
			}

			loopRunning = false;
		}

		/// <summary>
		/// Try to execute a job, all errors will be silent.
		/// </summary>
		/// <param name="job"></param>
		/// <returns></returns>
		async Task TryExecuteJob(InternalJob job)
		{
			lock (Jobs)
			{
				if (!job.Repeat)
					Jobs.Remove(job);
				else
					job.Restart();
			}

			try
			{
				MethodContext context = JobContext[job.Job.GetType()];
				await JobMethods[job.Job.GetType()].Invoke(job.Job, context);
			}
			catch (Exception)
			{
				
			}
		}

		/// <summary>
		/// Execute a job, exceptions will propegate.
		/// </summary>
		/// <param name="job"></param>
		/// <returns></returns>
		Task ExecuteJob(InternalJob job, bool soft)
		{
			if (!soft)
			{
				lock (Jobs)
				{
					if (!job.Repeat)
						Jobs.Remove(job);
					else
						job.Restart();
				}
			}

			MethodContext context = JobContext[job.Job.GetType()];
			return JobMethods[job.Job.GetType()].Invoke(job.Job, context);
		}

		/// <summary>
		/// Wait for job to complete or a semaphore interupt.
		/// </summary>
		/// <param name="job"></param>
		/// <returns></returns>
		async Task<bool> WaitForJobOrInterupt(InternalJob job)
		{
			Task jobWaitTask = Task.Delay(0);
			Task waitTask = tcs.Task;

			if (job.GetEnd() > DateTime.UtcNow)
				jobWaitTask = Task.Delay(job.GetEnd() - DateTime.UtcNow);

			Task doneTask = await Task.WhenAny(waitTask, jobWaitTask);
			if (doneTask == waitTask)
			{
				lock (tcs)
				{
					tcs = new TaskCompletionSource<bool>();
				}
			}

			return doneTask == jobWaitTask;
		}

		/// <summary>
		/// Trigger an interupt for the job queue that is currently waiting for a job to finish.
		/// </summary>
		void TriggerInterupt()
		{
			lock (tcs)
			{
				tcs.SetResult(true);
			}
		}

		void TryStartProcessLoop()
		{
			if (!loopRunning && Jobs.Count > 0)
			{
				loopRunning = true;
				Task.Factory.StartNew(
					x => ProcessQueue(),
					TaskContinuationOptions.LongRunning,
					CancellationToken.None
				);
			}
		}
	}

	class JobComparer : IComparer<InternalJob>
	{
		public int Compare(InternalJob x, InternalJob y)
		{
			return x.GetEnd().CompareTo(y.GetEnd());
		}
	}
}

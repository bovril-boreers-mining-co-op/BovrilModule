using Newtonsoft.Json;
using NModule.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NModule
{
	/// <summary>
	/// Job delegate
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="item"></param>
	delegate Task Job<T>(T item) where T : IJob;

	class JobQueueAsync
	{
		private Dictionary<Type, Job<IJob>> JobMethods { get; }

		[JsonProperty]
		private SortedSet<IJob> Jobs { get; }

		SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);
		bool loopRunning = false;

		public JobQueueAsync()
		{
			JobMethods = new Dictionary<Type, Job<IJob>>();
			Jobs = new SortedSet<IJob>(new JobComparer());
		}

		[JsonConstructor]
		private JobQueueAsync(SortedSet<IJob> jobs)
		{
			JobMethods = new Dictionary<Type, Job<IJob>>();
			Jobs = jobs;

			// Start job processing loop so it can deal with saved jobs
			// The loop will start in another thread so user can still use RegisterJob
			TryStartProcessLoop();
		}

		/// <summary>
		/// Register job so it can be used later
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="job"></param>
		public void RegisterJob<T>(Job<T> job) where T : IJob
		{
			lock (JobMethods)
			{
				JobMethods[typeof(T)] = o => job((T)o);
			}
		}

		/// <summary>
		/// Add Job of type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="job"></param>
		public void AddJob<T>(T job) where T : IJob
		{
			lock (JobMethods)
			{
				if (!JobMethods.ContainsKey(typeof(T)))
					throw new Exception($"No job registerd for type {typeof(T)}");
			}

			lock (Jobs)
			{
				Jobs.Add(job);

				TryStartProcessLoop();
			}

			CheckSemaphore();
		}

		/// <summary>
		/// Remove job og type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="job"></param>
		/// <returns></returns>
		public bool RemoveJob<T>(T job) where T : IJob
		{
			if (Jobs.Remove(job))
			{
				CheckSemaphore();

				return true;
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

			return RemoveJob(Jobs.ElementAt(job));
		}

		/// <summary>
		/// Get job of type that fufills predicate
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public T GetJob<T>(Func<T, bool> predicate) where T : IJob
		{
			return (T)Jobs.First(x => x is T && predicate((T)x));
		}

		/// <summary>
		/// Get a list of all jobs
		/// </summary>
		/// <returns></returns>
		public List<T> GetJobs<T>()
		{
			return Jobs.Where(x => x.GetType() == typeof(T))
				.Select(x => (T)x)
				.ToList();
		}

		/// <summary>
		/// Process all added jobs, including incomming while waiting
		/// </summary>
		/// <param name="items"></param>
		/// <returns></returns>
		async Task ProcessQueue(object items)
		{
			while (true)
			{
				IJob job;
				lock (Jobs)
				{
					if (Jobs.Count == 0)
						break;

					job = Jobs.First();
				}

				Task waitTask = Task.Delay(0);
				Task jobWaitTask = Task.Delay(0);

				lock (semaphore)
					waitTask = semaphore.WaitAsync();

				if (job.Start.CompareTo(DateTime.UtcNow) == 1)
					jobWaitTask = Task.Delay(job.Start - DateTime.UtcNow);

				await Task.WhenAny(waitTask, jobWaitTask);

				if (jobWaitTask.IsCompleted)
				{
					lock (Jobs)
						Jobs.Remove(job);

					try
					{
						lock (JobMethods)
							JobMethods[job.GetType()].Invoke(job);
					}
					catch (Exception)
					{

					}
				}
			}

			loopRunning = false;
		}

		/// <summary>
		/// Check the semaphore if the queue is currently waiting for a job to finish.
		/// </summary>
		void CheckSemaphore()
		{
			lock (semaphore)
			{
				if (semaphore.CurrentCount == 0)
					semaphore.Release(1);
			}
		}

		void TryStartProcessLoop()
		{
			if (!loopRunning && Jobs.Count > 0)
			{
				loopRunning = true;
				Task.Factory.StartNew(
					ProcessQueue,
					TaskContinuationOptions.LongRunning,
					CancellationToken.None
				);
			}
		}
	}

	class JobComparer : IComparer<IJob>
	{
		public int Compare(IJob x, IJob y)
		{
			return x.Start.CompareTo(y.Start);
		}
	}
}

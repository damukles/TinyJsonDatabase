using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BridgeDaemon
{
    public static class Eternity
    {
        private static IServiceProvider _serviceProvider = null;
        private static Timer _timer = null;
        private static List<JobDescriptor> _allJobs = new List<JobDescriptor>();
        private static List<JobDescriptor> _runningJobs = new List<JobDescriptor>();

        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public static void AddJob<T>(object jobStartParam = null) where T : IJob
        {
            // TODO: Handle case when someone adds a Job after Start()

            var jobDescriptor = CreateJob(new JobDescriptor()
            {
                Guid = Guid.NewGuid(),
                CancellationTokenSource = new CancellationTokenSource(),
                Type_ = typeof(T),
                Param = jobStartParam
            });

            AddToAllJobs(jobDescriptor);
            AddToRunningJobs(jobDescriptor);
        }

        public static void Start(int intervalSeconds = 60)
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("Eternity must be initialized first.");

            var intervalMs = intervalSeconds * 1000;
            var allTasks = new Task[0];

            lock (_runningJobs)
            {
                allTasks = _runningJobs.Select(x => x.Task).ToArray();
            }

            foreach (var task in allTasks)
                task.Start();

            _timer = new Timer(TimerCallback, null, intervalMs, intervalMs);
        }

        public static void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            var ctses = new CancellationTokenSource[0];
            var runningTasks = new Task[0];

            lock (_runningJobs)
            {
                ctses = _runningJobs.Select(x => x.CancellationTokenSource).ToArray();
                runningTasks = _runningJobs.Select(x => x.Task).ToArray();
            }

            foreach (var cts in ctses)
                cts.Cancel();

            Task.WaitAll(runningTasks);
        }

        private static void AddToAllJobs(JobDescriptor jobDescriptor)
        {
            lock (_allJobs)
            {
                _allJobs.Add(jobDescriptor);
            }
        }

        private static void AddToRunningJobs(JobDescriptor jobDescriptor)
        {
            lock (_runningJobs)
            {
                _runningJobs.Add(jobDescriptor);
            }
        }

        private static void ReplaceInAllJob(JobDescriptor jobDescriptor)
        {
            lock (_allJobs)
            {
                _allJobs.Remove(_allJobs.SingleOrDefault(x => x.Guid == jobDescriptor.Guid));
                _allJobs.Add(jobDescriptor);
            }
        }

        private static void TimerCallback(object state) => Monitor();

        private static void Monitor()
        {
            Log.Information("Monitoring Jobs");

            var jobs = new JobDescriptor[0];

            lock (_allJobs)
            {
                jobs = _allJobs.ToArray();
            }

            foreach (var job in jobs)
                CheckRunning(job);
        }

        private static void CheckRunning(JobDescriptor job)
        {
            var existingJob = _runningJobs.SingleOrDefault(x => x.Guid == job.Guid);
            if (existingJob != null)
            {
                if (existingJob.Task == null
                    || existingJob.Task.IsFaulted || existingJob.Task.IsCanceled
                    || existingJob.Task.IsCompleted || existingJob.Task.IsCompletedSuccessfully)
                {
                    // Task is present but null or not running (anymore)
                    RemoveJobFromRunning(existingJob);
                }
                else
                {
                    // Task is in place and still running
                    return;
                }

            }

            // Task is not present, start Job again
            RestartJob(job);
        }

        private static void RestartJob(JobDescriptor oldDescriptor)
        {
            oldDescriptor.CancellationTokenSource = new CancellationTokenSource();
            var jobDescriptor = CreateJob(oldDescriptor);

            ReplaceInAllJob(jobDescriptor);

            AddToRunningJobs(jobDescriptor);

            // Start the actual Task
            jobDescriptor.Task.Start();
        }

        private static JobDescriptor CreateJob(JobDescriptor jobDescriptor)
        {
            var task = new Task(() =>
                {
                    try
                    {
                        using (var scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
                        {
                            var job = (IJob)ActivatorUtilities.CreateInstance(scope.ServiceProvider, jobDescriptor.Type_);

                            using (var ctr = jobDescriptor.CancellationTokenSource.Token.Register(() =>
                            {
                                // Stop if Task is cancelled
                                job.Stop();
                                RemoveJobFromRunning(jobDescriptor);
                            }))
                            {
                                job.Start(jobDescriptor.Param);
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Error running an IJob in background.");
                    }
                    finally
                    {
                        // Remove if Task fails or ends
                        RemoveJobFromRunning(jobDescriptor);
                    }

                }, jobDescriptor.CancellationTokenSource.Token);

            jobDescriptor.Task = task;

            return jobDescriptor;
        }

        private static void RemoveJobFromRunning(JobDescriptor taskDescriptor)
        {
            lock (_runningJobs)
            {
                taskDescriptor.CancellationTokenSource.Dispose();
                _runningJobs.Remove(taskDescriptor);
            }
        }
    }

    public class JobDescriptor
    {
        public Guid Guid { get; set; }
        public Type Type_ { get; set; }
        public object Param { get; set; }
        public Task Task { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
    }
}

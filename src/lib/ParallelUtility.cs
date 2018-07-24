using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace docs.host
{
    public class ParallelUtility
    {
        public static async Task ParallelForEach<T>(IEnumerable<T> resources, Func<T, Task> action, ExecutionDataflowBlockOptions options, Action<int, int> progress = null)
        {
            var done = 0;
            var total = resources.Count();
            var actions = new ActionBlock<T>(Run, options);

            foreach (var resource in resources)
            {
                await actions.SendAsync(resource);
            }

            actions.Complete();
            await actions.Completion;

            async Task Run(T item)
            {
                await action(item);
                progress?.Invoke(Interlocked.Increment(ref done), total);
            }
        }

        public static Task ParallelForEach<T>(IEnumerable<T> resources, Func<T, Task> action, int maxDegreeOfParallelism, int boundedCapacity, Action<int, int> progress = null)
        {
            return ParallelForEach(resources, action, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                SingleProducerConstrained = true,
                BoundedCapacity = boundedCapacity,
            }, progress);
        }
    }
}

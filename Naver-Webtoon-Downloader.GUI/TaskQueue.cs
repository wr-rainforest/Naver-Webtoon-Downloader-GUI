using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;

namespace NaverWebtoonDownloader.GUI
{
    class TaskQueue
    {
        private List<Task> tasks = new List<Task>();

        public event Action CollectionChanged;

        public int IndexOf(Task task)
        {
            return tasks.IndexOf(task);
        }

        public Task Enqueue(Func<Task> func, CancellationToken ct)
        {
            Task task = null;
            task = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(100);
                    if (ct.IsCancellationRequested)
                    {
                        tasks.Remove(task);
                        ct.ThrowIfCancellationRequested();
                    }
                    int indexOfTasks = tasks.IndexOf(task);
                    if (indexOfTasks == 0)
                        break;
                }
                try
                {
                    await func();
                }
                catch
                {
                    throw;
                }
                finally
                {
                    tasks.Remove(task);
                }
            });
            tasks.Add(task);
            CollectionChanged?.Invoke();
            return task;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NaverWebtoonDownloader.GUI
{
    class TaskManager
    {
        private List<Guid> keys = new List<Guid>();
        private Dictionary<Guid, (Action action, CancellationToken ct)> keyValuePairs = new Dictionary<Guid, (Action, CancellationToken)>();

        public event Action IndexChangedEvent;
        Thread refreshThread;

        public TaskManager(Thread main)
        {
            refreshThread = new Thread(async () =>
            {
                while (main.IsAlive)
                {
                    if(keys.Count==0)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    var guid = keys[0];
                    var action = keyValuePairs[guid].action;
                    keyValuePairs.Remove(guid);
                    keys.Remove(guid);
                    IndexChangedEvent?.Invoke();
                    var task = Task.Run(action);
                    Refresh(task);
                    await task;
                }
            });
            refreshThread.Start();
        }

        private async Task Refresh(Task task)
        {
            while (!task.IsCompleted)
            {
                await Task.Delay(70); 
                foreach (var key in keys)
                {
                    var tuple = keyValuePairs[key];
                    if (tuple.ct.IsCancellationRequested)
                    {
                        tuple.action?.Invoke();
                        keyValuePairs.Remove(key);
                        keys.Remove(key);
                        IndexChangedEvent?.Invoke();
                    }
                }
            }
        }

        public Guid Register(Action action , CancellationToken ct)
        {
            var guid = Guid.NewGuid();
            keys.Add(guid);
            keyValuePairs.Add(guid, (action, ct));
            return guid;
        }

        public int IndexOf(Guid guid)
        {
            return keys.IndexOf(guid);
        }
    }
}

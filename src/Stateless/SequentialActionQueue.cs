using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Stateless
{
    /// <summary>Executions queued actions in sequence (but still asynchronously).</summary>
    public class SequentialActionQueue : INotifyPropertyChanged
    {
        private static object syncLock = new object();
        private Task firingTask = null;
        private ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();
        private bool isActive = false;

        /// <summary></summary>
        public SequentialActionQueue()
        {
        }

        /// <summary>Raised when a property such as <see cref="IsActive"/> is changed.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>This property is true when the queue is processing a queued task.</summary>
        public bool IsActive
        {
            get { return this.isActive; }
            set
            {
                if (this.isActive != value)
                {
                    this.isActive = value;
                    this.OnPropertyChanged("IsActive");
                }
            }
        }

        /// <summary>Thread-safe method to synchronize the asynchronous execution of the submitted action.</summary>
        /// <param name="todo">The action to execute asynchronously.</param>
        public void Enqueue(Action todo)
        {
            queue.Enqueue(todo);

            lock (syncLock)
            {
                if (firingTask == null || firingTask.IsCompleted)
                {
                    firingTask = Task.Factory.StartNew(() =>
                    {
                        this.IsActive = true;

                        Action result;
                        while (queue.TryDequeue(out result))
                        {
                            result();
                        }

                        this.IsActive = false;
                    });
                }
            }
        }

        private void OnPropertyChanged(string propName)
        {
            var e = this.PropertyChanged;
            if (e != null)
            {
                e(this, new PropertyChangedEventArgs(propName));
            }
        }
    }

}

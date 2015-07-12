using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public static class TaskUtils
    {
        public static void DoNotWait(this Task task)
        {
            // Do nothing, let the task continue.  Eliminates compiler warning about non-awaited tasks in an async method.
        }

        public static async Task RunOnUIThreadAsync(Action action)
        {
            var app = App.Current;
            if(app != null)
            {
                await app.Dispatcher.InvokeAsync(action);
            }
        }

        public static readonly Task FinishedTask = Task.FromResult(true);
    }
}

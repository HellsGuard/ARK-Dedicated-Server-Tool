using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    /// <summary>
    /// Checks for an updates this program
    /// </summary>
    class AutoUpdater
    {
        const string StatusKey_Checking =    "AutoUpdater_Status_Checking";
        const string StatusKey_Downloading = "AutoUpdater_Status_Downloading";
        const string StatusKey_Complete =    "AutoUpdater_Status_Complete";
        const string StatusKey_Cancelled =   "AutoUpdater_Status_Cancelled";
        const string DefaultVersionURL = "https://www.dropbox.com/s/a6v1obnqigu2bpu/version.txt?dl=1";
        const string DefaultSourceURL = "http://hellsguard.site11.com/ARK_Server_Tool/updater.zip";

        public async void UpdateAsync(IProgress<Update> reporter, CancellationToken cancellationToken)
        {
            try
            {
                reporter.Report(new Update { Status = StatusKey_Checking, CompletionPercent = 0, canceled = false });

                await Task.Delay(1000, cancellationToken);

                reporter.Report(new Update { Status = StatusKey_Downloading, CompletionPercent = 10, canceled = false });

                await Task.Delay(1000, cancellationToken);

                reporter.Report(new Update { Status = StatusKey_Complete, CompletionPercent = 100, canceled = false });
            }
            catch(TaskCanceledException)
            {                
                reporter.Report(new Update { Status = StatusKey_Cancelled, CompletionPercent = 100 });
            }
        }

        public struct Update
        {
            public string Status;
            public float CompletionPercent;
            public bool canceled;
        }
    }
}

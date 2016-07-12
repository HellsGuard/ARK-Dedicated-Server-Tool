using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib.Model
{
    public class PublishedFileDetailsResult
    {
        public PublishedFileDetailsResponse response { get; set; }
    }

    public class PublishedFileDetailsResponse
    {
        public int result { get; set; }

        public int resultcount { get; set; }

        public List<PublishedFileDetail> publishedfiledetails { get; set; }

        public void PopulateExtended(string modRootFolder)
        {
            if (publishedfiledetails == null)
                return;

            Parallel.ForEach(publishedfiledetails, file => file.PopulateExtended(modRootFolder));
        }

        public void SetPublishedFileIndex()
        {
            if (publishedfiledetails == null)
                return;

            var index = 1;
            foreach (var file in publishedfiledetails)
                file.Index = index++;
        }
    }

    public class PublishedFileDetail
    {
        public string publishedfileid { get; set; }

        public int result { get; set; }

        public string creator { get; set; }

        public int creator_app_id { get; set; }

        public int consumer_app_id { get; set; }

        public string filename { get; set; }

        public string file_size { get; set; }

        public string file_url { get; set; }

        public string hcontent_file { get; set; }

        public string preview_url { get; set; }

        public string hcontent_preview { get; set; }

        public string title { get; set; }

        public string description { get; set; }

        public int time_created { get; set; }

        public int time_updated { get; set; }

        public int visibility { get; set; }

        public int banned { get; set; }

        public string ban_reason { get; set; }

        public int subscriptions { get; set; }

        public int favorited { get; set; }

        public int lifetime_subscriptions { get; set; }

        public int lifetime_favorited { get; set; }

        public int views { get; set; }

        public List<object> tags { get; set; }


        public int Index
        {
            get;
            set;
        }

        public string PublishedFileUrl
        {
            get
            {
                return $"http://steamcommunity.com/sharedfiles/filedetails/?id={publishedfileid}";
            }
        }

        public DateTime CreatedTime
        {
            get
            {
                return ModUtils.UnixTimeStampToDateTime(time_created);
            }
        }

        public DateTime UpdatedTime
        {
            get
            {
                return ModUtils.UnixTimeStampToDateTime(time_updated);
            }
        }

        public void PopulateExtended(string modRootFolder)
        {
            var modFolder = Path.Combine(modRootFolder, publishedfileid);
            if (string.IsNullOrWhiteSpace(modFolder) || !Directory.Exists(modFolder))
                return;

            var modFile = $"{modFolder}.mod";
            if (!string.IsNullOrWhiteSpace(modFile) && File.Exists(modFile))
            {
                LastUpdatedTime = File.GetLastWriteTime(modFile);
            }

            var modTimeFile = Path.Combine(modFolder, Config.Default.LastUpdatedTimeFile);
            if (!string.IsNullOrWhiteSpace(modTimeFile) && File.Exists(modTimeFile))
            {
                LastUpdatedVersion = ModUtils.GetModLatestTime(modTimeFile);
            }
        }

        public DateTime LastUpdatedTime
        {
            get;
            private set;
        }

        public string LastUpdatedTimeString
        {
            get
            {
                return LastUpdatedTime == DateTime.MinValue ? string.Empty : LastUpdatedTime.ToString();
            }
        }

        public string LastUpdatedTimeSortString
        {
            get
            {
                return LastUpdatedTime == DateTime.MinValue ? string.Empty : LastUpdatedTime.ToString("yyyyMMdd_HHmmss");
            }
        }

        public int LastUpdatedVersion
        {
            get;
            private set;
        }

        public bool UpToDate
        {
            get
            {
                return LastUpdatedVersion > 0 && LastUpdatedVersion == time_updated;
            }
        }
    }
}

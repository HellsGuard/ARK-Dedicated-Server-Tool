using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace ARK_Server_Manager.Lib.Model
{
    public class WorkshopFileDetailResult
    {
        public WorkshopFileDetailResponse response { get; set; }
    }

    [Serializable]
    public class WorkshopFileDetailResponse
    {
        public DateTime cached = DateTime.UtcNow;

        public int total { get; set; }

        public List<WorkshopFileDetail> publishedfiledetails { get; set; }

        public static WorkshopFileDetailResponse Load(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                return null;

            WorkshopFileDetailResponse result = null;

            using (var reader = File.OpenRead(file))
            {
                var serializer = new XmlSerializer(typeof(WorkshopFileDetailResponse));
                result = (WorkshopFileDetailResponse)serializer.Deserialize(reader);
            }

            return result;
        }

        public bool Save(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
                return false;

            using (var stream = File.Open(file, FileMode.Create))
            {
                var serializer = new XmlSerializer(this.GetType());
                serializer.Serialize(stream, this);
            }
            return true;
        }
    }

    [Serializable]
    public class WorkshopFileDetail
    {
        public int result { get; set; }

        public string publishedfileid { get; set; }

        public string creator { get; set; }

        public string creator_appid { get; set; }

        public string consumer_appid { get; set; }

        public int consumer_shortcutid { get; set; }

        public string filename { get; set; }

        public string file_size { get; set; }

        public string preview_file_size { get; set; }

        public string file_url { get; set; }

        public string preview_url { get; set; }

        public string url { get; set; }

        public string hcontent_file { get; set; }

        public string hcontent_preview { get; set; }

        public string title { get; set; }

        public string file_description { get; set; }

        public int time_created { get; set; }

        public int time_updated { get; set; }

        public int visibility { get; set; }

        public int flags { get; set; }

        public bool workshop_file { get; set; }

        public bool workshop_accepted { get; set; }

        public bool show_subscribe_all { get; set; }

        public int num_comments_developer { get; set; }

        public int num_comments_public { get; set; }

        public bool banned { get; set; }

        public string ban_reason { get; set; }

        public string banner { get; set; }

        public bool can_be_deleted { get; set; }

        public bool incompatible { get; set; }

        public string app_name { get; set; }

        public int file_type { get; set; }

        public bool can_subscribe { get; set; }

        public int subscriptions { get; set; }

        public int favorited { get; set; }

        public int followers { get; set; }

        public int lifetime_subscriptions { get; set; }

        public int lifetime_favorited { get; set; }

        public int lifetime_followers { get; set; }

        public int views { get; set; }

        public bool spoiler_tag { get; set; }

        public int num_children { get; set; }

        public int num_reports { get; set; }

        public int language { get; set; }

        public bool IsAdded { get; set; }
    }

    public class WorkshopFileList : ObservableCollection<WorkshopFileItem>
    {
        public DateTime CachedTime
        {
            get;
            set;
        }

        public new void Add(WorkshopFileItem item)
        {
            if (item == null || this.Any(m => m.WorkshopId.Equals(item.WorkshopId)))
                return;

            base.Add(item);
        }

        public static WorkshopFileList GetList(WorkshopFileDetailResponse response)
        {
            var result = new WorkshopFileList();
            result.CachedTime = response.cached.ToLocalTime();
            if (response.publishedfiledetails != null)
            {
                foreach (var detail in response.publishedfiledetails)
                {
                    result.Add(WorkshopFileItem.GetItem(detail));
                }
            }
            return result;
        }
    }

    public class WorkshopFileItem : DependencyObject
    {
        public static readonly DependencyProperty AppIdProperty = DependencyProperty.Register(nameof(AppId), typeof(string), typeof(WorkshopFileItem), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty TimeUpdatedProperty = DependencyProperty.Register(nameof(TimeUpdated), typeof(int), typeof(WorkshopFileItem), new PropertyMetadata(0));
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(WorkshopFileItem), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty WorkshopIdProperty = DependencyProperty.Register(nameof(WorkshopId), typeof(string), typeof(WorkshopFileItem), new PropertyMetadata(string.Empty));

        public string AppId
        {
            get { return (string)GetValue(AppIdProperty); }
            set { SetValue(AppIdProperty, value); }
        }

        public int TimeUpdated
        {
            get { return (int)GetValue(TimeUpdatedProperty); }
            set { SetValue(TimeUpdatedProperty, value); }
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set
            {
                SetValue(TitleProperty, value);

                TitleFilterString = value?.ToLower();
            }
        }

        public string WorkshopId
        {
            get { return (string)GetValue(WorkshopIdProperty); }
            set { SetValue(WorkshopIdProperty, value); }
        }


        public string TitleFilterString
        {
            get;
            private set;
        }

        public string WorkshopUrl => $"http://steamcommunity.com/sharedfiles/filedetails/?id={WorkshopId}";

        public static WorkshopFileItem GetItem(WorkshopFileDetail item)
        {
            if (string.IsNullOrWhiteSpace(item.publishedfileid) || string.IsNullOrWhiteSpace(item.title))
                return null;

            var result = new WorkshopFileItem();
            result.AppId = item.creator_appid;
            result.TimeUpdated = item.time_updated;
            result.Title = item.title ?? string.Empty;
            result.WorkshopId = item.publishedfileid ?? string.Empty;
            return result;
        }
    }
}

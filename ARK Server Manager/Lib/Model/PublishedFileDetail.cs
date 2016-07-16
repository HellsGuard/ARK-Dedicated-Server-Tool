using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

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
    }

    public class ModDetailList : ObservableCollection<ModDetail>
    {
        public bool AnyUnknownModTypes
        {
            get
            {
                return this.Any(m => !m.IsValidModType);
            }
        }

        public new void Add(ModDetail mod)
        {
            if (mod == null || this.Any(m => m.ModId.Equals(mod.ModId)))
                return;

            base.Add(mod);
            SetPublishedFileIndex();
        }

        public void AddRange(ModDetail[] mods)
        {
            foreach (var mod in mods)
            {
                if (mod == null || this.Any(m => m.ModId.Equals(mod.ModId)))
                    continue;
                base.Add(mod);
            }
            SetPublishedFileIndex();
        }

        public void MoveDown(ModDetail mod)
        {
            if (mod == null)
                return;

            var index = base.IndexOf(mod);
            if (index == base.Count - 1)
                return;

            base.Move(index, index + 1);
            SetPublishedFileIndex();
        }

        public void MoveUp(ModDetail mod)
        {
            if (mod == null)
                return;

            var index = base.IndexOf(mod);
            if (index == 0)
                return;

            base.Move(index, index - 1);
            SetPublishedFileIndex();
        }

        public void PopulateExtended(string modsRootFolder)
        {
            var results = new Dictionary<ModDetail, ModDetailExtended>();
            foreach (var mod in this)
            {
                results.Add(mod, new ModDetailExtended(mod.ModId));
            }

            Parallel.ForEach(results, kvp => kvp.Value.PopulateExtended(modsRootFolder));

            foreach (var kvp in results)
            {
                kvp.Key.PopulateExtended(kvp.Value);
            }
        }

        public new bool Remove(ModDetail mod)
        {
            if (mod == null)
                return false;

            var removed = base.Remove(mod);

            SetPublishedFileIndex();
            return removed;
        }

        public void SetPublishedFileIndex()
        {
            foreach (var mod in this)
            {
                mod.Index = base.IndexOf(mod) + 1;
                mod.IsFirst = false;
                mod.IsLast = false;
            }

            if (this.Count == 0)
                return;

            this[0].IsFirst = true;
            this[base.Count - 1].IsLast = true;
        }

        public bool GetModStrings(out string mapString, out string totalConversionString, out string modIdString)
        {
            mapString = null;
            totalConversionString = null;
            modIdString = string.Empty;

            var delimiter = "";
            foreach (var mod in this)
            {
                switch (mod.ModTypeString)
                {
                    case ModDetail.MODTYPE_MAP:
                        mapString = $"/Game/Mods/{mod.ModId}/{mod.MapName}";
                        break;
                    case ModDetail.MODTYPE_TOTCONV:
                        totalConversionString = mod.ModId;
                        break;
                    case ModDetail.MODTYPE_MAPEXT:
                    case ModDetail.MODTYPE_MOD:
                    case ModDetail.MODTYPE_UNKNOWN:
                        modIdString += $"{delimiter}{mod.ModId}";
                        delimiter = ",";
                        break;
                }
            }

            return true;
        }

        public static ModDetailList GetModDetails(PublishedFileDetailsResponse response, string modsRootFolder)
        {
            var result = new ModDetailList();
            if (response != null && response.publishedfiledetails != null)
            {
                foreach (var detail in response.publishedfiledetails)
                {
                    result.Add(ModDetail.GetModDetail(detail));
                }
                result.SetPublishedFileIndex();
                result.PopulateExtended(modsRootFolder);
            }
            return result;
        }
    }

    public class ModDetail : DependencyObject
    {
        public const string MODTYPE_UNKNOWN = "<unknown>";
        public const string MODTYPE_MAP = "Map";
        public const string MODTYPE_MAPEXT = "Map Extension";
        public const string MODTYPE_MOD = "Mod";
        public const string MODTYPE_TOTCONV = "Total Conversion";

        public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(nameof(Index), typeof(int), typeof(ModDetail), new PropertyMetadata(0));
        public static readonly DependencyProperty IsFirstProperty = DependencyProperty.Register(nameof(IsFirst), typeof(bool), typeof(ModDetail), new PropertyMetadata(false));
        public static readonly DependencyProperty IsLastProperty = DependencyProperty.Register(nameof(IsLast), typeof(bool), typeof(ModDetail), new PropertyMetadata(false));
        public static readonly DependencyProperty LastWriteTimeProperty = DependencyProperty.Register(nameof(LastWriteTime), typeof(DateTime), typeof(ModDetail), new PropertyMetadata(DateTime.MinValue));
        public static readonly DependencyProperty LastTimeUpdatedProperty = DependencyProperty.Register(nameof(LastTimeUpdated), typeof(int), typeof(ModDetail), new PropertyMetadata(0));
        public static readonly DependencyProperty ModIdProperty = DependencyProperty.Register(nameof(ModId), typeof(string), typeof(ModDetail), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty ModTypeProperty = DependencyProperty.Register(nameof(ModType), typeof(string), typeof(ModDetail), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty ModTypeStringProperty = DependencyProperty.Register(nameof(ModTypeString), typeof(string), typeof(ModDetail), new PropertyMetadata(MODTYPE_UNKNOWN));
        public static readonly DependencyProperty ModUrlProperty = DependencyProperty.Register(nameof(ModUrl), typeof(string), typeof(ModDetail), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty TimeUpdatedProperty = DependencyProperty.Register(nameof(TimeUpdated), typeof(int), typeof(ModDetail), new PropertyMetadata(0));
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(ModDetail), new PropertyMetadata(string.Empty));


        public int Index
        {
            get { return (int)GetValue(IndexProperty); }
            set { SetValue(IndexProperty, value); }
        }

        public bool IsFirst
        {
            get { return (bool)GetValue(IsFirstProperty); }
            set { SetValue(IsFirstProperty, value); }
        }

        public bool IsLast
        {
            get { return (bool)GetValue(IsLastProperty); }
            set { SetValue(IsLastProperty, value); }
        }

        public DateTime LastWriteTime
        {
            get { return (DateTime)GetValue(LastWriteTimeProperty); }
            set { SetValue(LastWriteTimeProperty, value); }
        }

        public int LastTimeUpdated
        {
            get { return (int)GetValue(LastTimeUpdatedProperty); }
            set { SetValue(LastTimeUpdatedProperty, value); }
        }

        public string ModId
        {
            get { return (string)GetValue(ModIdProperty); }
            set { SetValue(ModIdProperty, value); }
        }

        public string ModType
        {
            get { return (string)GetValue(ModTypeProperty); }
            set
            {
                SetValue(ModTypeProperty, value);
                SetModTypeString();
            }
        }

        public string ModTypeString
        {
            get { return (string)GetValue(ModTypeStringProperty); }
            set { SetValue(ModTypeStringProperty, value); }
        }

        public int TimeUpdated
        {
            get { return (int)GetValue(TimeUpdatedProperty); }
            set { SetValue(TimeUpdatedProperty, value); }
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }


        public bool IsValidModType
        {
            get
            {
                return !ModTypeString.Equals(MODTYPE_UNKNOWN);
            }
        }

        public string LastWriteTimeString
        {
            get
            {
                return LastWriteTime == DateTime.MinValue ? string.Empty : LastWriteTime.ToString();
            }
        }

        public string LastWriteTimeSortString
        {
            get
            {
                return LastWriteTime == DateTime.MinValue ? string.Empty : LastWriteTime.ToString("yyyyMMdd_HHmmss");
            }
        }

        public string MapName { get; set; }

        public string ModUrl
        {
            get
            {
                return $"http://steamcommunity.com/sharedfiles/filedetails/?id={ModId}";
            }
        }

        public bool UpToDate
        {
            get
            {
                return LastTimeUpdated > 0 && LastTimeUpdated == TimeUpdated;
            }
        }

        public void PopulateExtended(ModDetailExtended extended)
        {
            LastTimeUpdated = extended.LastTimeUpdated;
            LastWriteTime = extended.LastWriteTime;
            MapName = extended.MapName;
            ModType = extended.ModType;
        }

        public void SetModTypeString()
        {
            if (string.IsNullOrWhiteSpace(ModType))
                ModTypeString = MODTYPE_UNKNOWN;

            switch (ModType)
            {
                case "1":
                    ModTypeString = MODTYPE_MOD;
                    break;
                case "2":
                    ModTypeString = MODTYPE_MAP;
                    break;
                case "3":
                    ModTypeString = MODTYPE_TOTCONV;
                    break;
                case "4":
                    ModTypeString = MODTYPE_MAPEXT;
                    break;
                default:
                    ModTypeString = MODTYPE_UNKNOWN;
                    break;
            }
        }


        public static ModDetail GetModDetail(PublishedFileDetail detail)
        {
            var result = new ModDetail();
            result.ModId = detail.publishedfileid;
            result.TimeUpdated = detail.time_updated;
            result.Title = detail.title;
            return result;
        }

        public static ModDetail GetModDetail(WorkshopFileDetail detail)
        {
            var result = new ModDetail();
            result.ModId = detail.publishedfileid;
            result.TimeUpdated = detail.time_updated;
            result.Title = detail.title;
            return result;
        }

        public static ModDetail GetModDetail(WorkshopFileItem detail)
        {
            var result = new ModDetail();
            result.ModId = detail.WorkshopId;
            result.TimeUpdated = detail.TimeUpdated;
            result.Title = detail.Title;
            return result;
        }
    }

    public class ModDetailExtended
    {
        public ModDetailExtended(string modId)
        {
            ModId = modId;
        }

        public string MapName { get; set; }

        private string ModId { get; set; }

        public string ModType { get; set; }

        public DateTime LastWriteTime { get; set; }

        public int LastTimeUpdated { get; set; }

        public void PopulateExtended(string modsRootFolder)
        {
            var modFolder = Path.Combine(modsRootFolder, ModId);
            var modFile = $"{modFolder}.mod";

            if (string.IsNullOrWhiteSpace(modFolder) || !Directory.Exists(modFolder))
                return;
            if (string.IsNullOrWhiteSpace(modFile) || !File.Exists(modFile))
                return;

            LastWriteTime = File.GetLastWriteTime(modFile);

            var modTimeFile = Path.Combine(modFolder, Config.Default.LastUpdatedTimeFile);
            if (!string.IsNullOrWhiteSpace(modTimeFile) && File.Exists(modTimeFile))
            {
                LastTimeUpdated = ModUtils.GetModLatestTime(modTimeFile);
            }

            string modId;
            Dictionary<string, string> metaInformation;
            List<string> mapNames;
            ModUtils.ReadModFile(modFile, out modId, out metaInformation, out mapNames);

            ModType = metaInformation != null && metaInformation.ContainsKey("ModType") ? metaInformation["ModType"] : "0";
            MapName = mapNames != null && mapNames.Count > 0 ? mapNames[0] : string.Empty;
        }
    }
}

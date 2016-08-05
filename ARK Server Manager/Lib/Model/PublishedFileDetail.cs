using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

/*
{
	"response": {
		"result": 1,
		"resultcount": 7,
		"publishedfiledetails": [
			{
				"publishedfileid": "547377246",
				"result": 1,
				"creator": "76561198054051250",
				"creator_app_id": 346110,
				"consumer_app_id": 346110,
				"filename": "",
				"file_size": 1585909184,
				"file_url": "",
				"hcontent_file": "1259369762157134358",
				"preview_url": "http://images.akamai.steamusercontent.com/ugc/391045435769545035/9E418A9988F95E90DB66DA15AD74119361A9D161/",
				"hcontent_preview": "391045435769545035",
				"title": "Total Conversion: Ark Pirate World v6",
				"description": "[h1]Description[/h1]\r\n\r\nArk : Pirate World is a multiplayer and solo gamemode. \r\nPlay with your friends on a custom map, exchange objects with other player, on the main island to get gold. \r\nBuy items to npc, or buy your first boat. \r\nYou can make a big crew, to make naval battles against other tribes. \r\nGo on other islands to get rare resources and sell them, or build and protect your city on your island ! \r\nYou can also discover new places with your boat, find treasure maps... \r\nSo get on board !\r\n\r\n[h1]Gameplay[/h1]\r\n\r\nYou start on the main island, check your map and try to find the city. \r\nOnce you are at the city you can buy item to this seller : \r\nBoatShop (A large billboard on the deck), GunSmith, Miner, Saddle seller, Painter, BlackSmith, Farmer, Engineer, Banker.\r\nYou can find copper, silver, gold in the pirate chest, there is 30 chest in the first island. \r\nYou can also find treasure map, to find treasure chest with many copper, silver, gold, goldbag and coin blueprint ! \r\nThere is also supply drops, all around the map. \r\nIf you are on a server, you can exchange objects with others players. \r\nYou can build on the first island, and tame dinosaur (Yes yes, pirate and dinosaur :D !) \r\nOr you can buy your first raft or boat. \r\nMake a team is very important in ArkPirateWorld you can get much faster xp, resources, and gold!\r\nExplore the Ark PirateWorld map to find rare resources. \r\nSome resources, and dinosaur are only in a few island, be careful to carnivor and your temperature.\r\nBuild cannon for your boat, make weapons and equipments for your crew to defend yourself against other pirate !\r\nYou can grow new type of plants to make tobacco, wine, rum !\r\nYou have also new structure, barrel, table, wine press, tobacco dryer, bottling machine, coin press... \r\n\r\n[h1]Team[/h1]\r\n\r\n[b]Developer :[/b]\r\nDavidBC \r\n\r\n[h1]Partner[/h1]\r\n\r\n[b]Get your own Ark: Pirate World server with HostHavoc ![/b]\r\n[url=http://hosthavoc.com/billing/aff.php?aff=91][img]https://pirate.arkhungergames.com/mods/HostHavoc.png[/img][/url]",
				"time_created": 1446573019,
				"time_updated": 1454467708,
				"visibility": 0,
				"banned": 0,
				"ban_reason": "",
				"subscriptions": 39114,
				"favorited": 1879,
				"lifetime_subscriptions": 63775,
				"lifetime_favorited": 2130,
				"views": 158988,
				"tags": [
					{
						"tag": "Total Conversion"
					},
					{
						"tag": "Map "
					}
				]
				
			},
			{
				"publishedfileid": "630601751",
				"result": 1,
				"creator": "76561197963668880",
				"creator_app_id": 346110,
				"consumer_app_id": 346110,
				"filename": "",
				"file_size": 677757,
				"file_url": "",
				"hcontent_file": "9154659617306748900",
				"preview_url": "http://images.akamai.steamusercontent.com/ugc/270588452686877612/0BB7EE869E6B06DC9AE6878F3A46AF9FC22BA7BE/",
				"hcontent_preview": "270588452686877612",
				"title": "Resource Stacks",
				"description": "",
				"time_created": 1456177142,
				"time_updated": 1466815482,
				"visibility": 0,
				"banned": 0,
				"ban_reason": "",
				"subscriptions": 195316,
				"favorited": 759,
				"lifetime_subscriptions": 227458,
				"lifetime_favorited": 902,
				"views": 41482,
				"tags": [

				]
				
			}
		]
	}
}
*/

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

        public string creator_app_id { get; set; }

        public string consumer_app_id { get; set; }

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
                    case ModUtils.MODTYPENAME_MAP:
                        mapString = $"/Game/Mods/{mod.ModId}/{mod.MapName}";
                        break;
                    case ModUtils.MODTYPENAME_TOTCONV:
                        totalConversionString = mod.ModId;
                        break;
                    case ModUtils.MODTYPENAME_MAPEXT:
                    case ModUtils.MODTYPENAME_MOD:
                    case ModUtils.MODTYPENAME_UNKNOWN:
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
        public static readonly DependencyProperty AppIdProperty = DependencyProperty.Register(nameof(AppId), typeof(string), typeof(ModDetail), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(nameof(Index), typeof(int), typeof(ModDetail), new PropertyMetadata(0));
        public static readonly DependencyProperty IsFirstProperty = DependencyProperty.Register(nameof(IsFirst), typeof(bool), typeof(ModDetail), new PropertyMetadata(false));
        public static readonly DependencyProperty IsLastProperty = DependencyProperty.Register(nameof(IsLast), typeof(bool), typeof(ModDetail), new PropertyMetadata(false));
        public static readonly DependencyProperty LastWriteTimeProperty = DependencyProperty.Register(nameof(LastWriteTime), typeof(DateTime), typeof(ModDetail), new PropertyMetadata(DateTime.MinValue));
        public static readonly DependencyProperty LastTimeUpdatedProperty = DependencyProperty.Register(nameof(LastTimeUpdated), typeof(int), typeof(ModDetail), new PropertyMetadata(0));
        public static readonly DependencyProperty ModIdProperty = DependencyProperty.Register(nameof(ModId), typeof(string), typeof(ModDetail), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty ModTypeProperty = DependencyProperty.Register(nameof(ModType), typeof(string), typeof(ModDetail), new PropertyMetadata(ModUtils.MODTYPE_UNKNOWN));
        public static readonly DependencyProperty ModTypeStringProperty = DependencyProperty.Register(nameof(ModTypeString), typeof(string), typeof(ModDetail), new PropertyMetadata(ModUtils.MODTYPENAME_UNKNOWN));
        public static readonly DependencyProperty ModUrlProperty = DependencyProperty.Register(nameof(ModUrl), typeof(string), typeof(ModDetail), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty TimeUpdatedProperty = DependencyProperty.Register(nameof(TimeUpdated), typeof(int), typeof(ModDetail), new PropertyMetadata(0));
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(ModDetail), new PropertyMetadata(string.Empty));


        public string AppId
        {
            get { return (string)GetValue(AppIdProperty); }
            set { SetValue(AppIdProperty, value); }
        }

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
            set
            {
                SetValue(TitleProperty, value);

                TitleFilterString = value?.ToLower();
            }
        }


        public bool IsValidModType => !ModTypeString.Equals(ModUtils.MODTYPENAME_UNKNOWN);

        public string LastWriteTimeString => LastWriteTime == DateTime.MinValue ? string.Empty : LastWriteTime.ToString();

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

        public string TitleFilterString
        {
            get;
            private set;
        }

        public bool UpToDate => LastTimeUpdated > 0 && LastTimeUpdated == TimeUpdated;

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
                ModTypeString = ModUtils.MODTYPENAME_UNKNOWN;

            switch (ModType)
            {
                case ModUtils.MODTYPE_MOD:
                    ModTypeString = ModUtils.MODTYPENAME_MOD;
                    break;
                case ModUtils.MODTYPE_MAP:
                    ModTypeString = ModUtils.MODTYPENAME_MAP;
                    break;
                case ModUtils.MODTYPE_TOTCONV:
                    ModTypeString = ModUtils.MODTYPENAME_TOTCONV;
                    break;
                case ModUtils.MODTYPE_MAPEXT:
                    ModTypeString = ModUtils.MODTYPENAME_MAPEXT;
                    break;
                default:
                    ModTypeString = ModUtils.MODTYPENAME_UNKNOWN;
                    break;
            }
        }


        public static ModDetail GetModDetail(PublishedFileDetail detail)
        {
            var result = new ModDetail();
            result.AppId = detail.creator_app_id;
            result.ModId = detail.publishedfileid;
            result.TimeUpdated = detail.time_updated;
            result.Title = detail.title;
            return result;
        }

        public static ModDetail GetModDetail(WorkshopFileDetail detail)
        {
            var result = new ModDetail();
            result.AppId = detail.creator_appid;
            result.ModId = detail.publishedfileid;
            result.TimeUpdated = detail.time_updated;
            result.Title = detail.title;
            return result;
        }

        public static ModDetail GetModDetail(WorkshopFileItem detail)
        {
            var result = new ModDetail();
            result.AppId = detail.AppId;
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

            ModType = metaInformation != null && metaInformation.ContainsKey("ModType") ? metaInformation["ModType"] : ModUtils.MODTYPE_UNKNOWN;
            MapName = mapNames != null && mapNames.Count > 0 ? mapNames[0] : string.Empty;
        }
    }
}

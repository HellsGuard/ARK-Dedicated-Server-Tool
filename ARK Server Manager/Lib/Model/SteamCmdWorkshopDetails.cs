using System;
using System.Collections.Generic;
using System.Linq;
using NeXt.Vdf;

/*
"AppWorkshop"
{
	"appid"		"346110"
	"SizeOnDisk"		"99914"
	"NeedsUpdate"		"0"
	"NeedsDownload"		"0"
	"TimeLastUpdated"		"0"
	"TimeLastAppRan"		"0"
	"WorkshopItemsInstalled"
	{
		"708972573"
		{
			"manifest"		"6649979517228319960"
			"size"		"99914"
			"timeupdated"		"1466669008"
		}
	}
	"WorkshopItemDetails"
	{
		"708972573"
		{
			"manifest"		"6649979517228319960"
			"timeupdated"		"1466669008"
			"timetouched"		"1468841245"
		}
	}
}
*/

namespace ARK_Server_Manager.Lib.Model
{
    public class SteamCmdWorkshopDetailsResult
    {
        public static SteamCmdAppWorkshop Deserialize(VdfValue data)
        {
            var result = new SteamCmdAppWorkshop();

            var vdfTable = data as VdfTable;
            if (vdfTable != null)
            {
                var value = vdfTable.FirstOrDefault(v => v.Name.Equals("appid", StringComparison.OrdinalIgnoreCase));
                if (value != null) result.appid = GetValue(value);

                value = vdfTable.FirstOrDefault(v => v.Name.Equals("SizeOnDisk", StringComparison.OrdinalIgnoreCase));
                if (value != null) result.SizeOnDisk = GetValue(value);

                value = vdfTable.FirstOrDefault(v => v.Name.Equals("NeedsUpdate", StringComparison.OrdinalIgnoreCase));
                if (value != null) result.NeedsUpdate = GetValue(value);

                value = vdfTable.FirstOrDefault(v => v.Name.Equals("NeedsDownload", StringComparison.OrdinalIgnoreCase));
                if (value != null) result.NeedsDownload = GetValue(value);

                value = vdfTable.FirstOrDefault(v => v.Name.Equals("TimeLastUpdated", StringComparison.OrdinalIgnoreCase));
                if (value != null) result.TimeLastUpdated = GetValue(value);

                value = vdfTable.FirstOrDefault(v => v.Name.Equals("TimeLastAppRan", StringComparison.OrdinalIgnoreCase));
                if (value != null) result.TimeLastAppRan = GetValue(value);

                value = vdfTable.FirstOrDefault(v => v.Name.Equals("WorkshopItemsInstalled", StringComparison.OrdinalIgnoreCase));
                var tableValue = value as VdfTable;
                if (tableValue != null && tableValue.Count > 0)
                {
                    result.WorkshopItemsInstalled = new List<SteamCmdWorkshopItemsInstalled>();

                    foreach (var item in tableValue)
                    {
                        if (item is VdfTable)
                        {
                            var temp = new SteamCmdWorkshopItemsInstalled();
                            temp.publishedfileid = item.Name;

                            value = ((VdfTable)item).FirstOrDefault(v => v.Name.Equals("manifest", StringComparison.OrdinalIgnoreCase));
                            if (value != null) temp.manifest = GetValue(value);

                            value = ((VdfTable)item).FirstOrDefault(v => v.Name.Equals("size", StringComparison.OrdinalIgnoreCase));
                            if (value != null) temp.size = GetValue(value);

                            value = ((VdfTable)item).FirstOrDefault(v => v.Name.Equals("timeupdated", StringComparison.OrdinalIgnoreCase));
                            if (value != null) temp.timeupdated = GetValue(value);

                            result.WorkshopItemsInstalled.Add(temp);
                        }
                    }
                }

                value = vdfTable.FirstOrDefault(v => v.Name.Equals("WorkshopItemDetails", StringComparison.OrdinalIgnoreCase));
                tableValue = value as VdfTable;
                if (tableValue != null && tableValue.Count > 0)
                {
                    result.WorkshopItemDetails = new List<SteamCmdWorkshopItemDetails>();

                    foreach (var item in tableValue)
                    {
                        if (item is VdfTable)
                        {
                            var temp = new SteamCmdWorkshopItemDetails();
                            temp.publishedfileid = item.Name;

                            value = ((VdfTable)item).FirstOrDefault(v => v.Name.Equals("manifest", StringComparison.OrdinalIgnoreCase));
                            if (value != null) temp.manifest = GetValue(value);

                            value = ((VdfTable)item).FirstOrDefault(v => v.Name.Equals("timeupdated", StringComparison.OrdinalIgnoreCase));
                            if (value != null) temp.timeupdated = GetValue(value);

                            value = ((VdfTable)item).FirstOrDefault(v => v.Name.Equals("timetouched", StringComparison.OrdinalIgnoreCase));
                            if (value != null) temp.timetouched = GetValue(value);

                            result.WorkshopItemDetails.Add(temp);
                        }
                    }
                }
            }

            return result;
        }

        public static string GetValue(VdfValue data)
        {
            if (data == null)
                return null;

            switch (data.Type)
            {
                case VdfValueType.Decimal:
                    return ((VdfDecimal)data).Content.ToString("G0");
                case VdfValueType.Long:
                    return ((VdfLong)data).Content.ToString("G0");
                case VdfValueType.String:
                    return ((VdfString)data).Content;
                default:
                    return null;
            }
        }
    }

    public class SteamCmdAppWorkshop
    {
        public string appid { get; set; }

        public string SizeOnDisk { get; set; }

        public string NeedsUpdate { get; set; }

        public string NeedsDownload { get; set; }

        public string TimeLastUpdated { get; set; }

        public string TimeLastAppRan { get; set; }

        public List<SteamCmdWorkshopItemsInstalled> WorkshopItemsInstalled { get; set; }

        public List<SteamCmdWorkshopItemDetails> WorkshopItemDetails { get; set; }
    }

    public class SteamCmdWorkshopItemsInstalled
    {
        public string publishedfileid { get; set; }

        public string manifest { get; set; }

        public string size { get; set; }

        public string timeupdated { get; set; }
    }

    public class SteamCmdWorkshopItemDetails
    {
        public string publishedfileid { get; set; }

        public string manifest { get; set; }

        public string timeupdated { get; set; }

        public string timetouched { get; set; }
    }
}

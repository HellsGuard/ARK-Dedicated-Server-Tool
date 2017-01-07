using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ARK_Server_Manager.Lib.Model;
using NeXt.Vdf;

namespace ARK_Server_Manager.Lib
{
    public static class SteamUtils
    {
        public static WorkshopFileDetailResponse GetSteamModDetails(string appId)
        {
            const int MAX_IDS = 100;

            var totalRequests = 0;
            var requestIndex = 1;

            var response = new WorkshopFileDetailResponse();

            try
            {
                do
                {
                    var httpRequest = WebRequest.Create($"https://api.steampowered.com/IPublishedFileService/QueryFiles/v1/?key={SteamWebApiKey}&format=json&query_type=1&page={requestIndex}&numperpage={MAX_IDS}&appid={appId}&match_all_tags=0&include_recent_votes_only=0&totalonly=0&return_vote_data=0&return_tags=0&return_kv_tags=0&return_previews=0&return_children=0&return_short_description=0&return_for_sale_data=0&return_metadata=1");
                    httpRequest.Timeout = 30000;
                    var httpResponse = httpRequest.GetResponse();
                    var responseString = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();

                    var result = JsonUtils.Deserialize<WorkshopFileDetailResult>(responseString);
                    if (result == null || result.response == null)
                        break;

                    if (totalRequests == 0)
                    {
                        totalRequests = 1;
                        response = result.response;

                        if (response.total > MAX_IDS)
                        {
                            int remainder;
                            totalRequests = Math.DivRem(response.total, MAX_IDS, out remainder);
                            if (remainder > 0)
                                totalRequests++;
                        }
                    }
                    else
                    {
                        if (result.response.publishedfiledetails != null)
                            response.publishedfiledetails.AddRange(result.response.publishedfiledetails);
                    }

                    requestIndex++;
                } while (requestIndex <= totalRequests);

                return response;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(GetSteamModDetails)}\r\n{ex.Message}");
                return null;
            }
        }

        public static PublishedFileDetailsResponse GetSteamModDetails(List<string> modIdList)
        {
            const int MAX_IDS = 20;

            PublishedFileDetailsResponse response = null;

            try
            {
                if (modIdList.Count == 0)
                    return new PublishedFileDetailsResponse();

                modIdList = ModUtils.ValidateModList(modIdList);

                int remainder;
                var totalRequests = Math.DivRem(modIdList.Count, MAX_IDS, out remainder);
                if (remainder > 0)
                    totalRequests++;

                var requestIndex = 0;
                while (requestIndex < totalRequests)
                {
                    var count = 0;
                    var postData = "";
                    for (var index = requestIndex * MAX_IDS; count < MAX_IDS && index < modIdList.Count; index++)
                    {
                        postData += $"&publishedfileids[{count}]={modIdList[index]}";
                        count++;
                    }

                    postData = $"key={SteamWebApiKey}&format=json&itemcount={count}{postData}";

                    var data = Encoding.ASCII.GetBytes(postData);

                    var httpRequest = WebRequest.Create("https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/");
                    httpRequest.Timeout = 30000;
                    httpRequest.Method = "POST";
                    httpRequest.ContentType = "application/x-www-form-urlencoded";
                    httpRequest.ContentLength = data.Length;

                    using (var stream = httpRequest.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }

                    var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                    var responseString = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();

                    var result = JsonUtils.Deserialize<PublishedFileDetailsResult>(responseString);
                    if (result != null && result.response != null)
                    {
                        if (response == null)
                            response = result.response;
                        else
                        {
                            response.resultcount += result.response.resultcount;
                            response.publishedfiledetails.AddRange(result.response.publishedfiledetails);
                        }
                    }

                    requestIndex++;
                };

                return response ?? new PublishedFileDetailsResponse();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(GetSteamModDetails)}\r\n{ex.Message}");
                return null;
            }
        }

        public static SteamUserDetailResponse GetSteamUserDetails(List<string> steamIdList)
        {
            const int MAX_IDS = 100;

            SteamUserDetailResponse response = null;

            try
            {
                if (steamIdList.Count == 0)
                    return new SteamUserDetailResponse();

                steamIdList = steamIdList.Distinct().ToList();

                int remainder;
                var totalRequests = Math.DivRem(steamIdList.Count, MAX_IDS, out remainder);
                if (remainder > 0)
                    totalRequests++;

                var requestIndex = 0;
                while (requestIndex < totalRequests)
                {
                    var count = 0;
                    var postData = "";
                    var delimiter = "";
                    for (var index = requestIndex * MAX_IDS; count < MAX_IDS && index < steamIdList.Count; index++)
                    {
                        postData += $"{delimiter}{steamIdList[index]}";
                        delimiter = ",";
                        count++;
                    }

                    var httpRequest = WebRequest.Create($"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={SteamWebApiKey}&format=json&steamids={postData}");
                    httpRequest.Timeout = 30000;
                    var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                    var responseString = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();

                    var result = JsonUtils.Deserialize<SteamUserDetailResult>(responseString);
                    if (result != null && result.response != null)
                    {
                        if (response == null)
                            response = result.response;
                        else
                        {
                            response.players.AddRange(result.response.players);
                        }
                    }

                    requestIndex++;
                }

                return response ?? new SteamUserDetailResponse();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(GetSteamUserDetails)}\r\n{ex.Message}");
                return null;
            }
        }

        public static SteamCmdAppWorkshop ReadSteamCmdAppWorkshopFile(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                return null;

            var vdfSerializer = VdfDeserializer.FromFile(file);
            var vdf = vdfSerializer.Deserialize();

            return SteamCmdWorkshopDetailsResult.Deserialize(vdf);
        }

        public static string SteamWebApiKey
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Config.Default.SteamAPIKey))
                    return Config.Default.SteamAPIKey;
                return Config.Default.ASMSteamAPIKey;
            }
        }
    }
}

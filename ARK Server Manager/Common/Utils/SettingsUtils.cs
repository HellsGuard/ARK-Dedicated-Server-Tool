﻿using ARK_Server_Manager.Lib;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace ARK_Server_Manager.Utils
{
    public static class SettingsUtils
    {
        public static void BackupUserConfigSettings(System.Configuration.ApplicationSettingsBase settings, string fileName, string settingsPath, string backupPath)
        {
            if (settings == null || string.IsNullOrWhiteSpace(fileName))
                return;

            var settingsFileName = Path.GetFileNameWithoutExtension(fileName);
            var settingsFileExt = Path.GetExtension(fileName);
            var settingsFile = IOUtils.NormalizePath(Path.Combine(settingsPath, $"{fileName}"));

            try
            {
                // save the settings file to a json settings file
                var jsonSettings = new JsonSerializerSettings
                {
                    ContractResolver = new UserScopedSettingContractResolver()
                };
                JsonUtils.Serialize(settings, settingsFile, jsonSettings);
            }
            catch (Exception)
            {
                // do nothing, just exit
            }

            if (!string.IsNullOrWhiteSpace(backupPath))
            {
                // create a backup of the settings file
                var backupFile = IOUtils.NormalizePath(Path.Combine(backupPath, $"{settingsFileName}_{DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")}{settingsFileExt}"));

                try
                {
                    if (!Directory.Exists(backupPath))
                        Directory.CreateDirectory(backupPath);
                    File.Copy(settingsFile, backupFile);
                }
                catch (Exception)
                {
                    // do nothing, just exit
                }

                var filesToDelete = new DirectoryInfo(backupPath).GetFiles($"{settingsFileName}_*{settingsFileExt}").Where(f => f.LastWriteTimeUtc.AddDays(7) < DateTime.UtcNow).ToArray();
                foreach (var fileToDelete in filesToDelete)
                {
                    try
                    {
                        fileToDelete.Delete();
                    }
                    catch (Exception)
                    {
                        // do nothing, just exit
                    }
                }
            }
        }
    }
}

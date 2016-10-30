using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ARK_Server_Manager.Lib.ViewModel
{
    public class NPCSpawnSettings : DependencyObject
    {
        public NPCSpawnSettings()
        {
            NPCSpawnEntrySettings = new List<NPCSpawnEntrySettings>();
        }

        public static readonly DependencyProperty ContainerTypeProperty = DependencyProperty.Register(nameof(ContainerType), typeof(NPCSpawnContainerType), typeof(NPCSpawnSettings), new PropertyMetadata(NPCSpawnContainerType.Override));
        public NPCSpawnContainerType ContainerType
        {
            get { return (NPCSpawnContainerType)GetValue(ContainerTypeProperty); }
            set { SetValue(ContainerTypeProperty, value); }
        }

        public static readonly DependencyProperty NPCSpawnEntriesContainerClassStringProperty = DependencyProperty.Register(nameof(NPCSpawnEntriesContainerClassString), typeof(string), typeof(NPCSpawnSettings), new PropertyMetadata(string.Empty));
        public string NPCSpawnEntriesContainerClassString
        {
            get { return (string)GetValue(NPCSpawnEntriesContainerClassStringProperty); }
            set { SetValue(NPCSpawnEntriesContainerClassStringProperty, value); }
        }

        public static readonly DependencyProperty NPCSpawnEntrySettingsProperty = DependencyProperty.Register(nameof(NPCSpawnEntrySettings), typeof(List<NPCSpawnEntrySettings>), typeof(NPCSpawnSettings), new PropertyMetadata(null));
        public List<NPCSpawnEntrySettings> NPCSpawnEntrySettings
        {
            get { return (List<NPCSpawnEntrySettings>)GetValue(NPCSpawnEntrySettingsProperty); }
            set { SetValue(NPCSpawnEntrySettingsProperty, value); }
        }

        public NPCSpawnSettings Clone()
        {
            return new NPCSpawnSettings()
            {
                ContainerType = ContainerType,
                NPCSpawnEntriesContainerClassString = NPCSpawnEntriesContainerClassString,
                NPCSpawnEntrySettings = NPCSpawnEntrySettings.Select(s => s.Clone()).ToList()
            };
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(NPCSpawnEntriesContainerClassString);
    }

    public class NPCSpawnEntrySettings : DependencyObject
    {
        public static readonly DependencyProperty AnEntryNameProperty = DependencyProperty.Register(nameof(AnEntryName), typeof(string), typeof(NPCSpawnEntrySettings), new PropertyMetadata(string.Empty));
        public string AnEntryName
        {
            get { return (string)GetValue(AnEntryNameProperty); }
            set { SetValue(AnEntryNameProperty, value); }
        }

        public static readonly DependencyProperty NPCClassStringProperty = DependencyProperty.Register(nameof(NPCClassString), typeof(string), typeof(NPCSpawnEntrySettings), new PropertyMetadata(string.Empty));
        public string NPCClassString
        {
            get { return (string)GetValue(NPCClassStringProperty); }
            set { SetValue(NPCClassStringProperty, value); }
        }

        public static readonly DependencyProperty EntryWeightProperty = DependencyProperty.Register(nameof(EntryWeight), typeof(float), typeof(NPCSpawnEntrySettings), new PropertyMetadata(1.0f));
        public float EntryWeight
        {
            get { return (float)GetValue(EntryWeightProperty); }
            set { SetValue(EntryWeightProperty, value); }
        }

        public static readonly DependencyProperty MaxPercentageOfDesiredNumToAllowProperty = DependencyProperty.Register(nameof(MaxPercentageOfDesiredNumToAllow), typeof(float), typeof(NPCSpawnEntrySettings), new PropertyMetadata(1.0f));
        public float MaxPercentageOfDesiredNumToAllow
        {
            get { return (float)GetValue(MaxPercentageOfDesiredNumToAllowProperty); }
            set { SetValue(MaxPercentageOfDesiredNumToAllowProperty, value); }
        }

        public static readonly DependencyProperty FriendlyNameProperty = DependencyProperty.Register(nameof(FriendlyName), typeof(string), typeof(NPCSpawnEntrySettings), new PropertyMetadata(string.Empty));
        public string FriendlyName
        {
            get { return (string)GetValue(FriendlyNameProperty); }
            set { SetValue(FriendlyNameProperty, value); }
        }

        public NPCSpawnEntrySettings Clone()
        {
            return new NPCSpawnEntrySettings()
            {
                AnEntryName = AnEntryName,
                NPCClassString = NPCClassString,
                EntryWeight = EntryWeight,
                MaxPercentageOfDesiredNumToAllow = MaxPercentageOfDesiredNumToAllow,

                FriendlyName = FriendlyName,
            };
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(NPCClassString);
    }

    public class NPCSpawnSettingsList : SortableObservableCollection<NPCSpawnSettings>
    {
        public NPCSpawnContainerList<NPCSpawnContainer> ConfigAddNPCSpawnEntriesContainer { get; }
        public NPCSpawnContainerList<NPCSpawnContainer> ConfigSubtractNPCSpawnEntriesContainer { get; }
        public NPCSpawnContainerList<NPCSpawnContainer> ConfigOverrideNPCSpawnEntriesContainer { get; }

        public NPCSpawnSettingsList()
        {
            Reset();
        }

        public NPCSpawnSettingsList(NPCSpawnContainerList<NPCSpawnContainer> configAddNPCSpawnEntriesContainer,
            NPCSpawnContainerList<NPCSpawnContainer> configSubtractNPCSpawnEntriesContainer,
            NPCSpawnContainerList<NPCSpawnContainer> configOverrideNPCSpawnEntriesContainer)
        {
            ConfigAddNPCSpawnEntriesContainer = configAddNPCSpawnEntriesContainer;
            ConfigSubtractNPCSpawnEntriesContainer = configSubtractNPCSpawnEntriesContainer;
            ConfigOverrideNPCSpawnEntriesContainer = configOverrideNPCSpawnEntriesContainer;

            Reset();
        }

        public NPCSpawnSettingsList Clone()
        {
            var clone = new NPCSpawnSettingsList();
            clone.Clear();

            foreach (var npcSpawnSetting in this)
            {
                clone.Add(npcSpawnSetting.Clone());
            }

            return clone;
        }

        public void Reset()
        {
            this.Clear();
        }

        public void RenderToView()
        {
            Reset();

            foreach (var entry in this.ConfigAddNPCSpawnEntriesContainer)
            {
                if (!entry.IsValid)
                    continue;

                var spawnSettings = new NPCSpawnSettings {
                                        ContainerType = NPCSpawnContainerType.Add,
                                        NPCSpawnEntriesContainerClassString = entry.NPCSpawnEntriesContainerClassString
                                    };
                foreach (var item in entry.NPCSpawnEntries)
                {
                    spawnSettings.NPCSpawnEntrySettings.Add(new NPCSpawnEntrySettings {
                                                                AnEntryName = item.AnEntryName,
                                                                NPCClassString = item.NPCsToSpawnStrings,
                                                                EntryWeight = item.EntryWeight,
                                                                FriendlyName = GameData.FriendlyNameForClass(item.NPCsToSpawnStrings),
                                                            });
                }

                foreach (var item in entry.NPCSpawnLimits)
                {
                    var temp = spawnSettings.NPCSpawnEntrySettings.FirstOrDefault(i => i.NPCClassString.Equals(item.NPCClassString));
                    if (temp == null)
                        continue;

                    temp.MaxPercentageOfDesiredNumToAllow = item.MaxPercentageOfDesiredNumToAllow;
                }

                this.Add(spawnSettings);
            }

            foreach (var entry in this.ConfigSubtractNPCSpawnEntriesContainer)
            {
                if (!entry.IsValid)
                    continue;

                var spawnSettings = new NPCSpawnSettings {
                                        ContainerType = NPCSpawnContainerType.Subtract,
                                        NPCSpawnEntriesContainerClassString = entry.NPCSpawnEntriesContainerClassString
                                    };
                foreach (var item in entry.NPCSpawnEntries)
                {
                    spawnSettings.NPCSpawnEntrySettings.Add(new NPCSpawnEntrySettings {
                                                                AnEntryName = item.AnEntryName,
                                                                NPCClassString = item.NPCsToSpawnStrings,
                                                                EntryWeight = item.EntryWeight,
                                                                FriendlyName = GameData.FriendlyNameForClass(item.NPCsToSpawnStrings),
                                                            });
                }

                foreach (var item in entry.NPCSpawnLimits)
                {
                    var temp = spawnSettings.NPCSpawnEntrySettings.FirstOrDefault(i => i.NPCClassString.Equals(item.NPCClassString));
                    if (temp == null)
                        continue;

                    temp.MaxPercentageOfDesiredNumToAllow = item.MaxPercentageOfDesiredNumToAllow;
                }

                this.Add(spawnSettings);
            }

            foreach (var entry in this.ConfigOverrideNPCSpawnEntriesContainer)
            {
                if (!entry.IsValid)
                    continue;

                var spawnSettings = new NPCSpawnSettings {
                                        ContainerType = NPCSpawnContainerType.Override,
                                        NPCSpawnEntriesContainerClassString = entry.NPCSpawnEntriesContainerClassString
                                    };
                foreach (var item in entry.NPCSpawnEntries)
                {
                    spawnSettings.NPCSpawnEntrySettings.Add(new NPCSpawnEntrySettings {
                                                                AnEntryName = item.AnEntryName,
                                                                NPCClassString = item.NPCsToSpawnStrings,
                                                                EntryWeight = item.EntryWeight,
                                                                FriendlyName = GameData.FriendlyNameForClass(item.NPCsToSpawnStrings),
                                                            });
                }

                foreach (var item in entry.NPCSpawnLimits)
                {
                    var temp = spawnSettings.NPCSpawnEntrySettings.FirstOrDefault(i => i.NPCClassString.Equals(item.NPCClassString));
                    if (temp == null)
                        continue;

                    temp.MaxPercentageOfDesiredNumToAllow = item.MaxPercentageOfDesiredNumToAllow;
                }

                this.Add(spawnSettings);
            }
        }

        public void RenderToModel()
        {
            this.ConfigAddNPCSpawnEntriesContainer.Clear();
            this.ConfigSubtractNPCSpawnEntriesContainer.Clear();
            this.ConfigOverrideNPCSpawnEntriesContainer.Clear();

            foreach (var entry in this)
            {
                if (!entry.IsValid)
                    continue;

                var spawnContainer = new NPCSpawnContainer { NPCSpawnEntriesContainerClassString = entry.NPCSpawnEntriesContainerClassString };
                spawnContainer.NPCSpawnEntries.AddRange(entry.NPCSpawnEntrySettings.Where(s => s.IsValid).Select(s => new NPCSpawnEntry {
                                                                                                                          AnEntryName = string.IsNullOrWhiteSpace(s.AnEntryName) ? s.NPCClassString : s.AnEntryName,
                                                                                                                          EntryWeight = s.EntryWeight,
                                                                                                                          NPCsToSpawnStrings = s.NPCClassString
                                                                                                                      }));
                spawnContainer.NPCSpawnLimits.AddRange(entry.NPCSpawnEntrySettings.Where(s => s.IsValid).Select(s => new NPCSpawnLimit {
                                                                                                                         NPCClassString = s.NPCClassString,
                                                                                                                         MaxPercentageOfDesiredNumToAllow = s.MaxPercentageOfDesiredNumToAllow
                                                                                                                     }));

                switch (entry.ContainerType)
                {
                    case NPCSpawnContainerType.Add:
                        this.ConfigAddNPCSpawnEntriesContainer.Add(spawnContainer);
                        break;

                    case NPCSpawnContainerType.Subtract:
                        this.ConfigSubtractNPCSpawnEntriesContainer.Add(spawnContainer);
                        break;

                    case NPCSpawnContainerType.Override:
                        this.ConfigOverrideNPCSpawnEntriesContainer.Add(spawnContainer);
                        break;
                }
            }
        }

        public void UpdateForLocalization()
        {
            foreach (var item in this.SelectMany(entry => entry.NPCSpawnEntrySettings))
            {
                item.FriendlyName = GameData.FriendlyNameForClass(item.NPCClassString);
            }
        }
    }
}

using ARK_Server_Manager.Lib;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using WPFSharp.Globalizer;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for ProfileSyncWindow.xaml
    /// </summary>
    public partial class ProfileSyncWindow : Window
    {
        public class ProfileSection : DependencyObject
        {
            public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(nameof(Selected), typeof(bool), typeof(ProfileSection), new PropertyMetadata(false));
            public static readonly DependencyProperty SectionNameProperty = DependencyProperty.Register(nameof(SectionName), typeof(string), typeof(ProfileSection), new PropertyMetadata(string.Empty));
            public static readonly DependencyProperty SectionProperty = DependencyProperty.Register(nameof(Section), typeof(ServerProfile.ServerProfileSection), typeof(ProfileSection));

            public bool Selected
            {
                get { return (bool)GetValue(SelectedProperty); }
                set { SetValue(SelectedProperty, value); }
            }
            public string SectionName
            {
                get { return (string)GetValue(SectionNameProperty); }
                set { SetValue(SectionNameProperty, value); }
            }
            public ServerProfile.ServerProfileSection Section
            {
                get { return (ServerProfile.ServerProfileSection)GetValue(SectionProperty); }
                set { SetValue(SectionProperty, value); }
            }
        }

        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public static readonly DependencyProperty ProfilesProperty = DependencyProperty.Register(nameof(Profiles), typeof(ObservableCollection<ServerProfile>), typeof(ProfileSyncWindow), new PropertyMetadata(new ObservableCollection<ServerProfile>()));
        public static readonly DependencyProperty ProfileSectionsProperty = DependencyProperty.Register(nameof(ProfileSections), typeof(ObservableCollection<ProfileSection>), typeof(ProfileSyncWindow), new PropertyMetadata(new ObservableCollection<ProfileSection>()));
        public static readonly DependencyProperty SelectedProfileIdProperty = DependencyProperty.Register(nameof(SelectedProfileId), typeof(string), typeof(ProfileSyncWindow), new PropertyMetadata(string.Empty));

        public ProfileSyncWindow(ServerManager serverManager, ServerProfile profile)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);

            this.Title = string.Format(_globalizer.GetResourceString("ProfileSyncWindow_ProfileTitle"), profile?.ProfileName);

            this.ServerManager = serverManager;
            this.Profile = profile;
            this.DataContext = this;
        }

        public ServerManager ServerManager
        {
            get;
            private set;
        }

        public ObservableCollection<ServerProfile> Profiles
        {
            get { return (ObservableCollection<ServerProfile>)GetValue(ProfilesProperty); }
            set { SetValue(ProfilesProperty, value); }
        }

        public ObservableCollection<ProfileSection> ProfileSections
        {
            get { return (ObservableCollection<ProfileSection>)GetValue(ProfileSectionsProperty); }
            set { SetValue(ProfileSectionsProperty, value); }
        }

        public ServerProfile Profile
        {
            get;
            private set;
        }

        public string SelectedProfileId
        {
            get { return (string)GetValue(SelectedProfileIdProperty); }
            set { SetValue(SelectedProfileIdProperty, value); }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                CreateProfileList();
                CreateProfileSectionList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ProfileSyncWindow_Load_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Process_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                if (string.IsNullOrWhiteSpace(SelectedProfileId))
                {
                    MessageBox.Show(_globalizer.GetResourceString("ProfileSyncWindow_Process_NoProfileSelectedLabel"), _globalizer.GetResourceString("ProfileSyncWindow_Process_Title"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!ProfileSections.Any(s => s.Selected))
                {
                    MessageBox.Show(_globalizer.GetResourceString("ProfileSyncWindow_Process_NoSectionsSelectedLabel"), _globalizer.GetResourceString("ProfileSyncWindow_Process_Title"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (MessageBox.Show(_globalizer.GetResourceString("ProfileSyncWindow_Process_Label"), _globalizer.GetResourceString("ProfileSyncWindow_Process_Title"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);

                PerformProfileSync();

                MessageBox.Show(_globalizer.GetResourceString("ProfileSyncWindow_Process_SuccessLabel"), _globalizer.GetResourceString("ProfileSyncWindow_Process_Label"), MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ProfileSyncWindow_Process_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var section in ProfileSections)
            {
                section.Selected = true;
            }
        }

        private void UnselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var section in ProfileSections)
            {
                section.Selected = false;
            }
        }

        private void CreateProfileList()
        {
            Profiles.Clear();

            if (this.ServerManager == null || this.ServerManager.Servers == null)
                return;

            foreach (var server in this.ServerManager.Servers)
            {
                if (server.Profile == Profile)
                    continue;

                Profiles.Add(server.Profile);
            }
        }

        private void CreateProfileSectionList()
        {
            ProfileSections.Clear();

            ProfileSections.Add(new ProfileSection() { Selected = false, Section = ServerProfile.ServerProfileSection.AdministrationSection, SectionName = _globalizer.GetResourceString("ServerSettings_AdministrationSectionLabel") });
            ProfileSections.Add(new ProfileSection() { Selected = false, Section = ServerProfile.ServerProfileSection.AutomaticManagement, SectionName = _globalizer.GetResourceString("ServerSettings_AutomaticManagementLabel") });
            ProfileSections.Add(new ProfileSection() { Selected = false, Section = ServerProfile.ServerProfileSection.RulesSection, SectionName = _globalizer.GetResourceString("ServerSettings_RulesLabel") });
            ProfileSections.Add(new ProfileSection() { Selected = false, Section = ServerProfile.ServerProfileSection.ChatAndNotificationsSection, SectionName = _globalizer.GetResourceString("ServerSettings_ChatAndNotificationsLabel") });
            ProfileSections.Add(new ProfileSection() { Selected = false, Section = ServerProfile.ServerProfileSection.HudAndVisualsSection, SectionName = _globalizer.GetResourceString("ServerSettings_HUDAndVisualsLabel") });
            ProfileSections.Add(new ProfileSection() { Selected = false, Section = ServerProfile.ServerProfileSection.PlayerSettingsSection, SectionName = _globalizer.GetResourceString("ServerSettings_PlayerSettingsLabel") });
            ProfileSections.Add(new ProfileSection() { Selected = false, Section = ServerProfile.ServerProfileSection.DinoSettingsSection, SectionName = _globalizer.GetResourceString("ServerSettings_DinoSettingsLabel") });
            ProfileSections.Add(new ProfileSection() { Selected = false, Section = ServerProfile.ServerProfileSection.EnvironmentSection, SectionName = _globalizer.GetResourceString("ServerSettings_EnvironmentLabel") });
            ProfileSections.Add(new ProfileSection() { Selected = false, Section = ServerProfile.ServerProfileSection.StructuresSection, SectionName = _globalizer.GetResourceString("ServerSettings_StructuresLabel") });
            ProfileSections.Add(new ProfileSection() { Selected = false, Section = ServerProfile.ServerProfileSection.EngramsSection, SectionName = _globalizer.GetResourceString("ServerSettings_EngramsLabel") });
            ProfileSections.Add(new ProfileSection() { Selected = false, Section = ServerProfile.ServerProfileSection.CustomSettingsSection, SectionName = _globalizer.GetResourceString("ServerSettings_CustomSettingsLabel") });
            ProfileSections.Add(new ProfileSection() { Selected = false, Section = ServerProfile.ServerProfileSection.CustomLevelsSection, SectionName = _globalizer.GetResourceString("ServerSettings_CustomLevelProgressionsLabel") });
            if (Config.Default.SectionCraftingOverridesEnabled)
                ProfileSections.Add(new ProfileSection() { Selected = false, Section = ServerProfile.ServerProfileSection.CraftingOverridesSection, SectionName = _globalizer.GetResourceString("ServerSettings_CraftingOverridesLabel") });
            if (Config.Default.SectionMapSpawnerOverridesEnabled)
                ProfileSections.Add(new ProfileSection() { Selected = false, Section = ServerProfile.ServerProfileSection.MapSpawnerOverridesSection, SectionName = _globalizer.GetResourceString("ServerSettings_MapSpawnerOverridesLabel") });
            if (Config.Default.SectionSupplyCrateOverridesEnabled)
                ProfileSections.Add(new ProfileSection() { Selected = false, Section = ServerProfile.ServerProfileSection.SupplyCrateOverridesSection, SectionName = _globalizer.GetResourceString("ServerSettings_SupplyCrateOverridesLabel") });
            ProfileSections.Add(new ProfileSection() { Selected = false, Section = ServerProfile.ServerProfileSection.PGMSection, SectionName = _globalizer.GetResourceString("ServerSettings_PGMLabel") });
            ProfileSections.Add(new ProfileSection() { Selected = false, Section = ServerProfile.ServerProfileSection.SOTFSection, SectionName = _globalizer.GetResourceString("ServerSettings_SOTFLabel") });
        }

        private void PerformProfileSync()
        {
            var sourceProfile = Profiles.FirstOrDefault(p => p.ProfileID.Equals(SelectedProfileId));

            foreach (var section in ProfileSections)
            {
                if (!section.Selected)
                    continue;

                Profile.SyncSettings(section.Section, sourceProfile);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Window_FileNameSetter
{
    public partial class MainWindow : Window
    {
        private AppSettings currentSettings = new AppSettings();
        private ObservableCollection<FileItem> fileList = new ObservableCollection<FileItem>();
        private List<FileItem> allFiles = new List<FileItem>();

        private bool isUpdatingAll = false;

        public MainWindow()
        {
            InitializeComponent();
            ApplySettingsOnLoad();

            FileListView.ItemsSource = fileList;
        }

        private string GetLocalizedString(string key)
        {
            var resource = Application.Current.TryFindResource(key);
            return resource?.ToString() ?? key;
        }

        private void ApplySettingsOnLoad()
        {
            currentSettings = AppConfig.Load();
            this.Width = currentSettings.WindowWidth;
            this.Height = currentSettings.WindowHeight;

            LanguageComboBox.SelectionChanged -= LanguageComboBox_SelectionChanged;

            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag?.ToString() == currentSettings.Language)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }

            if (LanguageComboBox.SelectedItem == null)
            {
                LanguageComboBox.SelectedIndex = 0;
            }

            LanguageComboBox.SelectionChanged += LanguageComboBox_SelectionChanged;

            if (!string.IsNullOrEmpty(currentSettings.LastFolder) && Directory.Exists(currentSettings.LastFolder))
            {
                FolderPathTextBox.Text = currentSettings.LastFolder;
                ScanFolder(currentSettings.LastFolder);
            }
        }

        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new OpenFolderDialog
            {
                Title = GetLocalizedString("Msg_SelectFolderTitle")
            };

            if (dialog.ShowDialog() == true)
            {
                string selectedPath = dialog.FolderName;
                FolderPathTextBox.Text = selectedPath;
                ScanFolder(selectedPath);
            }
        }

        // 새로고침 버튼 동작
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(FolderPathTextBox.Text) && Directory.Exists(FolderPathTextBox.Text))
            {
                // 현재 유저가 보고 있던 필터 상태를 기억합니다.
                HashSet<string> currentFilters = new HashSet<string>();
                foreach (var item in ExtensionListBox.Items)
                {
                    if (item is CheckBox chk && chk.IsChecked == true)
                    {
                        currentFilters.Add(chk.Content?.ToString() ?? string.Empty);
                    }
                }

                // 기억한 필터를 넘겨주며 폴더를 다시 스캔합니다.
                ScanFolder(FolderPathTextBox.Text, currentFilters);
            }
        }

        private void DeepSearchCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(FolderPathTextBox.Text) && Directory.Exists(FolderPathTextBox.Text))
            {
                ScanFolder(FolderPathTextBox.Text);
            }
        }

        private void ScanFolder(string targetDirectory, HashSet<string> retainFilters = null)
        {
            allFiles.Clear();
            fileList.Clear();
            ExtensionListBox.Items.Clear();

            HashSet<string> foundExtensions = new HashSet<string>();

            try
            {
                SearchOption option = (DeepSearchCheckBox.IsChecked == true)
                                      ? SearchOption.AllDirectories
                                      : SearchOption.TopDirectoryOnly;

                var files = Directory.EnumerateFiles(targetDirectory, "*.*", option);

                foreach (string filePath in files)
                {
                    string ext = Path.GetExtension(filePath)?.ToLower() ?? string.Empty;

                    if (string.IsNullOrEmpty(ext))
                    {
                        ext = GetLocalizedString("Msg_NoExtension");
                    }

                    FileItem item = new FileItem
                    {
                        FullPath = filePath,
                        CurrentName = Path.GetFileName(filePath) ?? string.Empty,
                        NewName = Path.GetFileName(filePath) ?? string.Empty,
                        FilePath = Path.GetDirectoryName(filePath) ?? string.Empty,
                        Extension = ext,
                        IsChecked = true
                    };

                    item.PropertyChanged += FileItem_PropertyChanged;

                    allFiles.Add(item);
                    foundExtensions.Add(ext);
                }

                foreach (string ext in foundExtensions)
                {
                    CheckBox chk = new CheckBox();
                    chk.Content = ext;

                    chk.IsChecked = (retainFilters != null) ? retainFilters.Contains(ext) : true;
                    chk.Margin = new Thickness(0, 2, 0, 2);

                    chk.Checked += ExtensionCheckBox_Changed;
                    chk.Unchecked += ExtensionCheckBox_Changed;

                    ExtensionListBox.Items.Add(chk);
                }

                ApplyFilter();
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(GetLocalizedString("Msg_NoPermission"), GetLocalizedString("Msg_PermissionError"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format(GetLocalizedString("Msg_ReadError"), ex.Message);
                MessageBox.Show(errorMsg, GetLocalizedString("Msg_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FileItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!isUpdatingAll && e.PropertyName == "IsChecked")
            {
                UpdatePreview();
            }
        }

        private void ExtensionCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            HashSet<string> selectedExtensions = new HashSet<string>();

            foreach (var item in ExtensionListBox.Items)
            {
                if (item is CheckBox chk && chk.IsChecked == true)
                {
                    string extName = chk.Content?.ToString() ?? string.Empty;
                    selectedExtensions.Add(extName);
                }
            }

            fileList.Clear();

            foreach (FileItem file in allFiles)
            {
                if (selectedExtensions.Contains(file.Extension))
                {
                    fileList.Add(file);
                }
            }

            isUpdatingAll = true;
            bool isAllChecked = SelectAllCheckBox?.IsChecked ?? true;
            foreach (var file in fileList)
            {
                file.IsChecked = isAllChecked;
            }
            isUpdatingAll = false;

            UpdatePreview();
        }

        private void SelectAllCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (fileList == null || SelectAllCheckBox == null) return;

            isUpdatingAll = true;
            bool isAllChecked = SelectAllCheckBox.IsChecked ?? false;

            foreach (var item in fileList)
            {
                item.IsChecked = isAllChecked;
            }
            isUpdatingAll = false;

            UpdatePreview();
        }

        private void RuleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (PrefixTextBox == null || SuffixTextBox == null || ReplaceOldTextBox == null || ReplaceNewTextBox == null)
                return;

            string prefix = PrefixTextBox.Text;
            string suffix = SuffixTextBox.Text;
            string oldWord = ReplaceOldTextBox.Text;
            string newWord = ReplaceNewTextBox.Text;

            foreach (FileItem file in fileList)
            {
                if (!file.IsChecked)
                {
                    file.NewName = file.CurrentName;
                    continue;
                }

                string nameWithoutExtension = Path.GetFileNameWithoutExtension(file.CurrentName) ?? string.Empty;

                if (!string.IsNullOrEmpty(oldWord))
                {
                    nameWithoutExtension = nameWithoutExtension.Replace(oldWord, newWord);
                }

                string finalName = prefix + nameWithoutExtension + suffix + file.Extension;
                file.NewName = finalName;
            }
        }

        private void ExecuteRenameButton_Click(object sender, RoutedEventArgs e)
        {
            int targetCount = 0;
            foreach (var item in fileList)
            {
                if (item.IsChecked) targetCount++;
            }

            if (targetCount == 0)
            {
                MessageBox.Show(GetLocalizedString("Msg_NoFilesSelected"), GetLocalizedString("Msg_Notice"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string confirmMsg = string.Format(GetLocalizedString("Msg_ConfirmRename"), targetCount);
            MessageBoxResult confirmResult = MessageBox.Show(confirmMsg, GetLocalizedString("Msg_ExecuteRenameTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes) return;

            int successCount = 0;
            int failCount = 0;

            foreach (FileItem item in fileList)
            {
                if (!item.IsChecked) continue;
                if (item.CurrentName == item.NewName) continue;

                string oldPath = item.FullPath;
                string newPath = Path.Combine(item.FilePath, item.NewName);

                try
                {
                    if (File.Exists(newPath))
                    {
                        failCount++;
                        continue;
                    }

                    File.Move(oldPath, newPath);
                    successCount++;
                }
                catch (Exception)
                {
                    failCount++;
                }
            }

            string resultMsg = string.Format(GetLocalizedString("Msg_RenameResult"), successCount, failCount);
            MessageBox.Show(resultMsg, GetLocalizedString("Msg_Complete"), MessageBoxButton.OK, MessageBoxImage.Information);

            if (!string.IsNullOrEmpty(FolderPathTextBox.Text))
            {
                HashSet<string> currentFilters = new HashSet<string>();
                foreach (var item in ExtensionListBox.Items)
                {
                    if (item is CheckBox chk && chk.IsChecked == true)
                    {
                        currentFilters.Add(chk.Content?.ToString() ?? string.Empty);
                    }
                }

                ScanFolder(FolderPathTextBox.Text, currentFilters);
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string langCode = selectedItem.Tag?.ToString() ?? "en";
                currentSettings.Language = langCode;

                ResourceDictionary dict = new ResourceDictionary();

                string packUri = $"pack://application:,,,/Languages/Lang.{langCode}.xaml";

                switch (langCode)
                {
                    case "en":
                    case "ko":
                    case "ja":
                    case "zh":
                    case "ru":
                    case "de":
                    case "es":
                    case "fr":
                        dict.Source = new Uri(packUri, UriKind.Absolute);
                        break;
                    default:
                        dict.Source = new Uri("pack://application:,,,/Languages/Lang.en.xaml", UriKind.Absolute);
                        break;
                }

                Application.Current.Resources.MergedDictionaries.Clear();
                Application.Current.Resources.MergedDictionaries.Add(dict);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            currentSettings.WindowWidth = this.Width;
            currentSettings.WindowHeight = this.Height;
            currentSettings.LastFolder = FolderPathTextBox.Text;

            AppConfig.Save(currentSettings);

            base.OnClosed(e);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public MainWindow()
        {
            InitializeComponent();
            ApplySettingsOnLoad();

            FileListView.ItemsSource = fileList;
        }

        private string GetLocalizedString(string key)
        {
            var resource = Application.Current.TryFindResource(key);
            return resource?.ToString() ?? key; // 키가 없으면 키 자체를 반환
        }

        private void ApplySettingsOnLoad()
        {
            currentSettings = AppConfig.Load();
            this.Width = currentSettings.WindowWidth;
            this.Height = currentSettings.WindowHeight;

            LanguageComboBox.SelectionChanged -= LanguageComboBox_SelectionChanged;

            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag != null && item.Tag.ToString() == currentSettings.Language)
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
            // 폴더 선택 창의 제목도 다국어 단어장에서 가져오도록 수정했습니다.
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

        private void DeepSearchCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(FolderPathTextBox.Text) && Directory.Exists(FolderPathTextBox.Text))
            {
                ScanFolder(FolderPathTextBox.Text);
            }
        }

        private void ScanFolder(string targetDirectory)
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
                    string ext = Path.GetExtension(filePath).ToLower();

                    // 지적해주신 "[확장자 없음]" 하드코딩을 단어장 로드 방식으로 수정했습니다.
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

                    allFiles.Add(item);
                    foundExtensions.Add(ext);
                }

                foreach (string ext in foundExtensions)
                {
                    CheckBox chk = new CheckBox();
                    chk.Content = ext;
                    chk.IsChecked = true;
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
                    selectedExtensions.Add(chk.Content.ToString() ?? string.Empty);
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

            bool isAllChecked = SelectAllCheckBox?.IsChecked ?? true;
            foreach (var file in fileList)
            {
                file.IsChecked = isAllChecked;
            }

            UpdatePreview();
        }

        private void SelectAllCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (fileList == null || SelectAllCheckBox == null) return;

            bool isAllChecked = SelectAllCheckBox.IsChecked ?? false;

            foreach (var item in fileList)
            {
                item.IsChecked = isAllChecked;
            }
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
                string nameWithoutExtension = Path.GetFileNameWithoutExtension(file.CurrentName);

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
                ScanFolder(FolderPathTextBox.Text);
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string langCode = selectedItem.Tag.ToString() ?? "en";
                currentSettings.Language = langCode;

                ResourceDictionary dict = new ResourceDictionary();

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
                        dict.Source = new Uri($"Languages/Lang.{langCode}.xaml", UriKind.Relative);
                        break;
                    default:
                        dict.Source = new Uri("Languages/Lang.ko.xaml", UriKind.Relative);
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
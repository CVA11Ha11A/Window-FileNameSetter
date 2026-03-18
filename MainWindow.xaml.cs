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

        // 초기 앱 설정 적용
        private void ApplySettingsOnLoad()
        {
            currentSettings = AppConfig.Load();
            this.Width = currentSettings.WindowWidth;
            this.Height = currentSettings.WindowHeight;

            LanguageComboBox.SelectionChanged -= LanguageComboBox_SelectionChanged;

            // 향상된 로직: 하드코딩된 Index 대신, 아이템의 Tag 값을 순회하며 동적으로 UI를 매칭합니다.
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag != null && item.Tag.ToString() == currentSettings.Language)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }

            // 만약 저장된 언어가 콤보박스에 없다면 기본값인 첫 번째(한국어)를 선택합니다.
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
            OpenFolderDialog dialog = new OpenFolderDialog { Title = "변경할 파일들이 있는 폴더를 선택하세요." };

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

        // 폴더 탐색 및 파일 스캔
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
                    if (string.IsNullOrEmpty(ext)) ext = "[확장자 없음]";

                    FileItem item = new FileItem
                    {
                        FullPath = filePath,
                        CurrentName = Path.GetFileName(filePath),
                        NewName = Path.GetFileName(filePath),
                        FilePath = Path.GetDirectoryName(filePath),
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
                MessageBox.Show("해당 폴더에 접근할 권한이 없습니다.", "권한 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일을 읽어오는 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    selectedExtensions.Add(chk.Content.ToString());
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

        // 이름 변경 규칙 실시간 적용
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

        // 실제 파일 이름 변경 실행
        private void ExecuteRenameButton_Click(object sender, RoutedEventArgs e)
        {
            int targetCount = 0;
            foreach (var item in fileList)
            {
                if (item.IsChecked) targetCount++;
            }

            if (targetCount == 0)
            {
                MessageBox.Show("선택된 파일이 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBoxResult confirmResult = MessageBox.Show(
                $"선택된 총 {targetCount}개 파일의 이름을 정말로 변경하시겠습니까?",
                "이름 변경 실행",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

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

            MessageBox.Show($"작업 완료\n- 성공: {successCount}개\n- 실패 또는 건너뜀: {failCount}개",
                            "완료", MessageBoxButton.OK, MessageBoxImage.Information);

            if (!string.IsNullOrEmpty(FolderPathTextBox.Text))
            {
                ScanFolder(FolderPathTextBox.Text);
            }
        }

        // 언어 변경 이벤트 핸들러
        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string langCode = selectedItem.Tag.ToString();
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
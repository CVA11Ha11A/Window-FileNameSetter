using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Window_FileNameSetter
{
    // 리스트뷰에 띄워줄 개별 파일의 데이터 모델입니다.
    public class FileItem : INotifyPropertyChanged
    {
        private string _newName = string.Empty;
        private bool _isChecked = true; // 기본적으로 모든 파일은 선택된 상태로 시작합니다.

        // 사용자가 이 파일을 이름 변경 대상에 포함시킬지 여부를 결정하는 속성입니다.
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrentName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;

        // 변경될 이름 (미리보기용)
        public string NewName
        {
            get => _newName;
            set
            {
                if (_newName != value)
                {
                    _newName = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        // 속성 변경 이벤트를 발생시키는 헬퍼 함수입니다.
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
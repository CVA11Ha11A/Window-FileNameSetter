using System.Configuration;
using System.Data;
using System.Windows;

namespace Window_FileNameSetter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        // 프로그램이 가장 처음 시작될 때 실행되는 생명주기 함수입니다.
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. JSON 설정 파일 불러오기
            AppSettings settings = AppConfig.Load();

            // 2. 설정된 언어에 맞춰 단어장 로드
            ResourceDictionary dict = new ResourceDictionary();
            switch (settings.Language)
            {
                case "en":
                case "ko":
                    dict.Source = new Uri($"Languages/Lang.{settings.Language}.xaml", UriKind.Relative);
                    break;
                default:
                    // 알 수 없는 언어 코드가 들어왔을 때의 안전 장치(Fallback)
                    dict.Source = new Uri("Languages/Lang.ko.xaml", UriKind.Relative);
                    break;
            }

            // 앱 전체 리소스에 단어장 장착
            this.Resources.MergedDictionaries.Add(dict);
        }

    }

}

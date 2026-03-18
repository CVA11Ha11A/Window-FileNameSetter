using System;
using System.Windows;
using System.Windows.Threading; // 전역 예외 처리를 위해 추가된 네임스페이스입니다.

namespace Window_FileNameSetter
{
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }
        ~App()
        {
            this.DispatcherUnhandledException -= App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // 에러가 나면 조용히 꺼지지 않고, 원인을 메시지 박스로 띄워줍니다.
            MessageBox.Show($"Critical Error!:\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
                            "Program Quit", MessageBoxButton.OK, MessageBoxImage.Error);

            // 이 에러를 우리가 확인했으니 윈도우 기본 에러 창은 띄우지 말라고 처리합니다.
            e.Handled = true;

            // 확인 버튼을 누르면 안전하게 프로그램을 종료합니다.
            Environment.Exit(1);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppSettings settings = AppConfig.Load();
            ResourceDictionary dict = new ResourceDictionary();

            // 단일 파일 배포 시 안전하게 내부 리소스를 찾는 Pack URI 절대 경로입니다.
            string packUri = $"pack://application:,,,/Languages/Lang.{settings.Language}.xaml";

            switch (settings.Language)
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
                    dict.Source = new Uri("pack://application:,,,/Languages/Lang.ko.xaml", UriKind.Absolute);
                    break;
            }

            this.Resources.MergedDictionaries.Add(dict);
        }
    }
}
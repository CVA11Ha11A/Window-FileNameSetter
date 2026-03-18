using System;
using System.IO;
using System.Text.Json;

namespace Window_FileNameSetter
{
    // 저장할 설정 데이터 구조
    public class AppSettings
    {
        public string Language { get; set; } = "en";
        public string LastFolder { get; set; } = "";
        public double WindowWidth { get; set; } = 900;
        public double WindowHeight { get; set; } = 600;
    }

    // 설정을 파일로 관리하는 정적(Static) 매니저 클래스
    public static class AppConfig
    {
        // 1. 윈도우의 %LocalAppData% 경로를 가져온 뒤, 우리 프로그램 이름으로 된 전용 폴더 경로를 만듭니다.
        // 예시: C:\Users\UserName\AppData\Local\CVallHallA\Window_FileNameSetter
        private static readonly string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CVallHallA", "Window_FileNameSetter");

        // 2. 최종적으로 settings.json이 저장될 전체 경로입니다.
        private static readonly string configPath = Path.Combine(folderPath, "settings.json");

        public static AppSettings Load()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch { return new AppSettings(); }
            }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            // 3. 프로그램 전용 폴더가 존재하는지 확인합니다.
            if (!Directory.Exists(folderPath))
            {               
                Directory.CreateDirectory(folderPath);
            }

            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
        }
    }
}
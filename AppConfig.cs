using System;
using System.IO;
using System.Text.Json;

namespace Window_FileNameSetter
{
    // 저장할 설정 데이터 구조
    public class AppSettings
    {
        public string Language { get; set; } = "ko";      // 기본 언어
        public string LastFolder { get; set; } = "";      // 마지막 탐색 폴더
        public double WindowWidth { get; set; } = 900;    // 창 가로 크기
        public double WindowHeight { get; set; } = 600;   // 창 세로 크기
    }

    // 설정을 파일로 관리하는 정적(Static) 매니저 클래스
    public static class AppConfig
    {
        // 프로그램이 실행되는 폴더에 settings.json 이라는 이름으로 저장합니다.
        private static readonly string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static AppSettings Load()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    // JSON 텍스트를 C# 객체로 변환 (역직렬화)
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch { return new AppSettings(); } // 오류 시 기본값 반환
            }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            // C# 객체를 JSON 텍스트로 변환 (직렬화)
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
        }
    }
}
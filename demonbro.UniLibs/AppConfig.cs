using System;

namespace demonbro.UniLibs
{
    public class AppConfig
    {
        public double WindowWidth { get; set; } = 800;
        public double WindowHeight { get; set; } = 600;
        public string LastUsedList { get; set; } = "default";
        public string TextColorHex { get; set; } = "#FF000000"; // 使用十六进制格式存储
        public double AnimationSpeed { get; set; } = 1.0;
        public bool IsTopMost { get; set; }
    }
}
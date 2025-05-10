using System;

namespace RandPicker.SubModules
{
    public class AppConfig
    {
        public enum DefaultPageMode
        {
            MainPage,
            MultiPick
        }
        public DefaultPageMode DefaultPage { get; set; } = DefaultPageMode.MainPage;
        public string BorderColor { get; set; } = "#63B8FF";
        public bool UseRSAEncryption { get; set; }
        public bool UseInstantMode { get; set; } = false;
    }
}
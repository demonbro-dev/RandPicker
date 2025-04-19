using System;

namespace demonbro.UniLibs
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
    }
}
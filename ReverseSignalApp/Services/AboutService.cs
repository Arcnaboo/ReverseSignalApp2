namespace ReverseSignalApp.Services
{
    public class AboutService
    {


        public static async Task<string> AboutApp()
        {
            await Task.Delay(250);
            return "⚡ ArcSoftwares™ — Bu uygulama Arc Corp ve Yüksi CTO’su Arda Akgür tarafından geliştirilmiştir. " +
                   "Teknolojinin sınırlarını yeniden tanımlayan bir vizyonun ürünüdür. © 2025 Arc Corp.";
        }

    }
}

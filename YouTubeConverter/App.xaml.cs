using GalaSoft.MvvmLight.Threading;
using System.Windows;

namespace YouTubeConverter
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App
    {
        static App()
        {
            DispatcherHelper.Initialize();
        }

        private void App_OnStartUp(object sender, StartupEventArgs e)
        {
            Locator.Init();
        }
    }
}

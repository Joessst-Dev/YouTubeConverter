using CommonServiceLocator;
using GalaSoft.MvvmLight.Ioc;
using YouTubeConverter.ViewModels;

namespace YouTubeConverter
{
    public class Locator
    {
        public static void Init()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            SimpleIoc.Default.Register<MainViewModel>();
        }

        public MainViewModel MainViewModel => ServiceLocator.Current.GetInstance<MainViewModel>();
    }
}

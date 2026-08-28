using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Win32.DWrite;

namespace DWBox
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private AppViewModel _appViewModel = new AppViewModel();
        public AppViewModel AppViewModel => _appViewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args?.Contains("core") == true)
                DWriteFactory.SwitchLibraries(true);

            base.OnStartup(e);
        }

        public static new App Current => (App)Application.Current;
        public static AppViewModel ViewModel => Current.AppViewModel;
    }    
}

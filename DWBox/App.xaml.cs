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
        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args?.Contains("core") == true)
                DWriteFactory.SwitchLibraries(true);

            base.OnStartup(e);
        }
    }
}

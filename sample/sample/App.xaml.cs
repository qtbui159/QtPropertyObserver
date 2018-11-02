using Qt;
using sample.View;
using sample.ViewModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace sample
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            QtPropertyObserver.Complete<ViewModelBase>();
            QtPropertyObserver.Complete<MainViewModel>();

            MainWindow = new MainWindow();
            MainWindow.Show();
        }
    }
}

# QtPropertyObserver
Auto complete properties setter NotifyPropertyChanged in WPF <br/>

You can get the library from [nuget][1] or build this project

Code sample:

    class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
    
        [RaisePropertyChanged]
        public int ValueA { get; set;}
    }
    
    QtPropertyObserver.Complete<MainViewModel>(); //<-- make it works
    
    //Now, when ValueA is changed, the library will call PropertyChanged automatically
    //And there are three Attributes can help this library works well.
    
Get more infomation on sample solution :)

提供了3个Attributes来完成功能，用法可以参考sample项目


  [1]: https://www.nuget.org/packages/QtPropertyObserver/
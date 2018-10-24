using Qt;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace sample.ViewModel
{
    class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private static QtPropertyObserver<MainViewModel> m_PropertyObserver = new QtPropertyObserver<MainViewModel>();

        public int A { get; set; }

        [IgnoreNotify]
        public int B { get; set; }

        [AlsoNotify(nameof(D), nameof(E))]
        public int C { get; set; }
        public string D
        {
            get => $"C is {(C % 2 == 0 ? "even" : "odd")} number";
        }
        public string E
        {
            get => $"C is not {(C % 2 != 0 ? "even" : "odd")} number";
        }
        
        public MyCommand ChangeACommand { private set; get; }
        public MyCommand ChangeBCommand { private set; get; }
        public MyCommand ChangeCCommand { private set; get; }

        public MainViewModel()
        {
            ChangeACommand = new MyCommand(ChangeACommandProc);
            ChangeBCommand = new MyCommand(ChangeBCommandProc);
            ChangeCCommand = new MyCommand(ChangeCCommandProc);
        }

        private void ChangeACommandProc()
        {
            A++;
        }

        private void ChangeBCommandProc()
        {
            B++;
        }

        private void ChangeCCommandProc()
        {
            C++;
        }
    }
}

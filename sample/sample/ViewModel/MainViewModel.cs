using Qt;
using Qt.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace sample.ViewModel
{
    class MainViewModel : ViewModelBase
    {
        [RaisePropertyChanged]
        public int A { get; set; }

        public int B { get; set; }

        [RaiseOtherPropertyChanged(nameof(D), nameof(E))]
        [RaiseCanExecuteChanged(nameof(TestCommand))]
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
        public MyCommand TestCommand { private set; get; }

        [RaiseCanExecuteChanged(nameof(StaticTestCommand))]
        public static int Static { get; set; }

        public static MyCommand StaticCommand { private set; get; }
        public static MyCommand StaticTestCommand { private set; get; }

        public MainViewModel()
        {
            ChangeACommand = new MyCommand(ChangeACommandProc);
            ChangeBCommand = new MyCommand(ChangeBCommandProc);
            ChangeCCommand = new MyCommand(ChangeCCommandProc);
            TestCommand = new MyCommand(TestCommandProc, CanTestExecute);

            StaticCommand = new MyCommand(StaticCommandProc);
            StaticTestCommand = new MyCommand(StaticTestCommandProc, CanStaticTestExecute);
        }

        private void ChangeACommandProc()
        {
            A++;

            Title = A.ToString();
        }

        private void ChangeBCommandProc()
        {
            B++;
        }

        private void ChangeCCommandProc()
        {
            C++;
        }

        private void TestCommandProc()
        {

        }

        private bool CanTestExecute()
        {
            return C % 2 == 0;
        }

        private void StaticCommandProc()
        {
            ++Static;
        }

        private void StaticTestCommandProc()
        {
        }

        private bool CanStaticTestExecute()
        {
            return Static % 2 == 0;
        }
    }
}

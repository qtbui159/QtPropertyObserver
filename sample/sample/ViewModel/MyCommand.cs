using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace sample.ViewModel
{
    class MyCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private Action m_Action = null;

        public MyCommand(Action action)
        {
            m_Action = action;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            m_Action();
        }
    }
}

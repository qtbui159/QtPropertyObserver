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
        private Func<bool> m_CanExecute = null;

        public MyCommand(Action command, Func<bool> canExecute = null)
        {
            m_Action = command;
            m_CanExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (m_CanExecute == null)
            {
                return true;
            }
            return m_CanExecute();
        }

        public void Execute(object parameter)
        {
            m_Action();
        }
    }
}

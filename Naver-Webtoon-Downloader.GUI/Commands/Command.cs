using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace NaverWebtoonDownloader.GUI
{
    class Command : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private bool _canExecute;

        private Action _execute;

        public Command(Action execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public void Execute(object parameter)
        {
            _canExecute = false;
            _execute();
            _canExecute = true;
        }
    }
}

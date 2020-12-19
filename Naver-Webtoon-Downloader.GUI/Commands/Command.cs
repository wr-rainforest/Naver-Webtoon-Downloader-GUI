using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace NaverWebtoonDownloader.GUI
{
    class Command : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool Executable 
        {
            get => _canExecute;
            set
            {
                _canExecute = value;
                CanExecuteChanged(this, new EventArgs());
            }
        }

        private bool _canExecute = true;

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
            CanExecuteChanged(this, new EventArgs());
            _execute();
            _canExecute = true;
            CanExecuteChanged(this, new EventArgs());
        }
    }
}

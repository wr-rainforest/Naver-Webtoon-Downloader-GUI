using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace NaverWebtoonDownloader.GUI
{
    class Command : ICommand
    {
        bool isRunning;
        Action<object> execute;

        public Command(Action<object> execute)
        {
            this.execute = execute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object obj)
        {
            return !isRunning;
        }

        public void Execute(object obj)
        {
            isRunning = true;
            execute(obj);
            isRunning = false;
        }
    }
}

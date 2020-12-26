using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NaverWebtoonDownloader.GUI
{
    class AsyncCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private Func<object, Task> _func;

        public AsyncCommand(Func<object, Task> func)
        {
            _func = func;
        }

        public async void Execute(object parameter)
        {
            await _func(parameter);
        }

        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }
    }
}

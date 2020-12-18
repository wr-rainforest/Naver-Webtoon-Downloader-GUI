using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace NaverWebtoonDownloader.GUI
{
    class OpenSettingWindowCommand : ICommand
    {
        public Config Config { get; set; }

        public event EventHandler CanExecuteChanged;

        private bool _canExecute;

        public OpenSettingWindowCommand(Config config)
        {
            Config = config;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public void Execute(object parameter)
        {
            _canExecute = false;
            _canExecute = true;
        }
    }
}

using System;
using System.Windows.Input;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class ActionCommand : ICommand
    {
        public ActionCommand(Action act)
        {
            _act = act;
        }

        private readonly Action _act;

        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _act?.Invoke();

#pragma warning disable CS0067
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
    }

    public class ActionCommand<T> : ICommand
        where T : class
    {
        /// <summary>
        /// NOTE: null許容が求められるのはViewがパラメータをnullにする可能性が実際あるため。ここは根本的に非null保証がない
        /// </summary>
        /// <param name="act"></param>
        public ActionCommand(Action<T?> act)
        {
            _act = act;
        }

        private readonly Action<T?> _act;

        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _act?.Invoke(parameter as T);

#pragma warning disable CS0067
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
    }

}

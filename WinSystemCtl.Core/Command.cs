using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WinSystemCtl.Core
{
    public class Command : ICommand
    {
        private Action action;
        private Predicate<object> predicate;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return predicate == null || predicate.Invoke(parameter);
        }

        public void Execute(object parameter)
        {
            action?.Invoke();
        }

        public Command(Action actionToInvoke, Predicate<object> predicateToInvoke = null)
        {
            action = actionToInvoke;
            predicate = predicateToInvoke;
        }
    }

    public class Command<T> : ICommand
    {
        private Action<T> action;
        private Predicate<object> predicate;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return predicate == null || predicate.Invoke(parameter);
        }

        public void Execute(object parameter)
        {
            action?.Invoke((T)parameter);
        }

        public Command(Action<T> actionToInvoke, Predicate<object> predicateToInvoke = null)
        {
            action = actionToInvoke;
            predicate = predicateToInvoke;
        }
    }
}

using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NinaHA.Plugin.Mvvm {

    /// <summary>An async <see cref="ICommand"/> that disables itself while the operation runs.</summary>
    public sealed class AsyncDelegateCommand : ICommand {

        private readonly Func<object?, Task> execute;
        private readonly Predicate<object?>? canExecute;
        private bool isRunning;

        public AsyncDelegateCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null) {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        public AsyncDelegateCommand(Func<Task> execute, Predicate<object?>? canExecute = null)
            : this(_ => execute(), canExecute) { }

        public bool CanExecute(object? parameter) => !isRunning && (canExecute?.Invoke(parameter) ?? true);

        public async void Execute(object? parameter) {
            if (!CanExecute(parameter)) {
                return;
            }
            isRunning = true;
            RaiseCanExecuteChanged();
            try {
                await execute(parameter).ConfigureAwait(true);
            } finally {
                isRunning = false;
                RaiseCanExecuteChanged();
            }
        }

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

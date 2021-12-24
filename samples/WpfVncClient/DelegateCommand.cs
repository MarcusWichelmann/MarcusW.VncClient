using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfVncClient;

public class DelegateCommand : ICommand
{
    private static readonly Func<object?, Task<bool>> DefaultCanExecute = o => Task.FromResult(true);
    private readonly Func<object?, Task<bool>> _canExecute;
    private readonly Func<object?, Task> _execute;

    public DelegateCommand(Func<object?, Task> execute, Func<object?, Task<bool>> canExecute = null!)
    {
        _canExecute = canExecute ?? DefaultCanExecute;
        _execute = execute;
    }

    public bool CanExecute(object? parameter)
    {
        Task<bool> t = _canExecute(parameter);
        t.Wait();
        return t.Result;
    }

    public void Execute(object? parameter)
    {
        Task t = _execute.Invoke(parameter);

        // t.Wait();
    }

    public event EventHandler? CanExecuteChanged;

    protected virtual void OnCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

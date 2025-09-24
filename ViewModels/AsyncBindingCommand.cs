using System.ComponentModel;
using System.Windows.Input;

namespace SSHExplorer.ViewModels;

public class AsyncBindingCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool> _canExecute;
    private readonly INotifyPropertyChanged? _propertySource;

    public AsyncBindingCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null, INotifyPropertyChanged? propertySource = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute ?? (_ => true);
        _propertySource = propertySource;
        
        if (_propertySource is not null)
            _propertySource.PropertyChanged += OnPropertyChanged;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute(parameter);

    public async void Execute(object? parameter)
    {
        if (CanExecute(parameter))
            await _execute(parameter);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (MainThread.IsMainThread)
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
        }
    }
}

public class AsyncBindingCommand<T> : ICommand
{
    private readonly Func<T?, Task> _execute;
    private readonly Func<T?, bool> _canExecute;
    private readonly INotifyPropertyChanged? _propertySource;

    public AsyncBindingCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null, INotifyPropertyChanged? propertySource = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute ?? (_ => true);
        _propertySource = propertySource;
        
        if (_propertySource is not null)
            _propertySource.PropertyChanged += OnPropertyChanged;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => parameter is T t && _canExecute(t);

    public async void Execute(object? parameter)
    {
        if (parameter is T t && CanExecute(parameter))
            await _execute(t);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (MainThread.IsMainThread)
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
        }
    }
}
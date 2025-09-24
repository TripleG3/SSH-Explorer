namespace SSHExplorer.Models;

public abstract class StatePublisher<T> : IStatePublisher<T>
{
    private T _state;
    
    protected StatePublisher(T initialState)
    {
        _state = initialState;
    }

    public event Action<T>? StateChanged;
    
    public T State => _state;

    protected void SetState(T newState)
    {
        _state = newState;
        
        if (MainThread.IsMainThread)
        {
            StateChanged?.Invoke(_state);
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => StateChanged?.Invoke(_state));
        }
    }
}

public interface IStatePublisher<T>
{
    event Action<T> StateChanged;
    T State { get; }
}
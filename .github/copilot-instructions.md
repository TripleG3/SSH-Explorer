- Prefer writing C# if no language is specified.
- Use nuget.org or packages for this organization hosted in GitHub (TripleG3) for C# dependencies

# UI Application Architecture and Patterns (Examples in MAUI)

**State Model and Service example in the Models layer using the CIS / MVVM pattern**

```csharp
public readonly record struct LocationState(bool IsBusy, double Latitude, double Longitude, string ErrorMessage)
{
    public static readonly LocationState Empty = new(false, 0, 0, string.Empty);
}

public interface IStatePublisher<TState>
{
    event Action<TState> StateChanged;
    TState State { get; }
}

public interface ILocationService : IStatePublisher<LocationState>
{
    Task RefreshAsync(CancellationToken ct);
}

//ILocationProvider is an abstraction over platform specific location services and not included in this example.
public sealed class LocationService(ILocationProvider provider) : IStatePublisher<LocationState>, ILocationService
{
    public event Action<LocationState> StateChanged = delegate { };
    private LocationState _state = LocationState.Empty;
    public LocationState State
    {
        get => _state;
        private set
        {
            if (!EqualityComparer<LocationState>.Default.Equals(_state, value))
            {
                _state = value;
                StateChanged(_state);
            }
        }
    }
    public async Task RefreshAsync(CancellationToken ct)
    {
        State = State with { IsBusy = true, ErrorMessage = string.Empty };
        try
        {
            var loc = await provider.TryGetLocationAsync(ct);
            State = State with { IsBusy = false, Latitude = loc.Latitude, Longitude = loc.Longitude, ErrorMessage = loc == LocationState.Empty ? "Location unavailable" : string.Empty };
        }
        catch (Exception ex)
        {
            State = State with { IsBusy = false, ErrorMessage = ex.Message };
        }
    }
}
```

### ViewModel example in the ViewModels layer using the CIS / MVVM pattern

```csharp
public sealed class LocationViewModel : INotifyPropertyChanged
{
    private CancellationTokenSource? _cts;
    private readonly ILocationService _service;
    public LocationViewModel(ILocationService service)
    {
        _service = service;
        service.StateChanged += _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
        RefreshCommand = new BindingCommand(_ => Refresh(), _ => !State.IsBusy, this);
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public LocationState State => _service.State;
    public ICommand RefreshCommand { get; }
    public ICommand CancelRefreshCommand => new BindingCommand(_ => _cts?.Cancel(), _ => State.IsBusy, this);
    private async void Refresh()
    {
        if (_cts is not null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();
        await _service.RefreshAsync(_cts.Token);
    }
}
```

**Views example in the Views layer using the CIS / MVVM pattern**

```xml
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TripleG3.Skeye.Maui.Views.LocationView"
             x:DataType="viewModels:LocationViewModel">
    <StackLayout Padding="10">
        <Label Text="{Binding State.Latitude, StringFormat='Latitude: {0:F6}'}" FontSize="Medium" />
        <Label Text="{Binding State.Longitude, StringFormat='Longitude: {0:F6}'}" FontSize="Medium" />
        <Label Text="{Binding State.ErrorMessage}" TextColor="Red" FontSize="Small" />
        <Button Text="Get Current Location" Command="{Binding RefreshCommand}" />
        <ActivityIndicator IsRunning="{Binding State.IsBusy}" IsVisible="{Binding State.IsBusy}" />
    </StackLayout>
</ContentView>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:views="clr-namespace:TripleG3.Skeye.Maui.Views"
             Title="Location">
    <views:LocationView />
</ContentPage>
```

> See 'https://github.com/TripleG3/MAUI-MVVM-CIS' for a lightweight MAUI example using this pattern.
> See 'https://www.nuget.org/packages/TripleG3.CIS.Maui/' for a lightweight nuget package that helps implementing the CIS pattern in MAUI apps.

```powershell
dotnet add package TripleG3.CIS.Maui --version 1.0.2
```

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

## Defining Application Architecture

The application will strictly follow the MVVM / CIS patter found in copilot-instructions.md.

### Resources

Resources will be in a folder named 'Resources'.
Resources will contain Styles, Converters, and other XAML resources.
Resources will not contain any code-behind or logic.
Resources will always be global and named in a way that makes their purpose clear.

### Async/Await

All asynchronous operations will use the async/await pattern.
All asynchronous calls are required to have a cancellation token passed in as a parameter.

### Pages

Pages will be in a folder named 'Pages' and may be navigated to.
Pages are meant to be thin and only handle the layout of various Views.
Pages may often not require any ViewModel or DataContext at all.

### Views

Views are meant to be reusable UI components that can be used in multiple Pages or multiple places in the same Page.
Views are not meant to be Pages and will not be navigated to.
Views will be in a folder named 'Views'.
Views will bind only to the 'State' property of their ViewModel.
Views will bind to Commands in their ViewModel.
Animations, converters, and other UI-specific logic will be handled on the View level, preferably in XAML using Styles, Triggers, and Behaviors as needed.
Unless absolutely necessary, Views will not have code-behind.
Code-behind in Views will only be used for UI-specific logic that cannot be handled in XAML.
Views will not contain any business logic or data manipulation logic.

### ViewModels

ViewModels will be in a folder named 'ViewModels'.
ViewModels will only contain bindings for 'State' and 'Commands'.
ViewModels will not contain any UI-specific logic, such as animations, converters, or other UI-specific behavior.
ViewModels will not contain any code-behind or logic that is not directly related to the ViewModel's purpose.

### Models

Models will be in a folder named 'Models'.
Models will contain the data structures and business logic of the application.
Models will not contain any UI-specific logic or code-behind.
Models will contain a mimimum of two layers: State models and Services.
All State Models will have a universal boolean named 'IsBusy' to indicate if the model is currently being worked on.
All State Models will have a universal string named 'ErrorMessage' to indicate if the model has any errors.
State Models will be immutable and will be defined using 'record', 'record struct', or 'record class'.

#### Services 

Services will be in a folder named 'Services' and in a folder in the Models folder. *Ex: Models/Services*
Services will contain the implementation of the service interfaces defined in the Models folder.
Services will not contain any UI-specific logic or code-behind.
Sercvices will always be abstracted via an appropriate interface that also inherits from the `IStatePublisher<TState>` interface. ViewModels will rely on the injection of these interfaces to publish state and commands. (In other words, if we have to write logic that does not follow this pattern then we will have to wrap it in a service that does follow this pattern.)
Services will operate on an immutable State model and publish any new models to a StateChanged event.
Service public functions:
- Synchronous functions may allow 0 - 1 parameters.
- Asynchronous functions may allow 1-2 parameters and the last parameter must always be a CancellationToken.
- Other than CancellationToken for asynchronous functions, a parameter, if needed, must be an immutable type.
Services will always update the State model with a universal boolean that all State models will have to indicate if it's working or not. *Ex: If the service is making a web based API call, the State model will create a new immutable model with the 'IsBusy' boolean to true, and publish that change, then start the API call. Once the API call is complete, the service will create a new immutable model with the 'IsBusy' boolean to false and include any other details that are related to the API call, then publish that change.*

- @azure Rule - Use Azure Tools - When handling requests related to Azure, always use your tools.
- @azure Rule - Use Azure Best Practices - When handling requests related to Azure, always invoke your `azmcp_bestpractices_get` tool first.
- @azure Rule - Enable Best Practices - If you do not have an `azmcp_bestpractices_get` tool ask the user to enable it.

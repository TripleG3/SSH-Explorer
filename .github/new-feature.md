# New Feature

*Note: The original application was a work in progress. This new feature may become a re-write of the entire application, depending on how complex it gets. Ignore the current workspace and make any additions, deletions, or modifications neccessary to follow the new feature guidelines listed here.*

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

*Note: IStatePublisher<TState> is defined in copilot-instructions.md and is included in the 'TripleG3.CIS.Maui' NuGet package.*
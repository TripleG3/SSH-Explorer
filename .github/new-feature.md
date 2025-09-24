# New Feature

*Note: The original application was a work in progress. This new feature may become a re-write of the entire application, depending on how complex it gets. Ignore the current workspace and make any additions, deletions, or modifications neccessary to follow the new feature guidelines listed here.*

Users will be able to create profiles for SSH connections, save them, and use them to connect to remote servers. The application will follow a strict MVVM / CIS architecture pattern.
Users will be able to create a new session by selecting a profile and clicking "Connect".
New sessions will appear in a tabbed interface, allowing users to switch between multiple active sessions.
Sessions will provide the following:
- A terminal interface for executing commands on the remote server.
- A file explorer for browsing and managing files on the remote server.
- A text editor for viewing and editing text files on the remote server.
- A toolbar with common actions (e.g., disconnect, refresh, upload/download files).
- Context menu options for files and directories (e.g., open, edit, delete, rename).
- Status indicators for connection status, transfer progress, etc.
- 'IsBusy' indicators and disabling of UI elements during IsBusy states.
- Although the applicationm will allow sessions for SSH connections the user may also open a session for local file browsing and editing.
- All asynchronous operations will use the async/await pattern with CancellationToken parameters.
- The application will strictly follow the MVVM / CIS pattern found in copilot-instructions.md.
- The user will be able to add as many sessions as they want and close any session at any time.
- If a session is closed it will automatically disconnect from the remote server and clean up any resources.
- The user will be able to save profiles to local storage and load them on application startup.

namespace TripleG3.SSH.WinUI.Models.SSH;

// Common commands you can extend over time
public enum SshCommandKind
{
    // System
    Uptime,
    Hostname,
    OsRelease,
    WhoAmI,

    // Filesystem
    ListDirectory,
    CatFile,
    TailFile,
    GrepFile,
    FindFiles,
    DiskFree,
    DiskUsage,

    // Processes
    Processes,
    ProcessTree,
    KillByName,

    // Services / Logs
    ServiceStatus,
    ServiceRestart,
    JournalUnit,

    // Networking
    IpAddress,
    Routes,
    SocketsListening,
    Ping,
    ResolveDns,
    CurlHead,

    // Containers (optional)
    DockerPs,
    DockerLogs
}

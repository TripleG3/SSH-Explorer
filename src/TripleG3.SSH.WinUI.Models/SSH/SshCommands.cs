using System;
using System.Collections.Generic;

namespace TripleG3.SSH.WinUI.Models.SSH;

public static class SshCommands
{
    // System
    public static SshCommand Uptime()      => new(SshCommandKind.Uptime, Args());
    public static SshCommand Hostname()    => new(SshCommandKind.Hostname, Args());
    public static SshCommand OsRelease()   => new(SshCommandKind.OsRelease, Args());
    public static SshCommand WhoAmI()      => new(SshCommandKind.WhoAmI, Args());

    // Filesystem
    public static SshCommand ListDirectory(string path = ".", bool all = true, bool longFormat = true, bool recurse = false, int? maxDepth = null)
        => new(SshCommandKind.ListDirectory, Args(
            ("path", path), ("all", all), ("long", longFormat), ("recurse", recurse), ("maxDepth", maxDepth)));
    public static SshCommand CatFile(string path)
        => new(SshCommandKind.CatFile, Args(("path", path)));
    public static SshCommand TailFile(string path, int lines = 200)
        => new(SshCommandKind.TailFile, Args(("path", path), ("lines", lines)));
    public static SshCommand GrepFile(string path, string pattern)
        => new(SshCommandKind.GrepFile, Args(("path", path), ("pattern", pattern)));
    public static SshCommand FindFiles(string root = ".", string? namePattern = null, string? type = null /* f,d,l */)
        => new(SshCommandKind.FindFiles, Args(("root", root), ("name", namePattern), ("type", type)));
    public static SshCommand DiskFree()
        => new(SshCommandKind.DiskFree, Args());
    public static SshCommand DiskUsage(string path)
        => new(SshCommandKind.DiskUsage, Args(("path", path)));

    // Processes
    public static SshCommand Processes() => new(SshCommandKind.Processes, Args());
    public static SshCommand ProcessTree() => new(SshCommandKind.ProcessTree, Args());
    public static SshCommand KillByName(string name, string? signal = null /* e.g., TERM, KILL */)
        => new(SshCommandKind.KillByName, Args(("name", name), ("signal", signal)));

    // Services / Logs
    public static SshCommand ServiceStatus(string service)
        => new(SshCommandKind.ServiceStatus, Args(("service", service)));
    public static SshCommand ServiceRestart(string service)
        => new(SshCommandKind.ServiceRestart, Args(("service", service)));
    public static SshCommand JournalUnit(string unit, int lines = 200)
        => new(SshCommandKind.JournalUnit, Args(("unit", unit), ("lines", lines)));

    // Networking
    public static SshCommand IpAddress() => new(SshCommandKind.IpAddress, Args());
    public static SshCommand Routes() => new(SshCommandKind.Routes, Args());
    public static SshCommand SocketsListening() => new(SshCommandKind.SocketsListening, Args());
    public static SshCommand Ping(string host, int count = 4)
        => new(SshCommandKind.Ping, Args(("host", host), ("count", count)));
    public static SshCommand ResolveDns(string name)
        => new(SshCommandKind.ResolveDns, Args(("name", name)));
    public static SshCommand CurlHead(string url)
        => new(SshCommandKind.CurlHead, Args(("url", url)));

    // Containers
    public static SshCommand DockerPs() => new(SshCommandKind.DockerPs, Args());
    public static SshCommand DockerLogs(string containerIdOrName, int lines = 200)
        => new(SshCommandKind.DockerLogs, Args(("id", containerIdOrName), ("lines", lines)));

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (k, v) in items) dict[k] = v;
        return dict;
    }

    private static T GetOrDefault<T>(this IReadOnlyDictionary<string, object?> args, string key, T @default = default!)
        => args.TryGetValue(key, out var v) && v is T t ? t : @default;
}
using System;
using System.Collections.Generic;
using System.Text;

namespace TripleG3.SSH.WinUI.Models.SSH;

// Represents a typed SSH command that can render to a shell-safe string
public sealed record SshCommand(SshCommandKind Kind, IReadOnlyDictionary<string, object?> Args)
{
    public string Build()
    {
        // Build a Bash-compatible command string
        return Kind switch
        {
            // System
            SshCommandKind.Uptime       => "uptime",
            SshCommandKind.Hostname     => "hostnamectl || hostname",
            SshCommandKind.OsRelease    => "cat /etc/os-release",
            SshCommandKind.WhoAmI       => "whoami",

            // Filesystem
            SshCommandKind.ListDirectory => BuildLs(Args),
            SshCommandKind.CatFile       => $"cat {BashQuote(Args["path"])}",
            SshCommandKind.TailFile      => $"tail -n {Args.GetOrDefault<int>("lines", 200)} {BashQuote(Args["path"])}",
            SshCommandKind.GrepFile      => $"grep -n --color=never -e {BashQuote(Args["pattern"])} {BashQuote(Args["path"])}",
            SshCommandKind.FindFiles     => BuildFind(Args),
            SshCommandKind.DiskFree      => "df -h",
            SshCommandKind.DiskUsage     => $"du -sh {BashQuote(Args["path"])}",

            // Processes
            SshCommandKind.Processes     => "ps aux",
            SshCommandKind.ProcessTree   => "ps aux --forest",
            SshCommandKind.KillByName    =>  $"pkill {(Args.GetOrDefault<string>("signal") is { Length: > 0 } s ? $"-{s} " : string.Empty)}{BashQuote(Args["name"])}",

            // Services / Logs
            SshCommandKind.ServiceStatus  => $"systemctl status {BashQuote(Args["service"])} --no-pager",
            SshCommandKind.ServiceRestart => $"sudo -n systemctl restart {BashQuote(Args["service"])}",
            SshCommandKind.JournalUnit    => $"journalctl -u {BashQuote(Args["unit"])} -n {Args.GetOrDefault<int>("lines", 200)} --no-pager",

            // Networking
            SshCommandKind.IpAddress        => "ip a",
            SshCommandKind.Routes           => "ip r",
            SshCommandKind.SocketsListening => "ss -tulpn || netstat -plnt",
            SshCommandKind.Ping             => $"ping -c {Args.GetOrDefault<int>("count", 4)} {BashQuote(Args["host"])}",
            SshCommandKind.ResolveDns       => $"getent hosts {BashQuote(Args["name"])} || nslookup {BashQuote(Args["name"])} || dig +short {BashQuote(Args["name"])}",
            SshCommandKind.CurlHead         => $"curl -fsSI {BashQuote(Args["url"])}",

            // Containers
            SshCommandKind.DockerPs         => "docker ps --format 'table {{.ID}}\t{{.Image}}\t{{.Status}}\t{{.Names}}'",
            SshCommandKind.DockerLogs       => $"docker logs --tail {Args.GetOrDefault<int>("lines", 200)} {BashQuote(Args["id"])}",

            _ => throw new NotSupportedException($"Command kind '{Kind}' is not supported.")
        };
    }

    private static string BuildLs(IReadOnlyDictionary<string, object?> args)
    {
        var path = BashQuote(args.GetOrDefault<string>("path", "."));
        var all = args.GetOrDefault<bool>("all", true);
        var longFmt = args.GetOrDefault<bool>("long", true);
        var recurse = args.GetOrDefault<bool>("recurse", false);
        var depth = args.GetOrDefault<int?>("maxDepth", null);

        var opts = new StringBuilder();
        if (all) opts.Append('a');
        if (longFmt) opts.Append('l');
        var flags = opts.Length > 0 ? "-" + opts + " " : "";

        if (recurse)
        {
            // Use find for recursion with depth control to keep output predictable
            var depthPart = depth is int d ? $"-maxdepth {d} " : "";
            return $"find {path} {depthPart}-mindepth 0 -maxdepth {depth ?? 1} -printf '%p\\n'";
        }

        return $"ls {flags}{path}";
    }

    private static string BuildFind(IReadOnlyDictionary<string, object?> args)
    {
        var root = BashQuote(args.GetOrDefault<string>("root", "."));
        var name = args.GetOrDefault<string?>("name", null);
        var type = args.GetOrDefault<string?>("type", null); // f,d,l

        var sb = new StringBuilder($"find {root}");
        if (!string.IsNullOrWhiteSpace(name))
            sb.Append($" -name {BashQuote(name!)}");
        if (!string.IsNullOrWhiteSpace(type))
            sb.Append($" -type {BashQuote(type!)}");
        sb.Append(" -printf '%p\\n'");
        return sb.ToString();
    }

    private static string BashQuote(object? value)
    {
        var s = value?.ToString() ?? string.Empty;
        if (s.Length == 0) return "''";
        // Single-quote safe: close, escape, reopen: ' -> '\'' in POSIX shell
        return "'" + s.Replace("'", "'\"'\"'") + "'";
    }
}

internal static class SshDictionaryExtensions
{
    public static T GetOrDefault<T>(this IReadOnlyDictionary<string, object?> args, string key, T @default = default!)
    {
        if (args is null) return @default;
        if (args.TryGetValue(key, out var v) && v is T t)
            return t;
        return @default;
    }
}

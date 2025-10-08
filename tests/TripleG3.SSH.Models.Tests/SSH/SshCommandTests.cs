using TripleG3.SSH.WinUI.Models.SSH;

namespace TripleG3.SSH.WinUI.Models.Tests.SSH;

[TestClass]
public sealed class SshCommandTests
{
    private static string Build(SshCommandKind kind, params (string key, object? value)[] args)
    {
        var dict = args.ToDictionary(kv => kv.key, kv => kv.value, StringComparer.Ordinal);
        return new SshCommand(kind, dict).Build();
    }

    [TestMethod]
    public void SystemCommands_AreRendered()
    {
        Assert.AreEqual("uptime", Build(SshCommandKind.Uptime));
        Assert.AreEqual("hostnamectl || hostname", Build(SshCommandKind.Hostname));
        Assert.AreEqual("cat /etc/os-release", Build(SshCommandKind.OsRelease));
        Assert.AreEqual("whoami", Build(SshCommandKind.WhoAmI));
    }

    [TestMethod]
    public void ListDirectory_Defaults_UsesLsAlAndQuotesPath()
    {
        // Default path is '.' and defaults to -al
        Assert.AreEqual("ls -al '.'", Build(SshCommandKind.ListDirectory));
    }

    [TestMethod]
    public void ListDirectory_NoFlags_UsesLsWithQuotedPath()
    {
        Assert.AreEqual("ls '.'", Build(SshCommandKind.ListDirectory,
            ("path", "."),
            ("all", false),
            ("long", false)));
    }

    [TestMethod]
    public void ListDirectory_Recurse_NoDepth_UsesFindWithDefaultDepth1()
    {
        Assert.AreEqual("find '.' -mindepth 0 -maxdepth 1 -printf '%p\\n'", Build(SshCommandKind.ListDirectory,
            ("recurse", true)));
    }

    [TestMethod]
    public void ListDirectory_Recurse_WithDepth_UsesFindAndRepeatsMaxDepthAsImplemented()
    {
        // Note: implementation adds -maxdepth twice when depth is provided
        Assert.AreEqual("find '.' -maxdepth 2 -mindepth 0 -maxdepth 2 -printf '%p\\n'", Build(SshCommandKind.ListDirectory,
            ("recurse", true),
            ("maxDepth", 2)));
    }

    [TestMethod]
    public void CatFile_QuotesPath()
    {
        var path = "a b/it's"; // contains space and single quote to exercise quoting
        var expected = "cat 'a b/it'\"'\"'s'";
        Assert.AreEqual(expected, Build(SshCommandKind.CatFile, ("path", path)));
    }

    [TestMethod]
    public void TailFile_DefaultLinesAndQuotedPath()
    {
        Assert.AreEqual("tail -n 200 '.'", Build(SshCommandKind.TailFile, ("path", ".")));
    }

    [TestMethod]
    public void GrepFile_QuotesPatternAndPath()
    {
        var pattern = "O'Reilly";
        var expected = "grep -n --color=never -e 'O'\"'\"'Reilly' '.'";
        Assert.AreEqual(expected, Build(SshCommandKind.GrepFile,
            ("pattern", pattern),
            ("path", ".")));
    }

    [TestMethod]
    public void FindFiles_BuildsWithOptionalFilters()
    {
        Assert.AreEqual("find '.' -printf '%p\\n'", Build(SshCommandKind.FindFiles));
        Assert.AreEqual("find '/var' -name 'app*' -type 'f' -printf '%p\\n'", Build(SshCommandKind.FindFiles,
            ("root", "/var"),
            ("name", "app*"),
            ("type", "f")));
    }

    [TestMethod]
    public void DiskCommands_Render()
    {
        Assert.AreEqual("df -h", Build(SshCommandKind.DiskFree));
        Assert.AreEqual("du -sh '/etc'", Build(SshCommandKind.DiskUsage, ("path", "/etc")));
    }

    [TestMethod]
    public void ProcessCommands_Render()
    {
        Assert.AreEqual("ps aux", Build(SshCommandKind.Processes));
        Assert.AreEqual("ps aux --forest", Build(SshCommandKind.ProcessTree));
        Assert.AreEqual("pkill 'nginx'", Build(SshCommandKind.KillByName, ("name", "nginx")));
        Assert.AreEqual("pkill -9 'nginx'", Build(SshCommandKind.KillByName, ("name", "nginx"), ("signal", "9")));
    }

    [TestMethod]
    public void ServiceAndJournal_Render()
    {
        Assert.AreEqual("systemctl status 'sshd' --no-pager", Build(SshCommandKind.ServiceStatus, ("service", "sshd")));
        Assert.AreEqual("sudo -n systemctl restart 'sshd'", Build(SshCommandKind.ServiceRestart, ("service", "sshd")));
        Assert.AreEqual("journalctl -u 'sshd' -n 200 --no-pager", Build(SshCommandKind.JournalUnit, ("unit", "sshd")));
    }

    [TestMethod]
    public void Networking_Render()
    {
        Assert.AreEqual("ip a", Build(SshCommandKind.IpAddress));
        Assert.AreEqual("ip r", Build(SshCommandKind.Routes));
        Assert.AreEqual("ss -tulpn || netstat -plnt", Build(SshCommandKind.SocketsListening));
        Assert.AreEqual("ping -c 4 'example.com'", Build(SshCommandKind.Ping, ("host", "example.com")));
        Assert.AreEqual("getent hosts 'example.com' || nslookup 'example.com' || dig +short 'example.com'",
            Build(SshCommandKind.ResolveDns, ("name", "example.com")));
        Assert.AreEqual("curl -fsSI 'https://example.com'", Build(SshCommandKind.CurlHead, ("url", "https://example.com")));
    }

    [TestMethod]
    public void Containers_Render()
    {
        Assert.AreEqual("docker ps --format 'table {{.ID}}\t{{.Image}}\t{{.Status}}\t{{.Names}}'", Build(SshCommandKind.DockerPs));
        Assert.AreEqual("docker logs --tail 200 'abc123'", Build(SshCommandKind.DockerLogs, ("id", "abc123")));
    }
}

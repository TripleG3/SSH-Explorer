using System;
using System.Linq;
using TripleG3.SSH.WinUI.Models.SSH;

namespace TripleG3.SSH.Models.Tests.SSH;

[TestClass]
public sealed class SshCommandsTests
{
    [TestMethod]
    public void SystemFactories_ReturnCorrectKind_AndEmptyArgs()
    {
        var up = SshCommands.Uptime();
        Assert.AreEqual(SshCommandKind.Uptime, up.Kind);
        Assert.AreEqual(0, up.Args.Count);

        var hn = SshCommands.Hostname();
        Assert.AreEqual(SshCommandKind.Hostname, hn.Kind);
        Assert.AreEqual(0, hn.Args.Count);

        var os = SshCommands.OsRelease();
        Assert.AreEqual(SshCommandKind.OsRelease, os.Kind);
        Assert.AreEqual(0, os.Args.Count);

        var who = SshCommands.WhoAmI();
        Assert.AreEqual(SshCommandKind.WhoAmI, who.Kind);
        Assert.AreEqual(0, who.Args.Count);
    }

    [TestMethod]
    public void ListDirectory_Defaults_SetExpectedArgs()
    {
        var cmd = SshCommands.ListDirectory();
        Assert.AreEqual(SshCommandKind.ListDirectory, cmd.Kind);
        Assert.AreEqual(".", cmd.Args["path"]);
        Assert.AreEqual(true, cmd.Args["all"]);
        Assert.AreEqual(true, cmd.Args["long"]);
        Assert.AreEqual(false, cmd.Args["recurse"]);
        Assert.IsNull(cmd.Args["maxDepth"]);

        // spot-check build to ensure flags and quoting from args
        Assert.AreEqual("ls -al '.'", cmd.Build());
    }

    [TestMethod]
    public void ListDirectory_Custom_RecurseWithDepth_SetsArgs_AndBuildMatches()
    {
        var cmd = SshCommands.ListDirectory(path: "/etc", all: false, longFormat: false, recurse: true, maxDepth: 2);
        Assert.AreEqual(SshCommandKind.ListDirectory, cmd.Kind);
        Assert.AreEqual("/etc", cmd.Args["path"]);
        Assert.AreEqual(false, cmd.Args["all"]);
        Assert.AreEqual(false, cmd.Args["long"]);
        Assert.AreEqual(true, cmd.Args["recurse"]);
        Assert.AreEqual(2, cmd.Args["maxDepth"]);

        // Mirrors SshCommandTests behavior: BuildLs duplicates -maxdepth when depth provided
        Assert.AreEqual("find '/etc' -maxdepth 2 -mindepth 0 -maxdepth 2 -printf '%p\\n'", cmd.Build());
    }

    [TestMethod]
    public void Cat_Tail_Grep_Find_Disks_SetArgsAndBuildQuote()
    {
        var cat = SshCommands.CatFile("a b/it's");
        Assert.AreEqual(SshCommandKind.CatFile, cat.Kind);
        Assert.AreEqual("a b/it's", cat.Args["path"]);
        Assert.AreEqual("cat 'a b/it'\"'\"'s'", cat.Build());

        var tail = SshCommands.TailFile("/var/log/syslog");
        Assert.AreEqual(SshCommandKind.TailFile, tail.Kind);
        Assert.AreEqual("/var/log/syslog", tail.Args["path"]);
        Assert.AreEqual(200, tail.Args["lines"]);

        var tail50 = SshCommands.TailFile("/var/log/syslog", 50);
        Assert.AreEqual(50, tail50.Args["lines"]);

        var grep = SshCommands.GrepFile("/tmp/x", "O'Reilly");
        Assert.AreEqual(SshCommandKind.GrepFile, grep.Kind);
        Assert.AreEqual("/tmp/x", grep.Args["path"]);
        Assert.AreEqual("O'Reilly", grep.Args["pattern"]);
        Assert.AreEqual("grep -n --color=never -e 'O'\"'\"'Reilly' '/tmp/x'", grep.Build());

        var find = SshCommands.FindFiles(root: "/var", namePattern: "app*", type: "f");
        Assert.AreEqual(SshCommandKind.FindFiles, find.Kind);
        Assert.AreEqual("/var", find.Args["root"]);
        Assert.AreEqual("app*", find.Args["name"]);
        Assert.AreEqual("f", find.Args["type"]);
        Assert.AreEqual("find '/var' -name 'app*' -type 'f' -printf '%p\\n'", find.Build());

        var df = SshCommands.DiskFree();
        Assert.AreEqual(SshCommandKind.DiskFree, df.Kind);
        Assert.AreEqual(0, df.Args.Count);

        var du = SshCommands.DiskUsage("/etc");
        Assert.AreEqual(SshCommandKind.DiskUsage, du.Kind);
        Assert.AreEqual("/etc", du.Args["path"]);
    }

    [TestMethod]
    public void Processes_SetArgsAndKinds()
    {
        var ps = SshCommands.Processes();
        Assert.AreEqual(SshCommandKind.Processes, ps.Kind);
        Assert.AreEqual(0, ps.Args.Count);

        var tree = SshCommands.ProcessTree();
        Assert.AreEqual(SshCommandKind.ProcessTree, tree.Kind);
        Assert.AreEqual(0, tree.Args.Count);

        var kill = SshCommands.KillByName("nginx");
        Assert.AreEqual(SshCommandKind.KillByName, kill.Kind);
        Assert.AreEqual("nginx", kill.Args["name"]);
        Assert.IsNull(kill.Args["signal"]);

        var kill9 = SshCommands.KillByName("nginx", signal: "9");
        Assert.AreEqual("9", kill9.Args["signal"]);
    }

    [TestMethod]
    public void ServicesAndLogs_SetArgsAndKinds()
    {
        var status = SshCommands.ServiceStatus("sshd");
        Assert.AreEqual(SshCommandKind.ServiceStatus, status.Kind);
        Assert.AreEqual("sshd", status.Args["service"]);

        var restart = SshCommands.ServiceRestart("sshd");
        Assert.AreEqual(SshCommandKind.ServiceRestart, restart.Kind);
        Assert.AreEqual("sshd", restart.Args["service"]);

        var journal = SshCommands.JournalUnit("sshd");
        Assert.AreEqual(SshCommandKind.JournalUnit, journal.Kind);
        Assert.AreEqual("sshd", journal.Args["unit"]);
        Assert.AreEqual(200, journal.Args["lines"]);
    }

    [TestMethod]
    public void Networking_SetArgsAndKinds()
    {
        var ip = SshCommands.IpAddress();
        Assert.AreEqual(SshCommandKind.IpAddress, ip.Kind);
        Assert.AreEqual(0, ip.Args.Count);

        var routes = SshCommands.Routes();
        Assert.AreEqual(SshCommandKind.Routes, routes.Kind);
        Assert.AreEqual(0, routes.Args.Count);

        var sockets = SshCommands.SocketsListening();
        Assert.AreEqual(SshCommandKind.SocketsListening, sockets.Kind);
        Assert.AreEqual(0, sockets.Args.Count);

        var ping = SshCommands.Ping("example.com");
        Assert.AreEqual(SshCommandKind.Ping, ping.Kind);
        Assert.AreEqual("example.com", ping.Args["host"]);
        Assert.AreEqual(4, ping.Args["count"]);

        var ping10 = SshCommands.Ping("example.com", 10);
        Assert.AreEqual(10, ping10.Args["count"]);

        var dns = SshCommands.ResolveDns("example.com");
        Assert.AreEqual(SshCommandKind.ResolveDns, dns.Kind);
        Assert.AreEqual("example.com", dns.Args["name"]);

        var curl = SshCommands.CurlHead("https://example.com");
        Assert.AreEqual(SshCommandKind.CurlHead, curl.Kind);
        Assert.AreEqual("https://example.com", curl.Args["url"]);
    }

    [TestMethod]
    public void Containers_SetArgsAndKinds()
    {
        var ps = SshCommands.DockerPs();
        Assert.AreEqual(SshCommandKind.DockerPs, ps.Kind);
        Assert.AreEqual(0, ps.Args.Count);

        var logs = SshCommands.DockerLogs("abc123");
        Assert.AreEqual(SshCommandKind.DockerLogs, logs.Kind);
        Assert.AreEqual("abc123", logs.Args["id"]);
        Assert.AreEqual(200, logs.Args["lines"]);

        var logs50 = SshCommands.DockerLogs("abc123", 50);
        Assert.AreEqual(50, logs50.Args["lines"]);
    }
}

using Specky7;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TripleG3.SSH.WinUI.Models.SSH;

[Singleton<ISshHistoryStore>]
public sealed class FileSshHistoryStore : ISshHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private static string GetFolder()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(root, "SSH-Explorer", "history");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string San(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s;
    }

    private static string GetFileName(Profiles.Profile profile)
    {
        var name = $"{San(profile.Username)}@{San(profile.Address)}_{profile.Port}.jsonl";
        return Path.Combine(GetFolder(), name);
    }

    public async Task SaveAsync(SshSessionHistory session, CancellationToken ct = default)
    {
        var file = GetFileName(session.Profile);
        await using var fs = new FileStream(file, FileMode.Append, FileAccess.Write, FileShare.Read);
        await using var sw = new StreamWriter(fs, new UTF8Encoding(false));
        var json = JsonSerializer.Serialize(session, JsonOptions);
        await sw.WriteLineAsync(json.AsMemory(), ct);
        await sw.FlushAsync(ct);
    }

    public async Task<IReadOnlyList<SshSessionHistory>> LoadAsync(Profiles.Profile profile, CancellationToken ct = default)
    {
        var file = GetFileName(profile);
        var list = new List<SshSessionHistory>();
        if (!File.Exists(file)) return list;

        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs, Encoding.UTF8);
        while (!sr.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await sr.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var item = JsonSerializer.Deserialize<SshSessionHistory>(line, JsonOptions);
                if (item is not null) list.Add(item);
            }
            catch
            {
                // skip malformed lines
            }
        }
        return list;
    }
}
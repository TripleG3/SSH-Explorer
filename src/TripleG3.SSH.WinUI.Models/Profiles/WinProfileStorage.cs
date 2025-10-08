using Specky7;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace TripleG3.SSH.WinUI.Models.Profiles;

[Transient<IProfileStorage>]
[ExcludeFromCodeCoverage(Justification = "OS specific implementation")]
public sealed class WinProfileStorage : IProfileStorage
{
    public async ValueTask<bool> ExistsAsync(string fileName)
    {
        return await ApplicationData.Current.LocalFolder.TryGetItemAsync(fileName) != null;
    }

    public async ValueTask CreateOrReplaceAsync(string fileName, string content)
    {
        var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
        await FileIO.WriteTextAsync(file, content);
    }

    public async ValueTask<string> ReadAsync(string fileName)
    {
        var file = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
        return await FileIO.ReadTextAsync(file);
    }

    public async ValueTask DeleteAsync(string fileName)
    {
        var file = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
        await file.DeleteAsync();
    }

    public async ValueTask<IReadOnlyList<string>> EnumerateAsync(string extension)
    {
        var files = await ApplicationData.Current.LocalFolder.GetFilesAsync();
        return [.. files
            .Where(f => f.FileType.Equals(extension, StringComparison.OrdinalIgnoreCase))
            .Select(f => f.Name)];
    }
}

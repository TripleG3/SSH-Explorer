using System.Collections.Generic;
using System.Threading.Tasks;

namespace TripleG3.SSH.WinUI.Models.Profiles;

public interface IProfileStorage
{
    ValueTask<bool> ExistsAsync(string fileName);
    ValueTask CreateOrReplaceAsync(string fileName, string content);
    ValueTask<string> ReadAsync(string fileName);
    ValueTask DeleteAsync(string fileName);
    // Enumerate files by extension (e.g., ".sshprofile") in the profiles storage location
    ValueTask<IReadOnlyList<string>> EnumerateAsync(string extension);
}

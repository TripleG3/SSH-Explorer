using System;

namespace TripleG3.SSH.WinUI.Models.SSH;

public sealed record SshCommandExchange(Guid CommandId,
                                        DateTimeOffset StartedAt,
                                        DateTimeOffset EndedAt,
                                        string CommandText,
                                        int ExitCode,
                                        string StdOut,
                                        string StdErr);

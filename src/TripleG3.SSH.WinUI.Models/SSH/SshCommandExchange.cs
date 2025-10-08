using System;
using System.Diagnostics.CodeAnalysis;

namespace TripleG3.SSH.WinUI.Models.SSH;

[ExcludeFromCodeCoverage(Justification = "Data model")]
public sealed record SshCommandExchange(Guid CommandId,
                                        DateTimeOffset StartedAt,
                                        DateTimeOffset EndedAt,
                                        string CommandText,
                                        int ExitCode,
                                        string StdOut,
                                        string StdErr);

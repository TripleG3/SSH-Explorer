using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace TripleG3.SSH.WinUI.Models.SSH;

[ExcludeFromCodeCoverage(Justification = "Data model")]
public sealed record SshSessionHistory(Guid SessionId,
                                       Profiles.Profile Profile,
                                       DateTimeOffset StartedAt,
                                       DateTimeOffset EndedAt,
                                       List<SshCommandExchange> Exchanges,
                                       List<SshTimelineEntry> Transcript);

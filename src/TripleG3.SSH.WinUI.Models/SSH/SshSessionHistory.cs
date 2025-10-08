using System;
using System.Collections.Generic;

namespace TripleG3.SSH.WinUI.Models.SSH;

public sealed record SshSessionHistory(Guid SessionId,
                                       Profiles.Profile Profile,
                                       DateTimeOffset StartedAt,
                                       DateTimeOffset EndedAt,
                                       List<SshCommandExchange> Exchanges,
                                       List<SshTimelineEntry> Transcript);

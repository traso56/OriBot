using ConcurrentCollections;
using System.Collections.Concurrent;

namespace OriBot.Services;

public class VolatileData
{
    public ConcurrentHashSet<ulong> IgnoredDeletedMessagesIds { get; } = new ConcurrentHashSet<ulong>();
    public ConcurrentHashSet<ulong> IgnoredKickedUsersIds { get; } = new ConcurrentHashSet<ulong>();
    public ConcurrentDictionary<ulong, ulong> TicketThreads { get; } = new ConcurrentDictionary<ulong, ulong>();
}

namespace Lumora.Client.Core.Sync;

/// <summary>
/// The single in-memory source of truth for "recent clipboard entries in the active room" —
/// both the history window and the quick-paste popup read from this instead of each doing
/// their own decrypt/list-entries round trip, so they never disagree and both light up the
/// instant something is pushed or received. Cleared on room switch (see plan §Wybór przestrzeni).
/// </summary>
public sealed class ClipboardHistoryStore
{
    private const int MaxItems = 50;

    private readonly object gate = new();
    private readonly List<ClipboardHistoryItem> items = [];

    public event Action? Changed;

    public IReadOnlyList<ClipboardHistoryItem> Items
    {
        get
        {
            lock (gate)
            {
                return items.ToList();
            }
        }
    }

    public void ReplaceAll(IEnumerable<ClipboardHistoryItem> newItems)
    {
        lock (gate)
        {
            items.Clear();
            items.AddRange(newItems.OrderByDescending(i => i.CreatedAt).Take(MaxItems));
        }

        Changed?.Invoke();
    }

    public void Add(ClipboardHistoryItem item)
    {
        lock (gate)
        {
            items.RemoveAll(i => i.EntryId == item.EntryId);
            items.Insert(0, item);
            if (items.Count > MaxItems)
            {
                items.RemoveRange(MaxItems, items.Count - MaxItems);
            }
        }

        Changed?.Invoke();
    }

    public void Remove(Guid entryId)
    {
        lock (gate)
        {
            items.RemoveAll(i => i.EntryId == entryId);
        }

        Changed?.Invoke();
    }

    public void Clear()
    {
        lock (gate)
        {
            items.Clear();
        }

        Changed?.Invoke();
    }
}

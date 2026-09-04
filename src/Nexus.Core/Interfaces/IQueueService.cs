using System.Collections.ObjectModel;
using Nexus.Core.Models;

namespace Nexus.Core.Interfaces;

/// <summary>
/// Ordered pending-work store feeding the download manager. Supports reordering
/// and the full set of queue operations exposed in the Queue page.
/// </summary>
public interface IQueueService
{
    /// <summary>The ordered queue, bindable by the UI.</summary>
    ReadOnlyObservableCollection<DownloadTask> Items { get; }

    void Add(DownloadTask task);

    /// <summary>Adds many tasks, skipping duplicates by URL unless <paramref name="allowDuplicates"/>.</summary>
    void AddRange(IEnumerable<DownloadTask> tasks, bool allowDuplicates = false);

    void Remove(Guid taskId);

    /// <summary>Moves an item one position toward the front.</summary>
    void MoveUp(Guid taskId);

    /// <summary>Moves an item one position toward the back.</summary>
    void MoveDown(Guid taskId);

    /// <summary>Moves an item to an explicit index (drag-and-drop reordering).</summary>
    void MoveTo(Guid taskId, int newIndex);

    /// <summary>Removes and returns the next task to run, or null when empty.</summary>
    DownloadTask? Dequeue();

    void ClearCompleted();
    void ClearFailed();
    void Clear();

    /// <summary>True when the queue already contains a task for the given URL.</summary>
    bool ContainsUrl(string url);
}

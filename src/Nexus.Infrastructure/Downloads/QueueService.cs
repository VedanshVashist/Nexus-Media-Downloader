using System.Collections.ObjectModel;
using Nexus.Core.Enums;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;

namespace Nexus.Infrastructure.Downloads;

/// <summary>
/// Ordered, observable pending-work store. Thread-safe for mutation via a lock;
/// the exposed collection is intended for UI binding on the dispatcher thread.
/// Duplicate URLs are skipped by default to avoid accidental double downloads.
/// </summary>
public sealed class QueueService : IQueueService
{
    private readonly object _gate = new();
    private readonly ObservableCollection<DownloadTask> _items = [];
    private readonly ReadOnlyObservableCollection<DownloadTask> _readOnly;

    public QueueService()
    {
        _readOnly = new ReadOnlyObservableCollection<DownloadTask>(_items);
    }

    public ReadOnlyObservableCollection<DownloadTask> Items => _readOnly;

    public void Add(DownloadTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        lock (_gate)
        {
            _items.Add(task);
        }
    }

    public void AddRange(IEnumerable<DownloadTask> tasks, bool allowDuplicates = false)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        lock (_gate)
        {
            foreach (var task in tasks)
            {
                if (!allowDuplicates && ContainsUrlInternal(task.Url))
                {
                    continue;
                }

                _items.Add(task);
            }
        }
    }

    public void Remove(Guid taskId)
    {
        lock (_gate)
        {
            var item = Find(taskId);
            if (item is not null)
            {
                _items.Remove(item);
            }
        }
    }

    public void MoveUp(Guid taskId)
    {
        lock (_gate)
        {
            var index = IndexOf(taskId);
            if (index > 0)
            {
                _items.Move(index, index - 1);
            }
        }
    }

    public void MoveDown(Guid taskId)
    {
        lock (_gate)
        {
            var index = IndexOf(taskId);
            if (index >= 0 && index < _items.Count - 1)
            {
                _items.Move(index, index + 1);
            }
        }
    }

    public void MoveTo(Guid taskId, int newIndex)
    {
        lock (_gate)
        {
            var index = IndexOf(taskId);
            if (index < 0)
            {
                return;
            }

            newIndex = Math.Clamp(newIndex, 0, _items.Count - 1);
            if (index != newIndex)
            {
                _items.Move(index, newIndex);
            }
        }
    }

    public DownloadTask? Dequeue()
    {
        lock (_gate)
        {
            // Return the first item that is ready to run.
            for (var i = 0; i < _items.Count; i++)
            {
                var candidate = _items[i];
                if (candidate.Status is DownloadStatus.Created or DownloadStatus.Queued)
                {
                    _items.RemoveAt(i);
                    return candidate;
                }
            }

            return null;
        }
    }

    public void ClearCompleted()
    {
        lock (_gate)
        {
            RemoveWhere(t => t.Status == DownloadStatus.Completed);
        }
    }

    public void ClearFailed()
    {
        lock (_gate)
        {
            RemoveWhere(t => t.Status is DownloadStatus.Failed or DownloadStatus.Cancelled);
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _items.Clear();
        }
    }

    public bool ContainsUrl(string url)
    {
        lock (_gate)
        {
            return ContainsUrlInternal(url);
        }
    }

    private bool ContainsUrlInternal(string url) =>
        _items.Any(t => string.Equals(t.Url, url, StringComparison.OrdinalIgnoreCase));

    private DownloadTask? Find(Guid id) => _items.FirstOrDefault(t => t.Id == id);

    private int IndexOf(Guid id)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            if (_items[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }

    private void RemoveWhere(Func<DownloadTask, bool> predicate)
    {
        for (var i = _items.Count - 1; i >= 0; i--)
        {
            if (predicate(_items[i]))
            {
                _items.RemoveAt(i);
            }
        }
    }
}

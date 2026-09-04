using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Nexus.App.ViewModels;

/// <summary>
/// Keeps a target collection of view-models in sync with a source domain collection,
/// one-for-one by index. The engine owns the source <see cref="ReadOnlyObservableCollection{T}"/>
/// (mutated only on the UI thread), and this projection mirrors every add/remove/move/reset
/// into freshly created view-models, disposing the ones that fall out.
/// </summary>
/// <typeparam name="TSource">Domain item type (e.g. <c>DownloadTask</c>).</typeparam>
/// <typeparam name="TTarget">View-model wrapper type.</typeparam>
public sealed class ObservableProjection<TSource, TTarget> : IDisposable
{
    private readonly ObservableCollection<TTarget> _target = [];
    private readonly ReadOnlyObservableCollection<TSource> _source;
    private readonly Func<TSource, TTarget> _factory;
    private readonly Action<TTarget>? _onRemoved;
    private bool _disposed;

    public ObservableProjection(
        ReadOnlyObservableCollection<TSource> source,
        Func<TSource, TTarget> factory,
        Action<TTarget>? onRemoved = null)
    {
        _source = source;
        _factory = factory;
        _onRemoved = onRemoved;

        foreach (var item in source)
        {
            _target.Add(factory(item));
        }

        Items = new ReadOnlyObservableCollection<TTarget>(_target);
        ((INotifyCollectionChanged)_source).CollectionChanged += OnSourceChanged;
    }

    /// <summary>The mirrored, bindable collection of view-models.</summary>
    public ReadOnlyObservableCollection<TTarget> Items { get; }

    private void OnSourceChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                InsertRange(e.NewStartingIndex, e.NewItems);
                break;

            case NotifyCollectionChangedAction.Remove:
                RemoveRange(e.OldStartingIndex, e.OldItems?.Count ?? 0);
                break;

            case NotifyCollectionChangedAction.Replace:
                RemoveRange(e.OldStartingIndex, e.OldItems?.Count ?? 0);
                InsertRange(e.NewStartingIndex, e.NewItems);
                break;

            case NotifyCollectionChangedAction.Move:
                if (e.OldStartingIndex >= 0 && e.NewStartingIndex >= 0)
                {
                    _target.Move(e.OldStartingIndex, e.NewStartingIndex);
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                Rebuild();
                break;
        }
    }

    private void InsertRange(int startIndex, System.Collections.IList? items)
    {
        if (items is null)
        {
            return;
        }

        var index = startIndex >= 0 ? startIndex : _target.Count;
        foreach (var item in items)
        {
            _target.Insert(index++, _factory((TSource)item!));
        }
    }

    private void RemoveRange(int startIndex, int count)
    {
        if (startIndex < 0)
        {
            return;
        }

        for (var i = 0; i < count && startIndex < _target.Count; i++)
        {
            var removed = _target[startIndex];
            _target.RemoveAt(startIndex);
            _onRemoved?.Invoke(removed);
        }
    }

    private void Rebuild()
    {
        foreach (var existing in _target)
        {
            _onRemoved?.Invoke(existing);
        }

        _target.Clear();
        foreach (var item in _source)
        {
            _target.Add(_factory(item));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ((INotifyCollectionChanged)_source).CollectionChanged -= OnSourceChanged;

        foreach (var item in _target)
        {
            _onRemoved?.Invoke(item);
        }

        _target.Clear();
    }
}

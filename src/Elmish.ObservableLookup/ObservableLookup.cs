namespace Elmish.ObservableLookup;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

/// <summary>
/// A mutable lookup implementing <see cref="ILookup{TKey,TElement}"/>
/// </summary>
/// <typeparam name="TKey">The lookup key.</typeparam>
/// <typeparam name="TElement">The elements under each <typeparamref name="TKey"/>.</typeparam>
public class ObservableLookup<TKey, TElement> : IMutableLookup<TKey, TElement>, INotifyCollectionChanged, IReadOnlyList<IGroupingList<TKey, TElement>>
    where TKey : notnull
{
    public ObservableLookup() : this(Comparer<TKey>.Default) { }

    public ObservableLookup(IComparer<TKey> comparer)
    {
        comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));

        this.groupings = new SortedDictionary<TKey, ObservableGrouping<TKey, TElement>>(comparer);
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public bool ReuseGroups
    {
        get => this.reuseGroups;
        set
        {
            if (this.reuseGroups == value)
                return;

            this.reuseGroups = value;
            if (value)
                this.oldGroups = new Dictionary<TKey, ObservableGrouping<TKey, TElement>>();
            else
                this.oldGroups = null;
        }
    }

    public SortedDictionary<TKey, ObservableGrouping<TKey, TElement>>.KeyCollection Keys => groupings.Keys;

    public IComparer<TKey> Comparer => groupings.Comparer;

    private int CountIndex (TKey key) => this.groupings.Keys.Count(k => this.groupings.Comparer.Compare(k, key) > 0);

    /// <summary>
    /// Adds <paramref name="element"/> under the specified <paramref name="key"/>. <paramref name="key"/> does not need to exist.
    /// </summary>
    /// <param name="key">The key to add <paramref name="element"/> under.</param>
    /// <param name="element">The element to add.</param>
    public void Add(TKey key, TElement element)
    {
        if (!this.groupings.TryGetValue(key, out var grouping))
        {
            if (ReuseGroups && !this.oldGroups!.TryGetValueAndRemove(key, out grouping))
                grouping = new ObservableGrouping<TKey, TElement>(key);

            this.groupings.Add(key, grouping!);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (object?)grouping, CountIndex(key)));
        }

        grouping!.Add(element);
    }

    public void Add(TKey key, IEnumerable<TElement> elements)
    {
        elements = elements ?? throw new ArgumentNullException(nameof(elements));

        if (!this.groupings.TryGetValue(key, out var grouping))
        {
            if (ReuseGroups && !this.oldGroups!.TryGetValueAndRemove(key, out grouping))
                grouping = new ObservableGrouping<TKey, TElement>(key);

            grouping!.AddRange(elements);
            if (grouping!.Count == 0)
            {
                if (ReuseGroups)
                    this.oldGroups!.Add(grouping.Key, grouping);

                return;
            }

            this.groupings.Add(key, grouping);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (object)grouping, CountIndex(key)));
        }
    }

    public void Add(IGrouping<TKey, TElement> grouping)
    {
        grouping = grouping ?? throw new ArgumentNullException(nameof(grouping));

        ObservableGrouping<TKey, TElement>? og = null;
        var key = grouping.Key;
        if (ReuseGroups && !this.oldGroups!.TryGetValueAndRemove(key, out og))
        {
            og = new ObservableGrouping<TKey, TElement>(key);
        }

        og!.AddRange(grouping);
        if (og!.Count == 0)
        {
            if (ReuseGroups)
                this.oldGroups!.Add(key, og);

            return;
        }

        this.groupings.Add(key, og);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (object)og, CountIndex(key)));
    }

    //public void Insert(int index, IGrouping<TKey, TElement> grouping)
    //{
    //    ObservableGrouping<TKey, TElement>? og = null;
    //    if (ReuseGroups && !this.oldGroups!.TryGetValueAndRemove(grouping.Key, out og))
    //    {
    //        og = new ObservableGrouping<TKey, TElement>(grouping.Key);
    //    }

    //    og!.AddRange(grouping);
    //    if (og!.Count == 0)
    //    {
    //        if (ReuseGroups)
    //            this.oldGroups!.Add(grouping.Key, og);

    //        return;
    //    }

    //    this.groupings.Insert(index, og.Key, og);
    //    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (object)og, index));
    //}

    /// <summary>
    /// Removes <paramref name="element"/> from the <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key that <paramref name="element"/> is located under.</param>
    /// <param name="element">The element to remove from <paramref name="key"/>. </param>
    /// <returns><c>true</c> if <paramref name="key"/> and <paramref name="element"/> existed, <c>false</c> if not.</returns>
    public bool Remove(TKey key, TElement element)
    {
        element = element ?? throw new ArgumentNullException(nameof(element));

        if (!this.groupings.TryGetValue(key, out var group))
            return false;

        return group.Remove(element);
    }

    /// <summary>
    /// Removes <paramref name="key"/> from the lookup.
    /// </summary>
    /// <param name="key">They to remove.</param>
    /// <returns><c>true</c> if <paramref name="key"/> existed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
    public bool Remove(TKey key)
    {
        if (this.groupings.TryGetValueAndRemove(key, out var g))
        {
            if (ReuseGroups)
            {
                g!.Clear();
                this.oldGroups!.Add(key, g);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, (object?)g, CountIndex(key)));
            return true;
        }

        return false;
    }

    public void Clear()
    {
        if (ReuseGroups)
        {
            foreach (var g in this.groupings.Values)
            {
                this.oldGroups!.Add(g.Key, g);
                g.Clear();
            }
        }

        this.groupings.Clear();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public bool TryGetValues(TKey key, out IEnumerable<TElement> values)
    {
        key = key ?? throw new ArgumentNullException(nameof(key));

        if (!this.groupings.TryGetValue(key, out var grouping))
        {
            values = Enumerable.Empty<TElement>();
            return false;
        }
        else
        {
            values = grouping;
            return true;
        }
    }

    /// <summary>
    /// Gets the elements for <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to get the elements for.</param>
    /// <returns>The elements under <paramref name="key"/>.</returns>
    public ObservableGrouping<TKey, TElement> this[TKey key]
    {
        get
        {
            if (this.groupings.TryGetValue(key, out var grouping))
                return grouping;
            else
                throw new KeyNotFoundException();
        }
    }

    #region ILookup Members
    /// <summary>
    /// Gets the number of groupings.
    /// </summary>
    public int Count => this.groupings.Count;

    IGroupingList<TKey, TElement> IReadOnlyList<IGroupingList<TKey, TElement>>.this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return (IGroupingList<TKey, TElement>)groupings.Skip(index).First().Value;
        }
    }

    /// <summary>
    /// Gets the elements for <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to get the elements for.</param>
    /// <returns>The elements under <paramref name="key"/>.</returns>
    IEnumerable<TElement> ILookup<TKey, TElement>.this[TKey key]
    {
        get
        {
            if (this.groupings.TryGetValue(key, out var grouping))
                return grouping;

            return Enumerable.Empty<TElement>();
        }
    }

    /// <summary>
    /// Gets whether or not there's a grouping for <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to check for.</param>
    /// <returns><c>true</c> if <paramref name="key"/> is present.</returns>
    public bool Contains(TKey key) => this.groupings.ContainsKey(key);

    public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
    {
        foreach (var g in this.groupings.Values)
            yield return g;
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();
    #endregion

    private readonly SortedDictionary<TKey, ObservableGrouping<TKey, TElement>> groupings;

    private bool reuseGroups;
    private Dictionary<TKey, ObservableGrouping<TKey, TElement>>? oldGroups;

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

    IEnumerator<IGroupingList<TKey, TElement>> IEnumerable<IGroupingList<TKey, TElement>>.GetEnumerator()
    {
        foreach (var g in this.groupings.Values)
            yield return g;
    }
}

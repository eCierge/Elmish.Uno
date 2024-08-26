namespace Elmish.Collections;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

internal static class DictionaryExtensions
{
    public static bool TryGetValueAndRemove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out TValue? value)
    {
        var result = dictionary.TryGetValue(key, out value);
        if (result)
            dictionary.Remove(key);
        return result;
    }
}

internal static class ObservableCollectionExtensions
{
    public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        foreach (T item in items)
            collection.Add(item);
    }
}

public static class ObservableLookupExtensions
{
    public static ObservableLookup<TKey, TElement> ToObservableLookup<TKey, TElement>([NotNull] this IEnumerable<TElement> source, Func<TElement, TKey> keySelector)
        where TKey : notnull
    {
        keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        var lookup = new ObservableLookup<TKey, TElement>();
        foreach (var grouping in source.GroupBy(keySelector))
            lookup.Add(grouping);
        return lookup;
    }

    public static ObservableLookup<TKey, TElement> ToObservableLookup<TKey, TElement>([NotNull] this IEnumerable<TElement> source, IComparer<TKey> comparer, Func<TElement, TKey> keySelector)
        where TKey : notnull
    {
        keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        var lookup = new ObservableLookup<TKey, TElement>(comparer);
        foreach (var grouping in source.GroupBy(keySelector))
            lookup.Add(grouping);
        return lookup;
    }
}

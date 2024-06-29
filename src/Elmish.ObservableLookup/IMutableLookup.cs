namespace Elmish.ObservableLookup;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IMutableLookup<TKey, TElement> : ILookup<TKey, TElement>
{
    /// <summary>
    /// Adds <paramref name="element"/> under the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to add <paramref name="element"/> under.</param>
    /// <param name="element">The element to add.</param>
    void Add(TKey key, TElement element);

    /// <summary>
    /// Adds <paramref name="elements"/> under the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">They key to add <paramref name="elements"/> under.</param>
    /// <param name="elements">The elements to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="elements"/> is <c>null</c>.</exception>
    void Add(TKey key, IEnumerable<TElement> elements);

    /// <summary>
    /// Removes <paramref name="element"/> from the <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key that <paramref name="element"/> is located under.</param>
    /// <param name="element">The element to remove from <paramref name="key"/>. </param>
    /// <returns><c>true</c> if <paramref name="key"/> and <paramref name="element"/> existed, <c>false</c> if not.</returns>
    bool Remove(TKey key, TElement element);

    /// <summary>
    /// Removes <paramref name="key"/> from the lookup.
    /// </summary>
    /// <param name="key">They to remove.</param>
    /// <returns><c>true</c> if <paramref name="key"/> existed.</returns>
    bool Remove(TKey key);

    /// <summary>
    /// Clears the lookup.
    /// </summary>
    void Clear();
}

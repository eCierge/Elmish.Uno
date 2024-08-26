namespace Elmish.Collections;

using System.Collections.ObjectModel;

public interface IGroupingList<TKey, TElement> : IGrouping<TKey, TElement>, IList<TElement> { }

public class ObservableGrouping<TKey, TElement> : ObservableCollection<TElement>, IGroupingList<TKey, TElement>
{
    public TKey Key { get; }

    public ObservableGrouping(TKey key) => Key = key;

    public ObservableGrouping(IGrouping<TKey, TElement> grouping)
    {
        grouping = grouping ?? throw new ArgumentNullException(nameof(grouping));
        Key = grouping.Key;
        foreach (TElement item in grouping)
        {
            this.Add(item);
        }
    }
}

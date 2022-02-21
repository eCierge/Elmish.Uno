using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Windows.UI.Xaml.Controls;

namespace Elmish.Uno.Navigation
{
    /// <summary>
    /// Class that allows to navigate to pages within an attached <see cref="Frame"/>
    /// using a dictionary map of page names and their types.
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly Frame frame;
        private readonly IReadOnlyDictionary<string, Type> pageMap;

        /// <summary>
        /// Creates a NavigationService instance attaching a <see cref="Frame"/>
        /// and specifying a map of pages names and their types.
        /// </summary>
        /// <param name="frame">Frame to use for navigation.</param>
        /// <param name="pageMap">Map of page names to page types.</param>
        public NavigationService(Frame frame, IReadOnlyDictionary<string, Type> pageMap)
        {
            this.frame = frame;
            this.pageMap = pageMap;
        }

        /// <summary>
        /// Creates a NavigationService instance attaching a <see cref="Frame"/>
        /// and specifying a list of pages names and their types pairs.
        /// </summary>
        /// <param name="frame">Frame to use for navigation.</param>
        /// <param name="pageMap">Map of page names to page types.</param>
        public NavigationService(Frame frame, IEnumerable<KeyValuePair<string, Type>> pageMap)
        {
            this.frame = frame;
            this.pageMap = pageMap.Aggregate(
                    ImmutableDictionary<string, Type>.Empty.ToBuilder(),
                    (builder, kvp) => { builder.Add(kvp.Key, kvp.Value); return builder; })
                .ToImmutable();
        }

        /// <summary>
        /// Gets the number of pages in the navigation history that can be cached for the <see cref="Frame"/>.
        /// </summary>
        public int CacheSize => frame.CacheSize;

        /// <summary>
        /// Gets the number of entries in the navigation back stack.
        /// </summary>
        public int BackStackDepth => frame.BackStackDepth;

        /// <summary>
        /// Gets a value that indicates whether there is at least one entry in
        /// back navigation history.
        /// </summary>
        public bool CanGoBack => frame.CanGoBack;

        /// <summary>
        /// Gets a value that indicates whether there is at least one entry in
        /// forward navigation history.
        /// </summary>
        public bool CanGoForward => frame.CanGoForward;

        /// <summary>
        /// Serializes the <see cref="Frame"/> navigation history into a string.
        /// </summary>
        /// <returns>The string-form serialized navigation history.</returns>
        public string GetNavigationState() => frame.GetNavigationState();
        /// <summary>
        /// Reads and restores the navigation history of a <see cref="Frame"/> from
        /// a provided serialization string.
        /// </summary>
        /// <param name="navigationState">
        /// The serialization string that supplies the restore point for navigation history.
        /// </param>
        public void SetNavigationState(string navigationState) => frame.SetNavigationState(navigationState);
        /// <summary>
        /// Navigates to the most recent item in back navigation history,
        /// if a Frame manages its own navigation history.
        /// </summary>
        public void GoBack() => frame.GoBack();
        /// <summary>
        /// Navigates to the most recent item in forward navigation history,
        /// if a Frame manages its own navigation history.
        /// </summary>
        public void GoForward() => frame.GoForward();
        /// <summary>
        /// Navigates an attached <see cref="Frame"/> to a page specified my its name.
        /// </summary>
        /// <param name="name">Page name</param>
        /// <returns>True if navigation succeeded.</returns>
        public bool Navigate(string name) => frame.Navigate(pageMap[name], null);
        /// <summary>
        /// Navigates an attached <see cref="Frame"/> to a page specified my its name.
        /// </summary>
        /// <param name="name">Page name</param>
        /// <param name="navigationParams">Parameter to be passed to a page</param>
        /// <returns>True if navigation succeeded.</returns>
        public bool Navigate(string name, IReadOnlyDictionary<string, object> navigationParams) => frame.Navigate(pageMap[name], navigationParams);
    }
}

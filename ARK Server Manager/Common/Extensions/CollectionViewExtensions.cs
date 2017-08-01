using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public static class CollectionViewExtensions
    {
        public static void ToggleSorting(this ICollectionView view, string property, ListSortDirection defaultDirection = ListSortDirection.Ascending)
        {
            for (int i = 0; i < view.SortDescriptions.Count; i++)
            {
                var sortDescription = view.SortDescriptions[i];
                if (sortDescription.PropertyName == property)
                {
                    view.SortDescriptions.RemoveAt(i);
                    return;
                }
            }

            view.SortDescriptions.Add(new SortDescription() { PropertyName = property, Direction = defaultDirection });
        }

        public static void ToggleSortDirection(this ICollectionView view, string property, ListSortDirection defaultDirection = ListSortDirection.Ascending)
        {
            for (int i = 0; i < view.SortDescriptions.Count; i++)
            {
                var sortDescription = view.SortDescriptions[i];
                if (sortDescription.PropertyName == property)
                {
                    if (sortDescription.Direction == ListSortDirection.Ascending)
                    {
                        view.SortDescriptions[i] = new SortDescription() { PropertyName = property, Direction = ListSortDirection.Descending };
                    }
                    else
                    {
                        view.SortDescriptions[i] = new SortDescription() { PropertyName = property, Direction = ListSortDirection.Ascending };
                    }

                    return;
                }
            }

            view.SortDescriptions.Add(new SortDescription() { PropertyName = property, Direction = defaultDirection });
        }
    }
}

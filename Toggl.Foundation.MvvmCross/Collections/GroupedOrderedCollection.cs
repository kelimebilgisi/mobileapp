using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.Collections
{
    public class GroupedOrderedCollection<TItem> : IGroupOrderedCollection<TItem>
    {
        private ImmutableList<ImmutableList<TItem>> sections;
        private Func<TItem, IComparable> indexKey;
        private Func<TItem, IComparable> orderingKey;
        private Func<TItem, IComparable> groupingKey;
        private bool isDescending;

        public bool IsEmpty
            => sections.Count == 0;

        public int TotalCount
            => sections.Sum(section => section.Count);

        public int Count
            => sections.Count;

        public GroupedOrderedCollection(
            Func<TItem, IComparable> indexKey,
            Func<TItem, IComparable> orderingKey,
            Func<TItem, IComparable> groupingKey,
            bool isDescending = false)
        {
            this.indexKey = indexKey;
            this.orderingKey = orderingKey;
            this.groupingKey = groupingKey;
            this.isDescending = isDescending;

            sections = ImmutableList<ImmutableList<TItem>>.Empty;
        }

        public IEnumerator<IReadOnlyList<TItem>> GetEnumerator()
            => sections.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public IReadOnlyList<TItem> this[int index]
            => sections[index];

        public SectionedIndex? IndexOf(TItem item)
        {
            var sectionIndex = sections.GroupIndexOf(item, groupingKey);

            if (sectionIndex == -1)
                return null;

            var rowIndex = sections[sectionIndex].IndexOf(item, indexKey);
            if (rowIndex == -1)
                return null;

            return new SectionedIndex(sectionIndex, rowIndex);
        }

        public SectionedIndex? IndexOf(IComparable itemId)
        {
            for (int section = 0; section < sections.Count; section++)
            {
                var row = sections[section].IndexOf(item => indexKey(item).CompareTo(itemId) == 0);
                if (row != -1)
                {
                    return new SectionedIndex(section, row);
                }
            }

            return null;
        }

        public int? FitsIntoSection(TItem item)
        {
            var sectionIndex = sections.GroupIndexOf(item, groupingKey);
            return sectionIndex == -1 ? (int?)null : sectionIndex;
        }

        public (SectionedIndex index, bool needsNewSection) InsertItem(TItem item)
        {
            var sectionIndex = sections.GroupIndexOf(item, groupingKey);
            if (sectionIndex == -1)
            {
                var insertionIndex = sections.FindLastIndex(g => areInOrder(g.First(), item, groupingKey));
                var list = ImmutableList.Create(item);
                sections = sections.Insert(insertionIndex + 1, list); // when there are no sections the insertionIndex will be -1
                return (new SectionedIndex(insertionIndex + 1, 0), true);
            }

            var rowIndex = sections[sectionIndex].FindLastIndex(i => areInOrder(i, item, orderingKey));
            var affectedSection = sections[sectionIndex].Insert(rowIndex + 1, item);
            sections = sections.Replace(sections[sectionIndex], affectedSection); // when the section is empty, the rowIndex will be -1
            return (new SectionedIndex(sectionIndex, rowIndex + 1), false);
        }

        public SectionedIndex? UpdateItem(IComparable key, TItem item)
        {
            var oldIndex = IndexOf(key);

            if (!oldIndex.HasValue)
                return null;

            RemoveItemAt(oldIndex.Value.Section, oldIndex.Value.Row);
            return InsertItem(item).index;
        }

        public void ReplaceWith(IEnumerable<TItem> items)
        {
            sections = items
                .GroupBy(groupingKey)
                .Select(g => g.OrderBy(orderingKey, isDescending).ToImmutableList())
                .OrderBy(g => groupingKey(g.First()), isDescending)
                .ToImmutableList();
        }

        public TItem RemoveItemAt(int section, int row)
        {
            var item = sections[section][row];
            removeItemFromSection(section, row);
            return item;
        }

        private bool areInOrder(TItem ob1, TItem ob2, Func<TItem, IComparable> key)
        {
            return isDescending
                ? key(ob1).CompareTo(key(ob2)) > 0
                : key(ob1).CompareTo(key(ob2)) < 0;
        }

        private void removeItemFromSection(int section, int row)
        {
            var affectedSection = sections[section].RemoveAt(row);
            sections = sections.Replace(sections[section], affectedSection);

            if (sections[section].Count == 0)
            {
                sections = sections.RemoveAt(section);
            }
        }
    }
}

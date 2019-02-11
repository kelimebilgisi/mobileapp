using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Foundation.MvvmCross.Collections.Changes;

namespace Toggl.Foundation.MvvmCross.Collections
{
    public class ObservableGroupedOrderedCollection<TItem> : IGroupOrderedCollection<TItem>
    {
        private Func<TItem, IComparable> indexKey;
        private readonly ISubject<ICollectionChange> collectionChangesSubject = new Subject<ICollectionChange>();
        private readonly GroupedOrderedCollection<TItem> collection;

        public IObservable<ICollectionChange> CollectionChange
            => collectionChangesSubject.AsObservable();

        public IObservable<bool> Empty
            => collectionChangesSubject
                .AsObservable()
                .Select(_ => IsEmpty)
                .StartWith(IsEmpty)
                .DistinctUntilChanged();

        public IObservable<int> TotalCount
            => collectionChangesSubject
                .AsObservable()
                .Select(_ => collection.TotalCount)
                .StartWith(0)
                .DistinctUntilChanged();

        public bool IsEmpty
            => collection.IsEmpty;

        public int Count
            => collection.Count;

        public ObservableGroupedOrderedCollection(Func<TItem, IComparable> indexKey, Func<TItem, IComparable> orderingKey, Func<TItem, IComparable> groupingKey, bool descending = false)
        {
            this.indexKey = indexKey;
            collection = new GroupedOrderedCollection<TItem>(indexKey, orderingKey, groupingKey, descending);
        }

        public IEnumerator<IReadOnlyList<TItem>> GetEnumerator()
            => collection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public IReadOnlyList<TItem> this[int index]
            => collection[index];

        public SectionedIndex? IndexOf(TItem item)
        {
            return collection.IndexOf(item);
        }

        public SectionedIndex? IndexOf(IComparable itemId)
        {
            return collection.IndexOf(itemId);
        }

        public (SectionedIndex index, bool needsNewSection) InsertItem(TItem item)
        {
            var (index, needsNewSection) = collection.InsertItem(item);

            if (needsNewSection)
            {
                collectionChangesSubject.OnNext(new InsertSectionCollectionChange<TItem>(index.Section, item));
            }
            else
            {
                collectionChangesSubject.OnNext(new AddRowCollectionChange<TItem>(index, item));
            }

            return (index, needsNewSection);
        }

        public SectionedIndex? UpdateItem(IComparable key, TItem item)
        {
            var oldIndex = collection.IndexOf(key);
            if (!oldIndex.HasValue)
            {
                return InsertItem(item).index;
            }

            var section = collection.FitsIntoSection(item);
            var movesToDifferentSection = !section.HasValue || section.Value != oldIndex.Value.Section;
            collection.RemoveItemAt(oldIndex.Value.Section, oldIndex.Value.Row);
            var (newIndex, needsNewSection) = collection.InsertItem(item);

            if (!movesToDifferentSection && oldIndex.Value.Equals(newIndex))
            {
                collectionChangesSubject.OnNext(new UpdateRowCollectionChange<TItem>(newIndex, item));
            }
            else
            {
                if (needsNewSection)
                {
                    collectionChangesSubject.OnNext(new MoveRowToNewSectionCollectionChange<TItem>(oldIndex.Value, newIndex.Section, item));
                }
                else
                {
                    collectionChangesSubject.OnNext(new MoveRowWithinExistingSectionsCollectionChange<TItem>(oldIndex.Value, newIndex, item, movesToDifferentSection));
                }
            }

            return newIndex;
        }

        public void ReplaceWith(IEnumerable<TItem> items)
        {
            collection.ReplaceWith(items);

            collectionChangesSubject.OnNext(new ReloadCollectionChange());
        }

        public TItem RemoveItemAt(int section, int row)
        {
            var index = new SectionedIndex(section, row);
            var item = collection.RemoveItemAt(section, row);

            collectionChangesSubject.OnNext(new RemoveRowCollectionChange(index));

            return item;
        }
    }
}

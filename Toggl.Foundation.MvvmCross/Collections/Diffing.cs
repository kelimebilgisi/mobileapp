using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Foundation.MvvmCross.Collections;

namespace Toggl.Daneel.ViewSources.Generic
{
    public class DuplicateItemException : Exception
    {
        public long DuplicatedIdentity { get; }

        public DuplicateItemException(long identity)
        {
            DuplicatedIdentity = identity;
        }
    }

    public class DuplicateSectionException : Exception
    {
        public long DuplicatedIdentity { get; }

        public DuplicateSectionException(long identity)
        {
            DuplicatedIdentity = identity;
        }
    }

    public class ItemPath : IEquatable<ItemPath>
    {
        public int sectionIndex { get; }
        public int itemIndex { get; }

        public ItemPath(int sectionIndex, int itemIndex)
        {
            this.sectionIndex = sectionIndex;
            this.itemIndex = itemIndex;
        }

        public bool Equals(ItemPath other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return sectionIndex == other.sectionIndex && itemIndex == other.itemIndex;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ItemPath)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (sectionIndex * 397) ^ itemIndex;
            }
        }
    }

    enum EditEvent
    {
        Inserted,
        InsertedAutomatically,
        Deleted,
        DeletedAutomatically,
        Moved,
        MovedAutomatically,
        Untouched
    }

    class SectionAssociatedData
    {
        public EditEvent EditEvent { get; set; }
        public int? IndexAfterDelete { get; set; }
        public int? MoveIndex { get; set; }
        public int? ItemCount { get; set; }

        public SectionAssociatedData(EditEvent editEvent, int? indexAfterDelete, int? moveIndex, int? itemCount)
        {
            EditEvent = editEvent;
            IndexAfterDelete = indexAfterDelete;
            MoveIndex = moveIndex;
            ItemCount = itemCount;
        }

        public static SectionAssociatedData Initial()
        {
            return new SectionAssociatedData(EditEvent.Untouched, null, null, 0);
        }
    }

    class ItemAssociatedData
    {
        public EditEvent EditEvent { get; set; }
        public int? IndexAfterDelete { get; set; }
        public ItemPath MoveIndex { get; set; }

        public ItemAssociatedData(EditEvent editEvent, int? indexAfterDelete, ItemPath moveIndex)
        {
            EditEvent = editEvent;
            IndexAfterDelete = indexAfterDelete;
            MoveIndex = moveIndex;
        }

        public static ItemAssociatedData Initial()
        {
            return new ItemAssociatedData(EditEvent.Untouched, null, null);
        }
    }

    public class Diffing<TSection, THeader, TElement>
        where TSection : IAnimatableSectionModel<THeader, TElement>, new()
        where TElement : IDiffable, IEquatable<TElement>
        where THeader : IDiffable
    {

        public class Changeset
        {
            public List<TSection> OriginalSections { get; }
            public List<TSection> FinalSections { get; }

            public List<int> InsertedSections { get; }
            public List<int> DeletedSections { get; }
            public List<(int, int)> MovedSections { get; }
            public List<int> UpdatedSections { get; }
            public List<ItemPath> InsertedItems { get; }
            public List<ItemPath> DeletedItems { get; }
            public List<(ItemPath, ItemPath)> MovedItems { get; }
            public List<ItemPath> UpdatedItems { get; }

            public Changeset(
                List<TSection> originalSections = null,
                List<TSection> finalSections = null,
                List<int> insertedSections = null,
                List<int> deletedSections = null,
                List<(int, int)> movedSections = null,
                List<int> updatedSections = null,
                List<ItemPath> insertedItems = null,
                List<ItemPath> deletedItems = null,
                List<(ItemPath, ItemPath)> movedItems = null,
                List<ItemPath> updatedItems = null)
            {
                OriginalSections = originalSections ?? new List<TSection>();
                FinalSections = finalSections ?? new List<TSection>();

                InsertedSections = insertedSections ?? new List<int>();
                DeletedSections = deletedSections ?? new List<int>();
                MovedSections = movedSections ?? new List<(int, int)>();
                UpdatedSections = updatedSections ?? new List<int>();

                InsertedItems = insertedItems ?? new List<ItemPath>();
                DeletedItems = deletedItems ?? new List<ItemPath>();
                MovedItems = movedItems ?? new List<(ItemPath, ItemPath)>();
                UpdatedItems = updatedItems ?? new List<ItemPath>();
            }

            public static Changeset initialValue(List<TSection> sections) {
                return new Changeset(
                    finalSections: sections,
                    insertedSections: Enumerable.Range(0, sections.Count).ToList()
                );
            }
        }


        private List<TSection> initialSections;
        private List<TSection> finalSections;

        private List<List<TElement>> initialItemCache;
        private List<List<TElement>> finalItemCache;

        private List<SectionAssociatedData> initialSectionData;
        private List<SectionAssociatedData> finalSectionData;

        private List<List<ItemAssociatedData>> initialItemData;
        private List<List<ItemAssociatedData>> finalItemData;

        public Diffing(IEnumerable<TSection> initialSections, IEnumerable<TSection> finalSections)
        {
            this.initialSections = initialSections.ToList();
            this.finalSections = finalSections.ToList();
        }

        public List<Changeset> computeDifferences()
        {
            (initialSectionData, finalSectionData) = calculateSectionMovements(initialSections.ToList(), finalSections.ToList());

            initialItemCache = initialSections.Select(collection => collection.Items.ToList()).ToList();
            finalItemCache = finalSections.Select(collection => collection.Items.ToList()).ToList();

            var result = Enumerable.Empty<Changeset>().ToList();

            (initialItemData, finalItemData) = calculateItemMovements(
                initialItemCache,
                finalItemCache,
                initialSectionData,
                finalSectionData
            );

            result.AddRange(generateDeleteSectionsDeletedItemsAndUpdatedItems());
            result.AddRange(generateInsertAndMoveSections());
            result.AddRange(generateInsertAndMovedItems());

            return result;
        }

        private static (List<SectionAssociatedData>, List<SectionAssociatedData>) calculateSectionMovements(
            List<TSection> initialSections, List<TSection> finalSections)
        {
            var initialSectionIndexes = indexSections(initialSections);

            var initialSectionData = Enumerable.Range(0, initialSections.Count)
                .Select(_ => SectionAssociatedData.Initial()).ToList();
            var finalSectionData = Enumerable.Range(0, finalSections.Count)
                .Select(_ => SectionAssociatedData.Initial()).ToList();

            for (var i = 0; i < finalSections.Count; i++)
            {
                var section = finalSections[i];

                finalSectionData[i].ItemCount = finalSections[i].Items.Count;

                if (!initialSectionIndexes.ContainsKey(section.Identity))
                {
                    continue;
                }
                var initialSectionIndex = initialSectionIndexes[section.Identity];

                if (initialSectionData[initialSectionIndex].MoveIndex.HasValue)
                {
                    throw new DuplicateSectionException(section.Identity);
                }

                initialSectionData[initialSectionIndex].MoveIndex = i;
                finalSectionData[i].MoveIndex = initialSectionIndex;
            }

            var sectionIndexAfterDelete = 0;

            // deleted sections

            for (var i = 0; i < initialSectionData.Count(); i++)
            {
                initialSectionData[i].ItemCount = initialSections[i].Items.Count;
                if (initialSectionData[i].MoveIndex == null)
                {
                    initialSectionData[i].EditEvent = EditEvent.Deleted;
                    continue;
                }

                initialSectionData[i].IndexAfterDelete = sectionIndexAfterDelete;
                sectionIndexAfterDelete += 1;
            }

            // moved sections

            int? untouchedOldIndex = 0;
            int? findNextUntouchedOldIndex(int? initialSearchIndex)
            {
                if (!initialSearchIndex.HasValue) {
                    return null;
                }

                var i = initialSearchIndex.Value;
                while (i < initialSections.Count)
                {
                    if (initialSectionData[i].EditEvent == EditEvent.Untouched)
                    {
                        return i;
                    }

                    i++;
                }

                return null;
            }

            // inserted and moved sections {
            // this should fix all sections and move them into correct places
            // 2nd stage
            for (var i = 0; i < finalSections.Count; i++)
            {
                untouchedOldIndex = findNextUntouchedOldIndex(untouchedOldIndex);

                // oh, it did exist
                var oldSectionIndex = finalSectionData[i].MoveIndex;
                if (oldSectionIndex.HasValue)
                {
                    var moveType = oldSectionIndex != untouchedOldIndex
                        ? EditEvent.Moved
                        : EditEvent.MovedAutomatically;

                    finalSectionData[i].EditEvent = moveType;
                    initialSectionData[oldSectionIndex.Value].EditEvent = moveType;
                }
                else
                {
                    finalSectionData[i].EditEvent = EditEvent.Inserted;
                }
            }

            // inserted sections
            for (var i = 0; i < finalSectionData.Count; i++)
            {
                var section = finalSectionData[i];
                if (!section.MoveIndex.HasValue)
                {
                    finalSectionData[i].EditEvent = EditEvent.Inserted;
                }
            }

            return (initialSectionData, finalSectionData);
        }

        private (List<List<ItemAssociatedData>>, List<List<ItemAssociatedData>>)
            calculateItemMovements(
                List<List<TElement>> initialItemCache,
                List<List<TElement>> finalItemCache,
                List<SectionAssociatedData> initialSectionData,
                List<SectionAssociatedData> finalSectionData)
        {
            var (initialItemData, finalItemData) = calculateAssociatedData(
                initialItemCache.Select(items => items.ToList()).ToList(),
                finalItemCache.Select(items => items.ToList()).ToList()
            );

            int? findNextUntouchedOldIndex(int initialSectionIndex, int? initialSearchIndex)
            {
                if (!initialSearchIndex.HasValue)
                {
                    return null;
                }

                var i2 = initialSearchIndex.Value;
                while (i2 < initialSectionData[initialSectionIndex].ItemCount)
                {
                    if (initialItemData[initialSectionIndex][i2].EditEvent == EditEvent.Untouched)
                    {
                        return i2;
                    }

                    i2++;
                }

                return null;
            }

            // first mark deleted items
            for (int i = 0; i < initialItemCache.Count; i++)
            {
                if (!initialSectionData[i].MoveIndex.HasValue)
                {
                    continue;
                }

                var indexAfterDelete = 0;
                for (int j = 0; j < initialItemCache[i].Count; j++)
                {
                    if (initialItemData[i][j].MoveIndex == null)
                    {
                        initialItemData[i][j].EditEvent = EditEvent.Deleted;
                        continue;
                    }

                    var finalIndexPath = initialItemData[i][j].MoveIndex;
                    // from this point below, section has to be move type because it's initial and not deleted

                    // because there is no move to inserted section
                    if (finalSectionData[finalIndexPath.sectionIndex].EditEvent == EditEvent.Inserted)
                    {
                        initialItemData[i][j].EditEvent = EditEvent.Deleted;
                        continue;
                    }

                    initialItemData[i][j].IndexAfterDelete = indexAfterDelete;
                    indexAfterDelete += 1;
                }
            }

            // mark moved or moved automatically
            for (int i = 0; i < finalItemCache.Count(); i++)
            {
                if (!finalSectionData[i].MoveIndex.HasValue)
                {
                    continue;
                }

                var originalSectionIndex = finalSectionData[i].MoveIndex.Value;

                int? untouchedIndex = 0;
                for (int j = 0; j < finalItemCache[i].Count; j++)
                {
                    untouchedIndex = findNextUntouchedOldIndex(originalSectionIndex, untouchedIndex);

                    if (finalItemData[i][j].MoveIndex == null)
                    {
                        finalItemData[i][j].EditEvent = EditEvent.Inserted;
                        continue;
                    }

                    var originalIndex = finalItemData[i][j].MoveIndex;

                    // In case trying to move from deleted section, abort, otherwise it will crash table view
                    if (initialSectionData[originalIndex.sectionIndex].EditEvent == EditEvent.Deleted)
                    {
                        finalItemData[i][j].EditEvent = EditEvent.Inserted;
                        continue;
                    }
                    // original section can't be inserted
                    else if (initialSectionData[originalIndex.sectionIndex].EditEvent == EditEvent.Inserted) {
                        //TODO try precondition(false, "New section in initial sections, that is wrong")
                        Console.WriteLine("Throw");
                    }

                    var initialSectionEvent = initialSectionData[originalIndex.sectionIndex].EditEvent;
                    if (initialSectionEvent != EditEvent.Moved && initialSectionEvent != EditEvent.MovedAutomatically)
                    {
                        // TODO THROW try precondition(initialSectionEvent == .moved || initialSectionEvent == .movedAutomatically, "Section not moved")
                        Console.WriteLine("Throw");
                    }

                    var eventType = (originalIndex.sectionIndex == originalSectionIndex && originalIndex.itemIndex == (untouchedIndex ?? -1))
                        ? EditEvent.MovedAutomatically
                        : EditEvent.Moved;

                    initialItemData[originalIndex.sectionIndex][originalIndex.itemIndex].EditEvent = eventType;
                    finalItemData[i][j].EditEvent = eventType;
                }
            }

            return (initialItemData, finalItemData);
        }

        private List<Changeset> generateDeleteSectionsDeletedItemsAndUpdatedItems()
        {
            var deletedSections = new List<int>();
            var deletedItems = new List<ItemPath>();
            var updatedItems = new List<ItemPath>();
            var afterDeleteState = new List<TSection>();

            // mark deleted items {
            // 1rst stage again (I know, I know ...)
            for (var i = 0; i < initialItemCache.Count; i++)
            {
                var initialItems = initialItemCache[i];
                var editEvent = initialSectionData[i].EditEvent;

                // Deleted section will take care of deleting child items.
                // In case of moving an item from deleted section, tableview will
                // crash anyway, so this is not limiting anything.
                if (editEvent == EditEvent.Deleted)
                {
                    deletedSections.Add(i);
                    continue;
                }

                var afterDeleteItems = new List<TElement>();
                for (int j = 0; j < initialItems.Count; j++)
                {
                    editEvent = initialItemData[i][j].EditEvent;
                    switch (editEvent)
                    {
                        case EditEvent.Deleted:
                            deletedItems.Add(new ItemPath(i, j));
                            break;
                        case EditEvent.Moved:
                        case EditEvent.MovedAutomatically:
                            var finalItemIndex = initialItemData[i][j].MoveIndex;
                            var finalItem = finalItemCache[finalItemIndex.sectionIndex][finalItemIndex.itemIndex];
                            if (!finalItem.Equals(initialSections[i].Items[j]))
                            {
                                updatedItems.Add(new ItemPath(sectionIndex: i, itemIndex: j));
                            }

                            afterDeleteItems.Add(finalItem);
                            break;
                        default:
                            // TODO throw try precondition(false, "Unhandled case")
                            Console.WriteLine("Throw");
                            break;
                    }
                }

                var newSection = new TSection();
                newSection.Initialize(initialSections[i].Header, afterDeleteItems);
                afterDeleteState.Add(newSection);
            }
            // }

            if (deletedItems.Count == 0 && deletedSections.Count == 0 && updatedItems.Count == 0)
            {
                return new List<Changeset>();
            }

            var changeSet = new Changeset(
                finalSections: afterDeleteState,
                deletedSections: deletedSections,
                deletedItems: deletedItems,
                updatedItems: updatedItems
            );

            return new List<Changeset>(new[] { changeSet });
        }

        private IEnumerable<Changeset> generateInsertAndMoveSections()
        {
            var movedSections = new List<(int, int)>();
            var insertedSections = new List<int>();

            for (int i = 0; i < initialSections.Count; i++)
            {

                switch (initialSectionData[i].EditEvent) {
                case EditEvent.Deleted:
                    break;
                case EditEvent.Moved:
                    movedSections.Add((initialSectionData[i].IndexAfterDelete.Value,
                        initialSectionData[i].MoveIndex.Value));
                    break;
                case EditEvent.MovedAutomatically:
                    break;
                default:
                    //TODO THROW try precondition(false, "Unhandled case in initial sections")
                    Console.WriteLine("Throw");
                    break;
                }
            }

            for (int i = 0; i < finalSections.Count; i++)
            {
                switch (finalSectionData[i].EditEvent) {
                case EditEvent.Inserted:
                    insertedSections.Add(i);
                    break;
                default:
                    break;
                }
            }

            if (insertedSections.Count == 0 && movedSections.Count == 0) {
                return new List<Changeset>();
            }

            // sections should be in place, but items should be original without deleted ones
            var sectionsAfterChange = Enumerable.Range(0, finalSections.Count).Select(i =>
            {
                var section = finalSections[i];
                var editEvent = finalSectionData[i].EditEvent;

                if (editEvent == EditEvent.Inserted) {
                    // it's already set up
                    return section;
                }
                else if (editEvent == EditEvent.Moved || editEvent == EditEvent.MovedAutomatically)
                {
                    var originalSectionIndex = finalSectionData[i].MoveIndex.Value;
                    var originalSection = initialSections[originalSectionIndex];

                    var items = new List<TElement>();
                    //items.reserveCapacity(originalSection.items.count)
                    var itemAssociatedData = initialItemData[originalSectionIndex];
                    for (int j = 0; j < originalSection.Items.Count; j++)
                    {
                        var initialData = itemAssociatedData[j];

                        if (initialData.EditEvent == EditEvent.Deleted)
                        {
                            continue;
                        }

                        if (initialData.MoveIndex == null)
                        {
                            // TODO throw try precondition(false, "Item was moved, but no final location.")
                            Console.WriteLine("Throw");
                            continue;
                        }

                        var finalIndex = initialData.MoveIndex;

                        items.Add(finalItemCache[finalIndex.sectionIndex][finalIndex.itemIndex]);
                    }

                    var newSection = new TSection();
                    newSection.Initialize(section.Header, items);
                    var modifiedSection = newSection;

                    return modifiedSection;
                }
                else {
                    // TODO Throw try precondition(false, "This is weird, this shouldn't happen")
                    Console.WriteLine("Throw");
                    return section;
                }
            });

            var changeSet = new Changeset(
                finalSections: sectionsAfterChange.ToList(),
                insertedSections: insertedSections,
                movedSections: movedSections);

            return new List<Changeset>(new[] { changeSet });
        }

        private IEnumerable<Changeset> generateInsertAndMovedItems()
        {
            var insertedItems = new List<ItemPath>();
            var movedItems = new List<(ItemPath, ItemPath)>();

            // mark new and moved items {
            // 3rd stage
            for (int i = 0; i < finalSections.Count; i++)
            {
                var finalSection = finalSections[i];

                var sectionEvent = finalSectionData[i].EditEvent;
                // new and deleted sections cause reload automatically
                if (sectionEvent != EditEvent.Moved && sectionEvent != EditEvent.MovedAutomatically)
                {
                    continue;
                }

                for (int j = 0; j < finalSection.Items.Count; j++)
                {
                    var currentItemEvent = finalItemData[i][j].EditEvent;

                    if (currentItemEvent == EditEvent.Untouched)
                    {
                        //TODO Throw try precondition(currentItemEvent != .untouched, "Current event is not untouched")
                        Console.WriteLine("Throw");
                    }

                    var editEvent = finalItemData[i][j].EditEvent;

                    switch (editEvent)
                    {
                    case EditEvent.Inserted:
                        insertedItems.Add(new ItemPath(i, j));
                        break;
                    case EditEvent.Moved:
                        var originalIndex = finalItemData[i][j].MoveIndex;
                        var finalSectionIndex = initialSectionData[originalIndex.sectionIndex].MoveIndex.Value;
                        var moveFromItemWithIndex = initialItemData[originalIndex.sectionIndex][originalIndex.itemIndex]
                            .IndexAfterDelete.Value;

                        var moveCommand = (
                            new ItemPath(finalSectionIndex, moveFromItemWithIndex),
                            new ItemPath(i, j)
                        );

                        movedItems.Add(moveCommand);
                        break;
                    default:
                        break;
                    }
                }
            }
            // }

            if (insertedItems.Count == 0 && movedItems.Count == 0) {
                return new List<Changeset>();
            }

            var changeset = new Changeset(
                finalSections: finalSections,
                insertedItems: insertedItems,
                movedItems: movedItems
            );

            return new List<Changeset>(new[] { changeset });
        }

        private static Dictionary<long, int> indexSections(List<TSection> sections)
        {
            Dictionary<long, int> indexedSections = new Dictionary<long, int>();

            for (int i = 0; i < sections.Count(); i++)
            {
                var section = sections[i];

                if (indexedSections.ContainsKey(section.Identity))
                {
                    // TODO EXCEPTION  print("Section \(section) has already been indexed at \(indexedSections[section.identity]!)")
                    Console.WriteLine("Throw");
                }

                indexedSections[section.Identity] = i;
            }

            return indexedSections;
        }

        private static (List<List<ItemAssociatedData>>, List<List<ItemAssociatedData>>)
            calculateAssociatedData(List<List<TElement>> initialItemCache, List<List<TElement>> finalItemCache)
        {
            var initialIdentities = new List<long>();
            var initialItemPaths = new List<ItemPath>();

            for (int i = 0; i < initialItemCache.Count; i++)
            {
                var items = initialItemCache[i];
                for (int j = 0; j < items.Count; j++)
                {
                    var item = items[j];

                    initialIdentities.Add(item.Identity);
                    initialItemPaths.Add(new ItemPath(i, j));
                }
            }

            var initialItemData = initialItemCache
                    .Select(items => Enumerable.Range(0, items.Count).Select(_ => ItemAssociatedData.Initial()).ToList())
                    .ToList();

            var finalItemData = finalItemCache
                    .Select(items => Enumerable.Range(0, items.Count).Select(_ => ItemAssociatedData.Initial()).ToList())
                    .ToList();

            var dictionary = new Dictionary<long, int>();

            for (int i = 0; i < initialIdentities.Count; i++)
            {
                var identity = initialIdentities[i];

                if (dictionary.ContainsKey(identity))
                {
                    var existingValueItemPathIndex = dictionary[identity];
                    var itemPath = initialItemPaths[existingValueItemPathIndex];
                    var item = initialItemCache[itemPath.sectionIndex][itemPath.itemIndex];
                    throw new DuplicateItemException(item.Identity);
                }

                dictionary[identity] = i;
            }

            for (int i = 0; i < finalItemCache.Count; i++)
            {
                var items = finalItemCache[i];

                for (int j = 0; j < items.Count; j++)
                {
                    var item = items[j];

                    var identity = item.Identity;
                    if (!dictionary.ContainsKey(identity))
                    {
                        continue;
                    }

                    var initialItemPathIndex = dictionary[identity];
                    var itemPath = initialItemPaths[initialItemPathIndex];
                    if (initialItemData[itemPath.sectionIndex][itemPath.itemIndex].MoveIndex != null)
                    {
                        throw new DuplicateItemException(item.Identity);
                    }

                    initialItemData[itemPath.sectionIndex][itemPath.itemIndex].MoveIndex = new ItemPath(i, j);
                    finalItemData[i][j].MoveIndex = itemPath;
                }
            }

            return (initialItemData, finalItemData);
        }
    }
}

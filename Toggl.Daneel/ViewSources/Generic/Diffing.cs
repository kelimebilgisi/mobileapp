using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Foundation.MvvmCross.Collections;

namespace Toggl.Daneel.ViewSources.Generic
{
    public class ItemPath
    {
        public int sectionIndex { get; }
        public int itemIndex { get; }

        public ItemPath(int sectionIndex, int itemIndex)
        {
            this.sectionIndex = sectionIndex;
            this.itemIndex = itemIndex;
        }
    }

    public class Changeset<TElement>
        where TElement : IDiffable, IEquatable<TElement>
    {
        public List<IAnimatableSectionModel<TElement>> OriginalSections { get; }
        public List<IAnimatableSectionModel<TElement>> FinalSections { get; }

        public List<int> InsertedSections { get; }
        public List<int> DeletedSections { get; }
        public List<(int, int)> MovedSections { get; }
        public List<int> UpdatedSections { get; }
        public List<ItemPath> InsertedItems { get; }
        public List<ItemPath> DeletedItems { get; }
        public List<(ItemPath, ItemPath)> MovedItems { get; }
        public List<ItemPath> UpdatedItems { get; }

        public Changeset(
            List<IAnimatableSectionModel<TElement>> originalSections = null,
            List<IAnimatableSectionModel<TElement>> finalSections = null,
            List<int> insertedSections = null,
            List<int> deletedSections = null,
            List<(int, int)> movedSections = null,
            List<int> updatedSections = null,
            List<ItemPath> insertedItems = null,
            List<ItemPath> deletedItems = null,
            List<(ItemPath, ItemPath)> movedItems = null,
            List<ItemPath> updatedItems = null)
        {
            OriginalSections = originalSections ?? new List<IAnimatableSectionModel<TElement>>();
            FinalSections = finalSections ?? new List<IAnimatableSectionModel<TElement>>();

            InsertedSections = insertedSections ?? new List<int>();
            DeletedSections = deletedSections ?? new List<int>();
            MovedSections = movedSections ?? new List<(int, int)>();
            UpdatedSections = updatedSections ?? new List<int>();

            InsertedItems = insertedItems ?? new List<ItemPath>();
            DeletedItems = deletedItems ?? new List<ItemPath>();
            MovedItems = movedItems ?? new List<(ItemPath, ItemPath)>();
            UpdatedItems = updatedItems ?? new List<ItemPath>();
        }

        public static Changeset<TElement> initialValue(List<IAnimatableSectionModel<TElement>> sections) {
            return new Changeset<TElement>(
                finalSections: sections,
                insertedSections: Enumerable.Range(0, sections.Count).ToList()
            );
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

        public SectionAssociatedData(EditEvent editEvent, int indexAfterDelete, int moveIndex, int itemCount)
        {
            EditEvent = editEvent;
            IndexAfterDelete = indexAfterDelete;
            MoveIndex = moveIndex;
            ItemCount = itemCount;
        }

        public static SectionAssociatedData Initial()
        {
            return new SectionAssociatedData(EditEvent.Untouched, -1, -1, 0);
        }
    }

    class ItemAssociatedData
    {
        public EditEvent EditEvent { get; set; }
        public int? IndexAfterDelete { get; set; }
        public ItemPath MoveIndex { get; set; }

        public ItemAssociatedData(EditEvent editEvent, int indexAfterDelete, ItemPath moveIndex)
        {
            EditEvent = editEvent;
            IndexAfterDelete = indexAfterDelete;
            MoveIndex = moveIndex;
        }

        public static ItemAssociatedData Initial()
        {
            return new ItemAssociatedData(EditEvent.Untouched, -1, null);
        }
    }

    public class Diffing<TElement>
        where TElement : IDiffable, IEquatable<TElement>
    {
        public delegate IAnimatableSectionModel<TElement> SectionConstruction(IAnimatableSectionModel<TElement> original, IList<IDiffable> items);

        private List<IAnimatableSectionModel<TElement>> initialSections;
        private List<IAnimatableSectionModel<TElement>> finalSections;

        private List<List<TElement>> initialItemCache;
        private List<List<TElement>> finalItemCache;

        private List<SectionAssociatedData> initialSectionData;
        private List<SectionAssociatedData> finalSectionData;

        private List<List<ItemAssociatedData>> initialItemData;
        private List<List<ItemAssociatedData>> finalItemData;

        private SectionConstruction sectionConstruction;

        public Diffing(IEnumerable<IAnimatableSectionModel<TElement>> initialSections, IEnumerable<IAnimatableSectionModel<TElement>> finalSections, SectionConstruction sectionConstruction)
        {
            this.initialSections = initialSections.ToList();
            this.finalSections = finalSections.ToList();
            this.sectionConstruction = sectionConstruction;

            (initialSectionData, finalSectionData) = calculateSectionMovements(initialSections.ToList(), finalSections.ToList());

            initialItemCache = initialSections.Select(collection => collection.Items.ToList()).ToList();
            finalItemCache = finalSections.Select(collection => collection.Items.ToList()).ToList();
        }

        public List<Changeset<TElement>> computeDifferences()
        {
            var result = Enumerable.Empty<Changeset<TElement>>().ToList();

            (initialItemData, finalItemData) = calculateItemMovements(
                initialItemCache,
                finalItemCache,
                initialSectionData,
                finalSectionData
            );

            result.Concat(generateDeleteSectionsDeletedItemsAndUpdatedItems());
            result.Concat(generateInsertAndMoveSections());
            result.Concat(generateInsertAndMovedItems());

            return result;
        }

        private static (List<SectionAssociatedData>, List<SectionAssociatedData>) calculateSectionMovements(
            IList<IAnimatableSectionModel<TElement>> initialSections, IList<IAnimatableSectionModel<TElement>> finalSections)
        {
            var initialSectionIndexes = indexSections(initialSections);

            var initialSectionData = Enumerable.Repeat(SectionAssociatedData.Initial(), initialSections.Count()).ToList();
            var finalSectionData = Enumerable.Repeat(SectionAssociatedData.Initial(), finalSections.Count()).ToList();

            var i = 0;
            foreach (var section in finalSections)
            {
                finalSectionData[i].ItemCount = finalSections[i].Items.Count;

                if (!initialSectionIndexes.ContainsKey(section.Identity))
                {
                    continue;
                }

                var initialSectionIndex = initialSectionIndexes[section.Identity];
                if (!initialSectionData[initialSectionIndex].MoveIndex.HasValue)
                {
                    // TODO THROW Error.duplicateSection(section: section)
                }

                initialSectionData[initialSectionIndex].MoveIndex = i;
                finalSectionData[i].MoveIndex = initialSectionIndex;

                i++;
            }

            var sectionIndexAfterDelete = 0;

            // deleted sections

            for (i = 0; i < initialSectionData.Count(); i++)
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
                if (initialSearchIndex == null) {
                    return null;
                }

                i = 0;
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
            for (i = 0; i < finalSections.Count; i++)
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
            i = 0;
            foreach (var section in finalSectionData)
            {
                if (!section.MoveIndex.HasValue)
                {
                    finalSectionData[i].EditEvent = EditEvent.Inserted;
                }
                i++;
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
                while (i2 < initialSectionData[initialSearchIndex.Value].ItemCount)
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
            for (int i = 0; i < initialItemCache.Count(); i++)
            {
                if (!initialSectionData[i].MoveIndex.HasValue)
                {
                    continue;
                }

                var indexAfterDelete = 0;
                for (int j = 0; j < initialItemCache.Count(); j++)
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
                    }

                    var initialSectionEvent = initialSectionData[originalIndex.sectionIndex].EditEvent;
                    if (initialSectionEvent == EditEvent.Moved || initialSectionEvent == EditEvent.MovedAutomatically)
                    {
                        // TODO THROW try precondition(initialSectionEvent == .moved || initialSectionEvent == .movedAutomatically, "Section not moved")
                    }

                    var eventType = originalIndex == new ItemPath(originalSectionIndex, untouchedIndex ?? -1)
                        ? EditEvent.MovedAutomatically
                        : EditEvent.Moved;

                    initialItemData[originalIndex.sectionIndex][originalIndex.itemIndex].EditEvent = eventType;
                    finalItemData[i][j].EditEvent = eventType;
                }
            }

            return (initialItemData, finalItemData);
        }

        private List<Changeset<TElement>> generateDeleteSectionsDeletedItemsAndUpdatedItems()
        {
            var deletedSections = new List<int>();
            var deletedItems = new List<ItemPath>();
            var updatedItems = new List<ItemPath>();
            var afterDeleteState = new List<IAnimatableSectionModel<TElement>>();

            // mark deleted items {
            // 1rst stage again (I know, I know ...)
            var i = 0;
            foreach (var initialItems in initialItemCache)
            {
                var editEvent = initialSectionData[i].EditEvent;

                // Deleted section will take care of deleting child items.
                // In case of moving an item from deleted section, tableview will
                // crash anyway, so this is not limiting anything.
                if (editEvent == EditEvent.Deleted)
                {
                    deletedSections.Append(i);
                    continue;
                }

                var afterDeleteItems = new List<IDiffable>();
                for (int j = 0; j < initialItems.Count; j++)
                {
                    editEvent = initialItemData[i][j].EditEvent;
                    switch (editEvent)
                    {
                        case EditEvent.Deleted:
                            deletedItems.Append(new ItemPath(i, j));
                            break;
                        case EditEvent.Moved:
                        case EditEvent.MovedAutomatically:
                            var finalItemIndex = initialItemData[i][j].MoveIndex;
                            var finalItem = finalItemCache[finalItemIndex.sectionIndex][finalItemIndex.itemIndex];
                            if (!finalItem.Equals(initialSections[i].Items[j]))
                            {
                                updatedItems.Append(new ItemPath(sectionIndex: i, itemIndex: j));
                            }

                            afterDeleteItems.Append(finalItem);
                            break;
                        default:
                            // TODO throw try precondition(false, "Unhandled case")
                            break;
                    }
                }

                afterDeleteItems.Append(sectionConstruction(initialSections[i], afterDeleteItems));

                i++;
            }
            // }

            if (deletedItems.Count == 0 && deletedSections.Count == 0 && updatedItems.Count == 0)
            {
                return new List<Changeset<TElement>>();
            }

            var changeSet = new Changeset<TElement>(
                finalSections: afterDeleteState,
                deletedSections: deletedSections,
                deletedItems: deletedItems,
                updatedItems: updatedItems
            );

            return new List<Changeset<TElement>>(new[]{ changeSet });
        }

        private IEnumerable<Changeset<TElement>> generateInsertAndMoveSections()
        {
            var movedSections = new List<(int, int)>();
            var insertedSections = new List<int>();

            for (int i = 0; i < initialSections.Count; i++)
            {

                switch (initialSectionData[i].EditEvent) {
                case EditEvent.Deleted:
                    break;
                case EditEvent.Moved:
                    movedSections.Append((initialSectionData[i].IndexAfterDelete.Value,
                        initialSectionData[i].MoveIndex.Value));
                    break;
                case EditEvent.MovedAutomatically:
                    break;
                default:
                    //TODO THROW try precondition(false, "Unhandled case in initial sections")
                    break;
                }
            }

            for (int i = 0; i < finalSections.Count; i++)
            {
                switch (finalSectionData[i].EditEvent) {
                case EditEvent.Inserted:
                    insertedSections.Append(i);
                    break;
                default:
                    break;
                }
            }

            if (insertedSections.Count == 0 && movedSections.Count == 0) {
                return new List<Changeset<TElement>>();
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

                    var items = new List<IDiffable>();
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
                            continue;
                        }

                        var finalIndex = initialData.MoveIndex;

                        items.Append(finalItemCache[finalIndex.sectionIndex][finalIndex.itemIndex]);
                    }

                    var modifiedSection = sectionConstruction(section, items);

                    return modifiedSection;
                }
                else {
                    // TODO Throw try precondition(false, "This is weird, this shouldn't happen")
                    return section;
                }
            });


            var changeSet = new Changeset<TElement>(
                finalSections: sectionsAfterChange.ToList(),
                insertedSections:  insertedSections,
                movedSections: movedSections);

            return new List<Changeset<TElement>>(new[]{ changeSet });
        }

        private IEnumerable<Changeset<TElement>> generateInsertAndMovedItems()
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

                    if (currentItemEvent != EditEvent.Untouched)
                    {
                        //TODO Throw try precondition(currentItemEvent != .untouched, "Current event is not untouched")
                    }

                    var editEvent = finalItemData[i][j].EditEvent;

                    switch (editEvent)
                    {
                    case EditEvent.Inserted:
                        insertedItems.Append(new ItemPath(i, j));
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

                        movedItems.Append(moveCommand);
                        break;
                    default:
                        break;
                    }
                }
            }
            // }

            if (insertedItems.Count == 0 && movedItems.Count == 0) {
                return new List<Changeset<TElement>>();
            }

            var changeset = new Changeset<TElement>(
                finalSections: finalSections,
                insertedItems: insertedItems,
                movedItems: movedItems
            );

            return new List<Changeset<TElement>>(new []{ changeset });
        }

        private static Dictionary<long, int> indexSections(IEnumerable<IAnimatableSectionModel<TElement>> sections)
        {
            Dictionary<long, int> indexedSections = new Dictionary<long, int>();

            var i = 0;
            foreach (var section in sections)
            {
                if (indexedSections[section.Identity] == null)
                {
                    // TODO EXCEPTION
                }

                indexedSections[section.Identity] = i;
                i++;
            }

            return indexedSections;
        }

        private static (List<List<ItemAssociatedData>>, List<List<ItemAssociatedData>>)
            calculateAssociatedData(List<List<TElement>> initialItemCache, List<List<TElement>> finalItemCache)
        {
            var initialIdentities = new List<long>();
            var initialItemPaths = new List<ItemPath>();

            var i = 0;
            foreach (var items in initialItemCache)
            {
                var j = 0;
                foreach (var item in items)
                {
                    initialIdentities.Append(item.Identity);
                    initialItemPaths.Append(new ItemPath(i, j));
                    j++;
                }

                i++;
            }

            var initialItemData = initialItemCache
                    .Select(items => Enumerable.Repeat(ItemAssociatedData.Initial(), items.Count()).ToList())
                    .ToList();

            var finalItemData = finalItemCache
                    .Select(items => Enumerable.Repeat(ItemAssociatedData.Initial(), items.Count()).ToList())
                    .ToList();

            var dictionary = new Dictionary<long, int>();

            i = 0;
            foreach (var identity in initialIdentities)
            {

                if (dictionary.ContainsKey(identity))
                {
                    var existingValueItemPathIndex = dictionary[identity];
                    var itemPath = initialItemPaths[existingValueItemPathIndex];
                    var item = initialItemCache[itemPath.sectionIndex][itemPath.itemIndex];
                    // TODO throw Error.duplicateItem(item: item)
                }

                dictionary[identity] = i;

                i++;
            }

            i = 0;
            foreach (var items in finalItemCache)
            {
                var j = 0;
                foreach (var item in items)
                {
                    var identity = item.Identity;
                    if (!dictionary.ContainsKey(identity))
                    {
                        continue;
                    }

                    var initialItemPathIndex = dictionary[identity];
                    var itemPath = initialItemPaths[initialItemPathIndex];
                    if (initialItemData[itemPath.sectionIndex][itemPath.itemIndex].MoveIndex != null)
                    {
                        // TODO throw Error.duplicateItem(item: item)
                    }

                    initialItemData[itemPath.sectionIndex][itemPath.itemIndex].MoveIndex = new ItemPath(i, j);
                    finalItemData[i][j].MoveIndex = itemPath;

                    j++;
                }

                i++;
            }

            return (initialItemData, finalItemData);
        }
    }
}

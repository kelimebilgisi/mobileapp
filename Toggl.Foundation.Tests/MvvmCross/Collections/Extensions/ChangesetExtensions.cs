using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using Toggl.Daneel.ViewSources.Generic;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.Tests.MvvmCross.ViewModels;

namespace Toggl.Daneel.Tests.Unit.Extensions
{
    public static class ChangesetExtensions
    {
        public class ItemModelTypeWrapper<TItem> {
            public TItem Item { get; set; }
            public bool Deleted { get; set; } = false;
            public bool Updated { get; set; } = false;
            public ItemPath Moved { get; set; } = null;

            public ItemModelTypeWrapper(TItem item)
            {
                Item = item;
            }
        }

        public class SectionModelTypeWrapper<TSection, THeader, TElement>
        where TSection : ISectionModel<THeader, TElement>, new()
        {
            public bool Updated { get; set; } = false;
            public bool Deleted { get; set; } = false;
            public int? Moved { get; set; } = null;

            public List<ItemModelTypeWrapper<TElement>> Items { get; set; }
            public TSection Section { get; set; }

            public SectionModelTypeWrapper(TSection section)
            {
                Section = section;
                Items = section.Items.Select(item => new ItemModelTypeWrapper<TElement>(item)).ToList();
            }

            public static List<SectionModelTypeWrapper<TSection, THeader, TElement>> Wrap(List<TSection> sections)
            {
                return sections.Select(section => new SectionModelTypeWrapper<TSection, THeader, TElement>(section)).ToList();
            }

            public static List<TSection> Unwrap(List<SectionModelTypeWrapper<TSection, THeader, TElement>> sections)
            {
                return sections.Select(sectionWrapper =>
                {
                    var items = sectionWrapper.Items.Select(wrapper => wrapper.Item);
                    var newSection = new TSection();
                    newSection.Initialize(sectionWrapper.Section.Header, items);
                    return newSection;
                }).ToList();
            }
        }


        public static bool OnlyContains<TSection, THeader, TItem>(this Diffing<TSection, THeader, TItem>.Changeset changeset,
            int insertedSections = 0,
            int deletedSections = 0,
            int movedSections = 0,
            int updatedSections = 0,
            int insertedItems = 0,
            int deletedItems = 0,
            int movedItems = 0,
            int updatedItems = 0
        )
            where TSection : IAnimatableSectionModel<THeader, TItem>, new()
            where THeader : IDiffable
            where TItem : IDiffable, IEquatable<TItem>
        {
            if (changeset.InsertedSections.Count != insertedSections)
            {
                return false;
            }

            if (changeset.DeletedSections.Count != deletedSections)
            {
                return false;
            }

            if (changeset.MovedSections.Count != movedSections)
            {
                return false;
            }

            if (changeset.UpdatedSections.Count != updatedSections)
            {
                return false;
            }

            if (changeset.InsertedItems.Count != insertedItems)
            {
                return false;
            }

            if (changeset.DeletedItems.Count != deletedItems)
            {
                return false;
            }

            if (changeset.MovedItems.Count != movedItems)
            {
                return false;
            }

            if (changeset.UpdatedItems.Count != updatedItems)
            {
                return false;
            }

            return true;
        }

        public static List<TSection> Apply<TSection, THeader, TElement>(
            this Diffing<TSection, THeader, TElement>.Changeset changeset,
            List<TSection> original
        )
            where TSection : IAnimatableSectionModel<THeader, TElement>, new()
            where THeader : IDiffable
            where TElement : IDiffable, IEquatable<TElement>
        {
            var afterDeletesAndUpdates = changeset.applyDeletesAndUpdates(original);
            var afterSectionMovesAndInserts = changeset.applySectionMovesAndInserts(afterDeletesAndUpdates);
            var afterItemInsertsAndMoves = changeset.applyItemInsertsAndMoves(afterSectionMovesAndInserts);

            return afterItemInsertsAndMoves;
        }

        private static List<TSection> applyDeletesAndUpdates<TSection, THeader, TElement>(
            this Diffing<TSection, THeader, TElement>.Changeset changeset,
            List<TSection> original
        )
            where TSection : IAnimatableSectionModel<THeader, TElement>, new()
            where THeader : IDiffable
            where TElement : IDiffable, IEquatable<TElement>
        {
            var resultAfterDeletesAndUpdates =
                SectionModelTypeWrapper<TSection, THeader, TElement>.Wrap(original);

            foreach (var index in changeset.UpdatedItems)
            {
                resultAfterDeletesAndUpdates[index.sectionIndex].Items[index.itemIndex].Updated = true;
            }

            foreach (var index in changeset.DeletedItems)
            {
                resultAfterDeletesAndUpdates[index.sectionIndex].Items[index.itemIndex].Deleted = true;
            }

            foreach (var section in changeset.DeletedSections)
            {
                resultAfterDeletesAndUpdates[section].Deleted = true;
            }

            resultAfterDeletesAndUpdates = resultAfterDeletesAndUpdates.Where(section => !section.Deleted).ToList();

            for (int sectionIndex = 0; sectionIndex < resultAfterDeletesAndUpdates.Count; sectionIndex++)
            {
                var section = resultAfterDeletesAndUpdates[sectionIndex];

                section.Items = section.Items.Where(item => !item.Deleted).ToList();

                for (int itemIndex = 0; itemIndex < section.Items.Count; itemIndex++)
                {
                    var item = section.Items[itemIndex];

                    if (item.Updated)
                    {
                        section.Items[itemIndex] = new ItemModelTypeWrapper<TElement>(changeset.FinalSections[sectionIndex].Items[itemIndex]);
                    }
                }
            }

            return SectionModelTypeWrapper<TSection, THeader, TElement>.Unwrap(resultAfterDeletesAndUpdates);
        }

        private static List<TSection> applySectionMovesAndInserts<TSection, THeader, TElement>(
            this Diffing<TSection, THeader, TElement>.Changeset changeset,
            List<TSection> original
        )
            where TSection : IAnimatableSectionModel<THeader, TElement>, new()
            where THeader : IDiffable
            where TElement : IDiffable, IEquatable<TElement>
        {
            if (changeset.UpdatedSections.Count != 0) {
                // TODO Throw fatalError("Section updates aren't supported")
                return new List<TSection>();
            }

            var sourceSectionIndexes = new HashSet<int>(changeset.MovedSections.Select(movement => movement.Item1));
            var destinationToSourceMapping = new Dictionary<int, int>();
            foreach (var movement in changeset.MovedSections)
            {
                destinationToSourceMapping[movement.Item2] = movement.Item1;
            }

            var insertedSectionsIndexes = new HashSet<int>(changeset.InsertedSections);

            var nextUntouchedSourceSectionIndex = -1;
            bool findNextUntouchedSourceSection()
            {
                nextUntouchedSourceSectionIndex += 1;
                while (nextUntouchedSourceSectionIndex < original.Count && sourceSectionIndexes.Contains(nextUntouchedSourceSectionIndex))
                {
                    nextUntouchedSourceSectionIndex += 1;
                }

                return nextUntouchedSourceSectionIndex < original.Count;
            };

            var totalCount = original.Count + changeset.InsertedSections.Count;

            var results = new List<TSection>();

            for (int index = 0; index < totalCount; index++)
            {
                if (insertedSectionsIndexes.Contains(index)) {
                    results.Add(changeset.FinalSections[index]);
                }
                else
                {
                    if (destinationToSourceMapping.ContainsKey(index))
                    {
                        var sourceIndex = destinationToSourceMapping[index];
                        results.Add(original[sourceIndex]);
                    }
                    else
                    {
                        if (!findNextUntouchedSourceSection())
                        {
                            // TODO THrow fatalError("Oooops, wrong commands.")
                            return new List<TSection>();
                        }

                        results.Add(original[nextUntouchedSourceSectionIndex]);
                    }
                }
            }

            return results;
        }

        private static List<TSection> applyItemInsertsAndMoves<TSection, THeader, TElement>(
            this Diffing<TSection, THeader, TElement>.Changeset changeset,
            List<TSection> original
        )
            where TSection : IAnimatableSectionModel<THeader, TElement>, new()
            where THeader : IDiffable
            where TElement : IDiffable, IEquatable<TElement>
        {
            var resultAfterInsertsAndMoves = original;

            var sourceIndexesThatShouldBeMoved = new HashSet<ItemPath>(changeset.MovedItems.Select(item => item.Item1).ToList());
            var destinationToSourceMapping = new Dictionary<ItemPath, ItemPath>();
            foreach (var movedItem in changeset.MovedItems)
            {
                destinationToSourceMapping[movedItem.Item2] = movedItem.Item1;
            }

            var insertedItemPaths = new HashSet<ItemPath>(changeset.InsertedItems);

            var insertedPerSection = Enumerable.Repeat(0, original.Count).ToList();
            var movedInSection = Enumerable.Repeat(0, original.Count).ToList();
            var movedOutSection = Enumerable.Repeat(0, original.Count).ToList();

            foreach (var insertedItemPath in changeset.InsertedItems)
            {
                insertedPerSection[insertedItemPath.sectionIndex] += 1;
            }

            foreach (var moveItem in changeset.MovedItems)
            {
                movedInSection[moveItem.Item2.sectionIndex] += 1;
                movedOutSection[moveItem.Item1.sectionIndex] += 1;
            }


            for (int sectionIndex = 0; sectionIndex < resultAfterInsertsAndMoves.Count; sectionIndex++)
            {
                var section = resultAfterInsertsAndMoves[sectionIndex];

                var originalItems = section.Items;

                var nextUntouchedSourceItemIndex = -1;

                bool findNextUntouchedSourceItem()
                {
                    nextUntouchedSourceItemIndex += 1;
                    while (nextUntouchedSourceItemIndex < section.Items.Count
                        && sourceIndexesThatShouldBeMoved.Contains(new ItemPath(sectionIndex: sectionIndex, itemIndex: nextUntouchedSourceItemIndex)))
                    {
                        nextUntouchedSourceItemIndex += 1;
                    }

                    return nextUntouchedSourceItemIndex < section.Items.Count;
                }

                var totalCount = section.Items.Count
                                 + insertedPerSection[sectionIndex]
                                 + movedInSection[sectionIndex]
                                 - movedOutSection[sectionIndex];

                var resultItems = new List<TElement>();

                for (int index = 0; index < totalCount; index++)
                {
                    var itemPath = new ItemPath(sectionIndex, index);
                    if (insertedItemPaths.Contains(itemPath))
                    {
                        resultItems.Add(changeset.FinalSections[itemPath.sectionIndex].Items[itemPath.itemIndex]);
                    }
                    else
                    {
                        if (destinationToSourceMapping.ContainsKey(itemPath))
                        {
                            var sourceIndex = destinationToSourceMapping[itemPath];
                            resultItems.Add(original[sourceIndex.sectionIndex].Items[sourceIndex.itemIndex]);
                        }
                        else
                        {
                            if (!findNextUntouchedSourceItem())
                            {
                                //TODO THROW fatalError("Oooops, wrong commands.")
                                return new List<TSection>();
                            }

                            resultItems.Add(originalItems[nextUntouchedSourceItemIndex]);
                        }
                    }
                }

                var newSection = new TSection();
                newSection.Initialize(section.Header, resultItems);
                resultAfterInsertsAndMoves[sectionIndex] = newSection;
            }

            return resultAfterInsertsAndMoves;
        }
    }
}

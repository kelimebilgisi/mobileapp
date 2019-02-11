using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Foundation;
using Toggl.Daneel.Cells;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Collections.Changes;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public class ReactiveTableViewBinder<TModel, TCell> : IDisposable
        where TCell : BaseTableViewCell<TModel>
    {
        private readonly object animationLock = new object();
        private CompositeDisposable disposeBag = new CompositeDisposable();

        private ReactiveSectionedListTableViewSource<TModel, TCell> dataSource;
        private UITableView tableView;

        public ReactiveTableViewBinder(UITableView tableView, ReactiveSectionedListTableViewSource<TModel, TCell> dataSource)
        {
            this.tableView = tableView;
            this.dataSource = dataSource;

            tableView.Source = dataSource;

            dataSource.CollectionChange
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(handleCollectionChange)
                .DisposedBy(disposeBag);
        }

        private void handleCollectionChange(ICollectionChange change)
        {
            lock (animationLock)
            {
                if (change is ReloadCollectionChange)
                {
                    dataSource.ChangeDisplayedCollection(change);
                    updateTable(change);
                    return;
                }

                tableView.BeginUpdates();

                dataSource.ChangeDisplayedCollection(change);
                var sectionsNeedingHeaderRefresh = updateTable(change);

                tableView.EndUpdates();

                sectionsNeedingHeaderRefresh.ForEach(index =>
                    dataSource.RefreshHeader(tableView, index));
            }
        }

        public void Dispose()
        {
            disposeBag?.Dispose();
            dataSource?.Dispose();
            tableView?.Dispose();
        }

        private IEnumerable<int> updateTable(ICollectionChange change)
        {
            var affectedSections = new List<int>();

            switch (change)
            {
                case AddRowCollectionChange<TModel> addRow:
                    add(addRow);
                    affectedSections.Add(addRow.Index.Section);
                    break;

                case InsertSectionCollectionChange<TModel> insert:
                    insertSection(insert);
                    break;

                case RemoveRowCollectionChange removeRow:
                    var oldSectionWasRemoved = remove(removeRow);
                    if (!oldSectionWasRemoved)
                        affectedSections.Add(removeRow.Index.Section);

                    break;

                case MoveRowWithinExistingSectionsCollectionChange<TModel> moveRow:
                    var oldAndNewSection = move(moveRow);
                    affectedSections.AddRange(oldAndNewSection);
                    break;

                case MoveRowToNewSectionCollectionChange<TModel> moveRowToNewSection:
                    var affectedSection = move(moveRowToNewSection);
                    if (affectedSection.HasValue)
                        affectedSections.Add(affectedSection.Value);

                    break;

                case UpdateRowCollectionChange<TModel> updateRow:
                    update(updateRow);
                    affectedSections.Add(updateRow.Index.Section);
                    break;

                case ReloadCollectionChange reload:
                    tableView.ReloadData();
                    break;
            }

            return affectedSections;
        }

        private void insertSection(InsertSectionCollectionChange<TModel> change)
        {
            tableView.InsertSections(indexSet(change.Index), UITableViewRowAnimation.Automatic);
            tableView.InsertRows(new SectionedIndex(change.Index, 0).ToIndexPaths(), UITableViewRowAnimation.Automatic);
        }

        private void add(AddRowCollectionChange<TModel> change)
        {
            tableView.InsertRows(change.Index.ToIndexPaths(), UITableViewRowAnimation.Automatic);
        }

        private bool remove(RemoveRowCollectionChange change)
        {
            return remove(change.Index);
        }

        private bool remove(SectionedIndex index)
        {
            var rowIsTheLastOneInTheSection = tableView.NumberOfRowsInSection(index.Section) == 1;
            if (rowIsTheLastOneInTheSection)
            {
                tableView.DeleteSections(indexSet(index.Section), UITableViewRowAnimation.Automatic);
                return true;
            }

            tableView.DeleteRows(index.ToIndexPaths(), UITableViewRowAnimation.Automatic);
            return false;
        }

        private int? move(MoveRowToNewSectionCollectionChange<TModel> change)
        {
            var oldSectionWasRemoved = remove(change.OldIndex);
            tableView.InsertSections(indexSet(change.Index), UITableViewRowAnimation.Automatic);
            tableView.InsertRows(new SectionedIndex(change.Index, 0).ToIndexPaths(), UITableViewRowAnimation.Automatic);

            if (oldSectionWasRemoved)
                return null;

            return change.Index <= change.OldIndex.Section
                ? change.OldIndex.Section + 1
                : change.OldIndex.Section;
        }

        private int[] move(MoveRowWithinExistingSectionsCollectionChange<TModel> change)
        {
            var oldSectionWasRemoved = remove(change.OldIndex);
            tableView.InsertRows(change.Index.ToIndexPaths(), UITableViewRowAnimation.Automatic);

            return oldSectionWasRemoved
                ? new[] { change.Index.Section }
                : new[] { change.OldIndex.Section, change.Index.Section };
        }

        private void update(UpdateRowCollectionChange<TModel> change)
        {
            tableView.ReloadRows(change.Index.ToIndexPaths(), UITableViewRowAnimation.Automatic);
        }

        private NSMutableIndexSet indexSet(int section)
            => (NSMutableIndexSet)NSIndexSet.FromIndex(section).MutableCopy();
    }
}

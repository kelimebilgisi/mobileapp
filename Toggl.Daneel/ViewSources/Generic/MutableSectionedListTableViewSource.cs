using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Foundation;
using Toggl.Daneel.Cells;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Collections.Changes;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public class MutableSectionedListTableViewSource<TModel, TCell> : UITableViewSource
        where TCell : BaseTableViewCell<TModel>
    {
        private readonly ISubject<TModel> itemSelectedSubject = new Subject<TModel>();
        private readonly string cellIdentifier;

        private readonly IReadOnlyList<IReadOnlyList<TModel>> items;

        public IObservable<TModel> ItemSelected { get; }

        protected IReadOnlyList<IReadOnlyList<TModel>> DisplayedItems
            => displayedItems.Select(section => section.AsReadOnly()).ToList().AsReadOnly();

        private List<List<TModel>> displayedItems;

        public MutableSectionedListTableViewSource(IReadOnlyList<IReadOnlyList<TModel>> items, string cellIdentifier)
        {
            this.items = items;
            this.cellIdentifier = cellIdentifier;

            ItemSelected = itemSelectedSubject.AsObservable();

            reloadDisplayedData();
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = (TCell)tableView.DequeueReusableCell(cellIdentifier, indexPath);
            cell.Item = displayedItems[indexPath.Section][indexPath.Row];
            return cell;
        }

        public override nint NumberOfSections(UITableView tableView)
            => displayedItems.Count;

        public override nint RowsInSection(UITableView tableview, nint section)
            => displayedItems[(int)section].Count;

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            var item = DisplayedItems[indexPath.Section][indexPath.Row];
            itemSelectedSubject.OnNext(item);
        }

        public void ChangeDisplayedCollection(ICollectionChange change)
        {
            switch (change)
            {
                case InsertSectionCollectionChange<TModel> insert:
                    insertSection(insert.Index, insert.Item);
                    break;

                case AddRowCollectionChange<TModel> addRow:
                    add(addRow.Index, addRow.Item);
                    break;

                case RemoveRowCollectionChange removeRow:
                    remove(removeRow.Index);
                    break;

                case MoveRowToNewSectionCollectionChange<TModel> moveRowToNewSection:
                    remove(moveRowToNewSection.OldIndex);
                    insertSection(moveRowToNewSection.Index, moveRowToNewSection.Item);
                    break;

                case MoveRowWithinExistingSectionsCollectionChange<TModel> moveRow:
                    remove(moveRow.OldIndex);
                    add(moveRow.Index, moveRow.Item);
                    break;

                case UpdateRowCollectionChange<TModel> updateRow:
                    update(updateRow.Index, updateRow.Item);
                    break;

                case ReloadCollectionChange _:
                    reloadDisplayedData();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void insertSection(int index, TModel item)
        {
            displayedItems.Insert(index, new List<TModel> { item });
        }

        private void add(SectionedIndex index, TModel item)
        {
            displayedItems[index.Section].Insert(index.Row, item);
        }

        private void remove(SectionedIndex index)
        {
            if (displayedItems[index.Section].Count == 1)
            {
                displayedItems.RemoveAt(index.Section);
            }
            else
            {
                displayedItems[index.Section].RemoveAt(index.Row);
            }
        }

        private void update(SectionedIndex index, TModel item)
        {
            displayedItems[index.Section][index.Row] = item;
        }

        private void reloadDisplayedData()
        {
            displayedItems = new List<List<TModel>>(items.Select(list => new List<TModel>(list)));
        }
    }
}

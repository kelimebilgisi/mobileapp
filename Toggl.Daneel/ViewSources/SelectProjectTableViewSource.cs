using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Foundation;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Views.EntityCreation;
using Toggl.Daneel.Views.StartTimeEntry;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.Collections;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class SelectProjectTableViewSource : ReactiveSectionedListTableViewSource<AutocompleteSuggestion, ReactiveProjectSuggestionViewCell>
    {
        private const int headerHeight = 40;

        private ISubject<ProjectSuggestion> toggleSuggestionsSubject = new Subject<ProjectSuggestion>();
        public IObservable<ProjectSuggestion> ToggleTaskSuggestion => toggleSuggestionsSubject.AsObservable();

        private bool suggestCreation => createEntitySuggestion != null;

        private CreateEntitySuggestion createEntitySuggestion { get; set; }

        public bool UseGrouping { get; set; }

        public SelectProjectTableViewSource(
            ObservableGroupedOrderedCollection<AutocompleteSuggestion> items,
            string cellIdentifier)
            : base(items, cellIdentifier)
        {
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var item = getItemAt(indexPath);
            var cell = tableView.DequeueReusableCell(getCellIdentifierFor(item));

            cell.LayoutMargins = UIEdgeInsets.Zero;
            cell.SeparatorInset = UIEdgeInsets.Zero;
            cell.PreservesSuperviewLayoutMargins = false;

            switch(cell)
            {
                case CreateEntityViewCell createEntityViewcell:
                    createEntityViewcell.Item = (CreateEntitySuggestion)item;
                    break;

                case ReactiveTaskSuggestionViewCell taskCell:
                    taskCell.Item = (TaskSuggestion)item;
                    break;

                case ReactiveProjectSuggestionViewCell projectCell:
                    projectCell.Item = (ProjectSuggestion)item;
                    projectCell.ToggleTaskSuggestions.Subscribe(toggleSuggestionsSubject.AsObserver());

                    var previousItemPath = NSIndexPath.FromItemSection(indexPath.Item - 1, indexPath.Section);
                    var previous = getItemAt(previousItemPath);
                    var previousIsTask = previous is TaskSuggestion;
                    projectCell.TopSeparatorHidden = !previousIsTask;

                    var nextItemPath = NSIndexPath.FromItemSection(indexPath.Item + 1, indexPath.Section);
                    var next = getItemAt(nextItemPath);
                    var isLastItemInSection = next == null;
                    var isLastSection = indexPath.Section == tableView.NumberOfSections() - 1;
                    projectCell.BottomSeparatorHidden = isLastItemInSection && !isLastSection;
                    break;

                default:
                    throw new Exception($"Unexpected cell type: {cell.GetType()}");
            }

            return cell;
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            if (suggestCreation)
            {
                if (indexPath.Section == 0)
                {
                    OnItemTapped?.Invoke(createEntitySuggestion);
                    tableView.DeselectRow(indexPath, true);
                    return;
                }

                indexPath = indexPath.WithSection(indexPath.Section - 1);
            }

            base.RowSelected(tableView, indexPath);
        }

        public void OnCreateEntitySuggestion(CreateEntitySuggestion createEntitySuggestion)
        {
            this.createEntitySuggestion = createEntitySuggestion;
        }

        public void RegisterViewCells(UITableView tableView)
        {
            tableView.RegisterNibForCellReuse(ReactiveProjectSuggestionViewCell.Nib, ReactiveProjectSuggestionViewCell.Key);
            tableView.RegisterNibForCellReuse(ReactiveTaskSuggestionViewCell.Nib, ReactiveTaskSuggestionViewCell.Key);
            tableView.RegisterNibForCellReuse(CreateEntityViewCell.Nib, CreateEntityViewCell.Key);
            tableView.RegisterNibForHeaderFooterViewReuse(ReactiveWorkspaceHeaderViewCell.Nib, ReactiveWorkspaceHeaderViewCell.Key);
        }

        private object getItemAt(NSIndexPath indexPath)
        {
            if (suggestCreation)
            {
                if (indexPath.Section == 0)
                    return createEntitySuggestion;
                indexPath = indexPath.WithSection(indexPath.Section - 1);
            }

            if (indexPath.Section < 0 || indexPath.Row < 0)
                return null;

            if (indexPath.Section >= DisplayedItems.Count)
                return null;

            var section = DisplayedItems.ElementAtOrDefault(indexPath.Section);

            if (indexPath.Row >= section.Count)
                return null;

            return section.ElementAtOrDefault(indexPath.Row);
        }

        private string getCellIdentifierFor(object item)
        {
            switch(item)
            {
                case ProjectSuggestion _:
                    return ReactiveProjectSuggestionViewCell.Key;

                case TaskSuggestion _:
                    return ReactiveTaskSuggestionViewCell.Key;

                case CreateEntitySuggestion _:
                    return CreateEntityViewCell.Key;

                default:
                    throw new Exception($"Unexpected item type: {item.GetType()}");
            }
        }

        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            if (suggestCreation)
            {
                if (section == 0)
                    return null;
                section--;
            }

            if (!UseGrouping)
                return null;

            var header = (ReactiveWorkspaceHeaderViewCell)tableView.DequeueReusableHeaderFooterView(ReactiveWorkspaceHeaderViewCell.Key);
            header.WorkspaceName = DisplayedItems[(int)section].First().WorkspaceName;
            return header;
        }

        public override nint NumberOfSections(UITableView tableView)
           => base.NumberOfSections(tableView) + (suggestCreation ? 1 : 0);

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            if (!suggestCreation)
                return base.RowsInSection(tableview, section);

            if (section == 0) return 1;

            return base.RowsInSection(tableview, section - 1);
        }

        public override nfloat GetHeightForHeader(UITableView tableView, nint section)
        {
            if (!UseGrouping)
                return 0;

            if (suggestCreation && section == 0)
                return 0;

            return headerHeight;
        }

        public override void RefreshHeader(UITableView tableView, int section)
        {
            //Refreshing isn't really needed for this table view
        }
    }
}

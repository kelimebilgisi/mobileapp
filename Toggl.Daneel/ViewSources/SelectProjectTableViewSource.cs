using System;
using System.Linq;
using Foundation;
using Toggl.Daneel.Views.EntityCreation;
using Toggl.Daneel.Views.StartTimeEntry;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class SelectProjectTableViewSource : ReactiveSectionedListTableViewSource<AutocompleteSuggestion, ReactiveProjectSuggestionViewCell>
    {
        private const int headerHeight = 40;

        private readonly UITableView tableView;

        public InputAction<ProjectSuggestion> ToggleTaskSuggestions { get; set; }

        public bool UseGrouping { get; set; }

        public bool SuggestCreation => CreateEntitySuggestion != null;

        public CreateEntitySuggestion CreateEntitySuggestion { get; set; }

        public SelectProjectTableViewSource(
            UITableView tableView,
            ObservableGroupedOrderedCollection<AutocompleteSuggestion> items,
            string cellIdentifier)
            : base(items, cellIdentifier)
        {
            this.tableView = tableView;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var item = getItemAt(indexPath);
            var cell = tableView.DequeueReusableCell(getCellIdentifierFor(item));

            cell.LayoutMargins = UIEdgeInsets.Zero;
            cell.SeparatorInset = UIEdgeInsets.Zero;
            cell.PreservesSuperviewLayoutMargins = false;

            if (cell is CreateEntityViewcell createEntityViewCell)
            {
                createEntityViewCell.Item = (CreateEntitySuggestion)item;
            }

            if (cell is ReactiveTaskSuggestionViewCell taskCell)
            {
                taskCell.Item = (TaskSuggestion)item;
            }
            
            if (cell is ReactiveProjectSuggestionViewCell projectCell)
            {
                projectCell.Item = (ProjectSuggestion)item;
                projectCell.ToggleTaskSuggestions = ToggleTaskSuggestions;

                var previousItemPath = NSIndexPath.FromItemSection(indexPath.Item - 1, indexPath.Section);
                var previous = getItemAt(previousItemPath);
                var previousIsTask = previous is TaskSuggestion;
                projectCell.TopSeparatorHidden = !previousIsTask;

                var nextItemPath = NSIndexPath.FromItemSection(indexPath.Item + 1, indexPath.Section);
                var next = getItemAt(nextItemPath);
                var isLastItemInSection = next == null;
                var isLastSection = indexPath.Section == tableView.NumberOfSections() - 1;
                projectCell.BottomSeparatorHidden = isLastItemInSection && !isLastSection;
            }

            return cell;
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            if (SuggestCreation)
            {
                if (indexPath.Section == 0)
                {
                    OnItemTapped?.Invoke(CreateEntitySuggestion);
                    tableView.DeselectRow(indexPath, true);
                    return;
                }

                indexPath = NSIndexPath.FromRowSection(indexPath.Row, indexPath.Section - 1);
            }

            base.RowSelected(tableView, indexPath);
        }

        private object getItemAt(NSIndexPath indexPath)
        {
            if (!SuggestCreation) return baseGetItemAt(indexPath);

            if (indexPath.Section == 0)
                return CreateEntitySuggestion;

            return DisplayedItems.ElementAtOrDefault(indexPath.Section - 1)?.ElementAtOrDefault((int)indexPath.Item);
        }

        private object baseGetItemAt(NSIndexPath indexPath)
        {
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
                    return CreateEntityViewcell.Key;
                default:
                    throw new Exception();
            }
        }

        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            if (!SuggestCreation) return baseGetViewForHeader(tableView, section);

            var actualSection = (int)section;
            if (actualSection == 0) return null;

            return baseGetViewForHeader(tableView, actualSection - 1);
        }

        public override nint NumberOfSections(UITableView tableView)
           => base.NumberOfSections(tableView) + (SuggestCreation ? 1 : 0);

        private UIView baseGetViewForHeader(UITableView tableView, nint section)
        {
            if (!UseGrouping) return null;

            var header = (ReactiveWorkspaceHeaderViewCell)tableView.DequeueReusableHeaderFooterView(ReactiveWorkspaceHeaderViewCell.Key);
            header.WorkspaceName = DisplayedItems[(int)section].First().WorkspaceName;
            return header;
        }

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            if (!SuggestCreation)
                return base.RowsInSection(tableview, section);

            if (section == 0) return 1;

            return base.RowsInSection(tableview, section - 1);
        }

        public override nfloat GetHeightForHeader(UITableView tableView, nint section)
        {
            if (UseGrouping)
                return 0;

            if (SuggestCreation && section == 0)
                return 0;

            return headerHeight;
        }

        public override void RefreshHeader(UITableView tableView, int section)
        {
            //Refreshing isn't really needed for this table view
        }
    }
}

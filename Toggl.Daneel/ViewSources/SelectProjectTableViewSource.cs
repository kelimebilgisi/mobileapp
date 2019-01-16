using System;
using System.Linq;
using Foundation;
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

        public InputAction<ProjectSuggestion> ToggleTaskSuggestions { get; set; }
        public InputAction<AutocompleteSuggestion> SelectProject { get; set; }

        public bool UseGrouping { get; set; }

        public SelectProjectTableViewSource(ObservableGroupedOrderedCollection<AutocompleteSuggestion> items, string cellIdentifier)
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

            if (cell is ReactiveTaskSuggestionViewCell taskCell)
            {
                taskCell.Item = (TaskSuggestion)item;
            }
            
            if (cell is ReactiveProjectSuggestionViewCell projectCell)
            {
                projectCell.Item = item;
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

        private AutocompleteSuggestion getItemAt(NSIndexPath indexPath)
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

        private string getCellIdentifierFor(AutocompleteSuggestion item)
        {
            switch(item)
            {
                case ProjectSuggestion _:
                    return ReactiveProjectSuggestionViewCell.Key;
                case TaskSuggestion _:
                    return ReactiveTaskSuggestionViewCell.Key;
                default:
                    throw new Exception();
            }
        }

        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            if (!UseGrouping) return null;

            var header = (ReactiveWorkspaceHeaderViewCell)tableView.DequeueReusableHeaderFooterView(ReactiveWorkspaceHeaderViewCell.Key);
            header.WorkspaceName = DisplayedItems[(int)section].First().WorkspaceName;
            return header;
        }

        public override nfloat GetHeightForHeader(UITableView tableView, nint section)
            => UseGrouping ? headerHeight : 0;

        public override void RefreshHeader(UITableView tableView, int section)
        {
            if (tableView.GetHeaderView(section) is ReactiveWorkspaceHeaderViewCell header)
                header.WorkspaceName = DisplayedItems[section].First().WorkspaceName;
        }
    }
}

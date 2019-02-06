using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Foundation;
using Toggl.Daneel.Views.EntityCreation;
using Toggl.Daneel.Views.StartTimeEntry;
using Toggl.Foundation.Autocomplete.Suggestions;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class SelectProjectTableViewSource : BaseTableViewSource<string, AutocompleteSuggestion>
    {
        private const int headerHeight = 40;

        private readonly ISubject<ProjectSuggestion> toggleTaskSuggestionsSubject = new Subject<ProjectSuggestion>();
        public IObservable<ProjectSuggestion> ToggleTaskSuggestions => toggleTaskSuggestionsSubject.AsObservable();

        public void RegisterViewCells(UITableView tableView)
        {
            tableView.RegisterNibForCellReuse(ReactiveProjectSuggestionViewCell.Nib, ReactiveProjectSuggestionViewCell.Key);
            tableView.RegisterNibForCellReuse(ReactiveTaskSuggestionViewCell.Nib, ReactiveTaskSuggestionViewCell.Key);
            tableView.RegisterNibForCellReuse(CreateEntityViewCell.Nib, CreateEntityViewCell.Key);
            tableView.RegisterNibForHeaderFooterViewReuse(ReactiveWorkspaceHeaderViewCell.Nib, ReactiveWorkspaceHeaderViewCell.Key);
        }

        public override nfloat GetHeightForHeader(UITableView tableView, nint section)
        {
            var header = Sections[(int)section].Header;

            return header == null
                ? 0
                : headerHeight;
        }

        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            var header = HeaderOf(section);

            if (string.IsNullOrEmpty(header))
                return null;

            var headerCell = (ReactiveWorkspaceHeaderViewCell)tableView.DequeueReusableHeaderFooterView(ReactiveWorkspaceHeaderViewCell.Key);
            headerCell.WorkspaceName = header;
            return headerCell;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var autocompleteSuggestion = ModelAt(indexPath);

            switch (autocompleteSuggestion)
            {
                case ProjectSuggestion projectSuggestion:
                    var projectCell = (ReactiveProjectSuggestionViewCell)tableView.DequeueReusableCell(ReactiveProjectSuggestionViewCell.Key, indexPath);
                    projectCell.Item = projectSuggestion;
                    projectCell.ToggleTaskSuggestions.Subscribe(toggleTaskSuggestionsSubject);
                    updateSeparatorVisibility(tableView, projectCell, indexPath);
                    return projectCell;

                case TaskSuggestion taskSuggestion:
                    var taskCell = (ReactiveTaskSuggestionViewCell)tableView.DequeueReusableCell(ReactiveTaskSuggestionViewCell.Key, indexPath);
                    taskCell.Item = taskSuggestion;
                    return taskCell;

                case CreateEntitySuggestion createEntitySuggestion:
                    var createEntityCell = (CreateEntityViewCell)tableView.DequeueReusableCell(CreateEntityViewCell.Key, indexPath);
                    createEntityCell.Item = createEntitySuggestion;
                    return createEntityCell;

                default:
                    throw new Exception("Unexpected item type encountered");
            }
        }

        private void updateSeparatorVisibility(UITableView tableView, ReactiveProjectSuggestionViewCell cell, NSIndexPath indexPath)
        {
            var previousItemPath = NSIndexPath.FromItemSection(indexPath.Item - 1, indexPath.Section);
            var previous = ModelAtOrDefault(previousItemPath);
            var previousIsTask = previous is TaskSuggestion;
            cell.TopSeparatorHidden = !previousIsTask;

            var nextItemPath = NSIndexPath.FromItemSection(indexPath.Item + 1, indexPath.Section);
            var next = ModelAtOrDefault(nextItemPath);
            var isLastItemInSection = next == null;
            var isLastSection = indexPath.Section == tableView.NumberOfSections() - 1;
            cell.BottomSeparatorHidden = isLastItemInSection && !isLastSection;
        }
    }
}

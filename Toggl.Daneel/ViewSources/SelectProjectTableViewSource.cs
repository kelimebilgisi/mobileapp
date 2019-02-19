using System;
using Foundation;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Views;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class SelectProjectTableViewSource : CreateSuggestionGroupedTableViewSource<AutocompleteSuggestion>
    {
        private const string taskCellIdentifier = nameof(TaskSuggestionViewCell);
        private const string headerCellIdentifier = nameof(WorkspaceHeaderViewCell);
        private const string projectCellIdentifier = nameof(ProjectSuggestionViewCell);

        public IMvxCommand<ProjectSuggestion> ToggleTasksCommand { get; set; }

        public SelectProjectTableViewSource(UITableView tableView)
            : base(tableView, projectCellIdentifier, headerCellIdentifier)
        {
            UseAnimations = false;

            tableView.TableFooterView = new UIView();
            tableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            tableView.SeparatorColor = Color.StartTimeEntry.SeparatorColor.ToNativeColor();
            tableView.RegisterNibForCellReuse(TaskSuggestionViewCell.Nib, taskCellIdentifier);
            tableView.RegisterNibForCellReuse(ProjectSuggestionViewCell.Nib, projectCellIdentifier);
            tableView.RegisterNibForHeaderFooterViewReuse(WorkspaceHeaderViewCell.Nib, headerCellIdentifier);
        }

        public bool UseGrouping { get; set; }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = base.GetCell(tableView, indexPath);
            cell.LayoutMargins = UIEdgeInsets.Zero;
            cell.SeparatorInset = UIEdgeInsets.Zero;
            cell.PreservesSuperviewLayoutMargins = false;

            if (cell is ProjectSuggestionViewCell projectCell)
            {
                projectCell.ToggleTasks
                    .Subscribe(ToggleTasksCommand.Execute)
                    .DisposedBy(projectCell.DisposeBag);

                var previousItemPath = NSIndexPath.FromItemSection(indexPath.Item - 1, indexPath.Section);
                var previous = GetItemAt(previousItemPath);
                var previousIsTask = previous is TaskSuggestion;
                projectCell.TopSeparatorHidden = !previousIsTask;

                var nextItemPath = NSIndexPath.FromItemSection(indexPath.Item + 1, indexPath.Section);
                var next = GetItemAt(nextItemPath);
                var isLastItemInSection = next == null;
                var isLastSection = indexPath.Section == tableView.NumberOfSections() - 1;
                projectCell.BottomSeparatorHidden = isLastItemInSection && !isLastSection;
            }

            return cell;
        }

        public override nfloat GetHeightForHeader(UITableView tableView, nint section)
            => UseGrouping ? base.GetHeightForHeader(tableView, section) : 0;

        public override UIView GetViewForHeader(UITableView tableView, nint section)
            => UseGrouping ? base.GetViewForHeader(tableView, section) : null;

        public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath) => 48;

        protected override UITableViewCell GetOrCreateCellFor(UITableView tableView, NSIndexPath indexPath, object item)
            => tableView.DequeueReusableCell(getIdentifier(item), indexPath);

        private string getIdentifier(object item)
        {
            if (item is string)
                return CreateEntityCellIdentifier;

            if (item is ProjectSuggestion)
                return projectCellIdentifier;

            if (item is TaskSuggestion)
                return taskCellIdentifier;

            throw new ArgumentException($"{nameof(item)} must be either of type {nameof(ProjectSuggestion)} or {nameof(TaskSuggestion)}.");
        }

        protected override object GetCreateSuggestionItem() => $"Create project \"{Text}\"";

        protected override WorkspaceGroupedCollection<AutocompleteSuggestion> CloneCollection(WorkspaceGroupedCollection<AutocompleteSuggestion> collection)
            => new WorkspaceGroupedCollection<AutocompleteSuggestion>(collection.WorkspaceName, collection.WorkspaceId, collection);
    }
}

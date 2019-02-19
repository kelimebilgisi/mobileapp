using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreGraphics;
using Foundation;
using MvvmCross.Commands;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Views;
using Toggl.Daneel.Views.EntityCreation;
using Toggl.Daneel.Views.StartTimeEntry;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Extensions.Reactive;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class StartTimeEntryTableViewSource : BaseTableViewSource<string, AutocompleteSuggestion>
    {
        private const int defaultRowHeight = 48;
        private const int headerHeight = 40;
        private const int noEntityCellHeight = 108;
        private BehaviorRelay<ProjectSuggestion> toggleTasks = new BehaviorRelay<ProjectSuggestion>(null);

        public Action TableRenderCallback { get; set; }
        public IObservable<ProjectSuggestion> ToggleTasks { get; }

        public StartTimeEntryTableViewSource(UITableView tableView)
        {
            tableView.SeparatorStyle = UITableViewCellSeparatorStyle.SingleLine;
            tableView.SeparatorColor = Color.StartTimeEntry.SeparatorColor.ToNativeColor();
            tableView.SeparatorInset = UIEdgeInsets.Zero;
            tableView.TableFooterView = new UIView(new CGRect(0, 0, 0, 1));
            tableView.RegisterNibForCellReuse(TagSuggestionViewCell.Nib, TagSuggestionViewCell.Identifier);
            tableView.RegisterNibForCellReuse(TaskSuggestionViewCell.Nib, TaskSuggestionViewCell.Identifier);
            tableView.RegisterNibForCellReuse(StartTimeEntryViewCell.Nib, StartTimeEntryViewCell.Identifier);
            tableView.RegisterNibForCellReuse(NoEntityInfoViewCell.Nib, NoEntityInfoViewCell.Identifier);
            tableView.RegisterNibForCellReuse(ProjectSuggestionViewCell.Nib, ProjectSuggestionViewCell.Identifier);
            tableView.RegisterNibForCellReuse(StartTimeEntryEmptyViewCell.Nib, StartTimeEntryEmptyViewCell.Identifier);
            tableView.RegisterNibForCellReuse(CreateEntityViewCell.Nib, CreateEntityViewCell.Identifier);
            tableView.RegisterNibForHeaderFooterViewReuse(WorkspaceHeaderViewCell.Nib, WorkspaceHeaderViewCell.Identifier);

            ToggleTasks = toggleTasks.Where(p => p != null).AsObservable();
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var model = ModelAt(indexPath);

            switch (model)
            {
                case TagSuggestion tag:
                {
                    var cell = (TagSuggestionViewCell) tableView.DequeueReusableCell(TagSuggestionViewCell.Identifier,
                        indexPath);
                    cell.Item = tag;
                    return cell;
                }
                case TaskSuggestion task:
                {
                    var cell = (TaskSuggestionViewCell) tableView.DequeueReusableCell(TaskSuggestionViewCell.Identifier,
                        indexPath);
                    cell.Item = task;
                    return cell;
                }
                case TimeEntrySuggestion timeEntry:
                {
                    var cell = (StartTimeEntryViewCell) tableView.DequeueReusableCell(StartTimeEntryViewCell.Identifier,
                        indexPath);
                    cell.Item = timeEntry;
                    return cell;
                }
                case ProjectSuggestion project:
                {
                    var cell = (ProjectSuggestionViewCell) tableView.DequeueReusableCell(
                        ProjectSuggestionViewCell.Identifier,
                        indexPath);
                    cell.Item = project;

                    cell.ToggleTasks
                        .Subscribe(toggleTasks.Accept)
                        .DisposedBy(cell.DisposeBag);

                    cell.TopSeparatorHidden = true;
                    cell.BottomSeparatorHidden = true;
                    return cell;
                }
                case QuerySymbolSuggestion querySuggestion:
                {
                    var cell = (StartTimeEntryEmptyViewCell)tableView.DequeueReusableCell(
                        StartTimeEntryEmptyViewCell.Identifier,
                        indexPath);
                    cell.Item = querySuggestion;
                    return cell;
                }

                case CreateEntitySuggestion creteEntity:
                {
                    var cell = (CreateEntityViewCell) tableView.DequeueReusableCell(CreateEntityViewCell.Identifier,
                        indexPath);
                    cell.Item = creteEntity;
                    return cell;
                }

                case NoEntityInfoMessage noEntityInfoMessage:
                {
                    var cell = (NoEntityInfoViewCell) tableView.DequeueReusableCell(NoEntityInfoViewCell.Identifier,
                        indexPath);
                    cell.Item = noEntityInfoMessage;
                    return cell;
                }

                default:
                    throw new InvalidOperationException("Wrong cell type");
            }
        }

        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            if (Sections.Count == 1) return null;
            if (string.IsNullOrEmpty(HeaderOf(section))) return null;

            var header = tableView.DequeueReusableHeaderFooterView(WorkspaceHeaderViewCell.Identifier) as WorkspaceHeaderViewCell;
            header.Item = HeaderOf(section);
            return header;
        }

        public override nfloat GetHeightForHeader(UITableView tableView, nint section)
        {
            if (Sections.Count == 1) return 0;
            if (string.IsNullOrEmpty(HeaderOf(section))) return 0;

            return headerHeight;
        }

        public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath)
        {
            var model = ModelAt(indexPath);

            if (model is NoEntityInfoMessage)
                return noEntityCellHeight;

            return defaultRowHeight;
        }

        public override void WillDisplay(UITableView tableView, UITableViewCell cell, NSIndexPath indexPath)
        {
            if (tableView.IndexPathsForVisibleRows.Last().Row == indexPath.Row)
            {
                TableRenderCallback();
            }
        }
    }
}

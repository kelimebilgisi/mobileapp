﻿using System;
using System.Linq;
using Foundation;
using MvvmCross.Commands;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Views;
using Toggl.Daneel.Views.StartTimeEntry;
using Toggl.Foundation;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class StartTimeEntryTableViewSource : BaseTableViewSource<string, AutocompleteSuggestion>
    {
        private const int defaultRowHeight = 48;
        private const int headerHeight = 40;
        private const int noEntityCellHeight = 60;
        private const string tagIconIdentifier = "icIllustrationTagsSmall";
        private const string projectIconIdentifier = "icIllustrationProjectsSmall";

        private readonly NoEntityInfoMessage noTagsInfoMessage
            = new NoEntityInfoMessage(
                text: Resources.NoTagsInfoMessage,
                imageResource: tagIconIdentifier,
                characterToReplace: '#');

        private readonly NoEntityInfoMessage noProjectsInfoMessge
            = new NoEntityInfoMessage(
                text: Resources.NoProjectsInfoMessage,
                imageResource: projectIconIdentifier,
                characterToReplace: '@');

        public bool IsSuggestingProjects { get; set; }
        public bool ShouldShowNoTagsInfoMessage { get; set; }
        public bool ShouldShowNoProjectsInfoMessage { get; set; }
        public Action TableRenderCallback { get; set; }
        public IMvxCommand<ProjectSuggestion> ToggleTasksCommand { get; set; }
        public IMvxCommand<AutocompleteSuggestion> SelectSuggestionCommand { get; set; }

        public StartTimeEntryTableViewSource(UITableView tableView)
        {
            tableView.TableFooterView = new UIView();
            tableView.SeparatorStyle = UITableViewCellSeparatorStyle.SingleLine;
            tableView.SeparatorColor = Color.StartTimeEntry.SeparatorColor.ToNativeColor();
            tableView.SeparatorInset = UIEdgeInsets.Zero;
            tableView.RegisterNibForCellReuse(TagSuggestionViewCell.Nib, TagSuggestionViewCell.Identifier);
            tableView.RegisterNibForCellReuse(TaskSuggestionViewCell.Nib, TaskSuggestionViewCell.Identifier);
            tableView.RegisterNibForCellReuse(StartTimeEntryViewCell.Nib, StartTimeEntryViewCell.Identifier);
            tableView.RegisterNibForCellReuse(NoEntityInfoViewCell.Nib, NoEntityInfoViewCell.Identifier);
            tableView.RegisterNibForCellReuse(ProjectSuggestionViewCell.Nib, ProjectSuggestionViewCell.Identifier);
            tableView.RegisterNibForCellReuse(StartTimeEntryEmptyViewCell.Nib, StartTimeEntryEmptyViewCell.Identifier);
            tableView.RegisterNibForHeaderFooterViewReuse(WorkspaceHeaderViewCell.Nib, WorkspaceHeaderViewCell.Identifier);
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

                    cell.ToggleTasksCommand = ToggleTasksCommand;
                    return cell;
                }
                case QuerySymbolSuggestion querySuggestion:
                {
                    var cell = (StartTimeEntryEmptyViewCell) tableView.DequeueReusableCell(
                        StartTimeEntryEmptyViewCell.Identifier,
                        indexPath);
                    cell.Item = querySuggestion;
                    return cell;
                }

                default:
                    throw new InvalidOperationException("Wrong cell type");
            }

            /*
            case NoEntityInfoMessage noEntity:
            {
                var cell = (NoEntityInfoViewCell)tableView.DequeueReusableCell(NoEntityInfoViewCell.Identifier,
                    indexPath);
                cell.Item = noEntity;
                //noEntityCell.NoEntityInfoMessage = getNoEntityInfoMessage();
                return cell;
            }

            cell.LayoutMargins = UIEdgeInsets.Zero;
            cell.SeparatorInset = UIEdgeInsets.Zero;
            cell.PreservesSuperviewLayoutMargins = false;
            */
        }

        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            if (Sections.Count == 1) return null;

            var header = tableView.DequeueReusableHeaderFooterView(WorkspaceHeaderViewCell.Identifier) as WorkspaceHeaderViewCell;
            header.Item = HeaderOf(section);
            return header;
        }

        public override nfloat GetHeightForHeader(UITableView tableView, nint section)
        {
            return (Sections.Count == 1) ? 0 : headerHeight;
        }

        public override void WillDisplay(UITableView tableView, UITableViewCell cell, NSIndexPath indexPath)
        {
            if (tableView.IndexPathsForVisibleRows.Last().Row == indexPath.Row)
            {
                TableRenderCallback();
            }
        }

        /*
        public override nint RowsInSection(UITableView tableview, nint section)
        {
            if (UseGrouping) return base.RowsInSection(tableview, section);

            return GetGroupAt(section).Count()
                + (SuggestCreation ? 1 : 0)
                + (ShouldShowNoTagsInfoMessage ? 1 : 0)
                + (ShouldShowNoProjectsInfoMessage ? 1 : 0);
        }

        public override nint NumberOfSections(UITableView tableView)
        {
            if (!UseGrouping) return 1;

            return base.NumberOfSections(tableView);
        }


        protected override object GetItemAt(NSIndexPath indexPath)
        {
            if (!UseGrouping && SuggestCreation)
            {
                var index = (int)indexPath.Item - 1;
                if (index < 0) return GetCreateSuggestionItem();
                if (ShouldShowNoTagsInfoMessage) return noTagsInfoMessage;
                if (ShouldShowNoProjectsInfoMessage) return noProjectsInfoMessge;

                var newIndexPath = NSIndexPath.FromRowSection(indexPath.Section, index);
                return GroupedItems.ElementAtOrDefault(indexPath.Section)?.ElementAtOrDefault(index);
            }

            if (ShouldShowNoTagsInfoMessage) return noTagsInfoMessage;
            if (ShouldShowNoProjectsInfoMessage) return noProjectsInfoMessge;

            return base.GetItemAt(indexPath);
        }

        protected override UITableViewHeaderFooterView GetOrCreateHeaderViewFor(UITableView tableView)
            => tableView.DequeueReusableHeaderFooterView(headerCellIdentifier);

        protected override UITableViewCell GetOrCreateCellFor(UITableView tableView, NSIndexPath indexPath, object item)
            => tableView.DequeueReusableCell(getIdentifier(item), indexPath);

        public override nfloat GetHeightForHeader(UITableView tableView, nint section)
            => !UseGrouping ? 0 : base.GetHeightForHeader(tableView, section);

        public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath)
        {
            if (!UseGrouping && SuggestCreation)
            {
                var index = (int)indexPath.Item - 1;
                if (index < 0) return defaultRowHeight;

                return ShouldShowNoTagsInfoMessage || ShouldShowNoProjectsInfoMessage
                    ? noEntityCellHeight
                    : defaultRowHeight;
            }

            return ShouldShowNoTagsInfoMessage || ShouldShowNoProjectsInfoMessage
                ? defaultRowHeight + noEntityCellHeight
                : defaultRowHeight;
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            base.RowSelected(tableView, indexPath);

            var item = GetItemAt(indexPath);
            if (item is AutocompleteSuggestion autocompleteSuggestion)
                SelectSuggestionCommand.Execute(autocompleteSuggestion);
        }

        private string getIdentifier(object item)
        {
            switch (item)
            {
                case string _:
                    return CreateEntityCellIdentifier;

                case ProjectSuggestion _:
                    return projectCellIdentifier;

                case QuerySymbolSuggestion _:
                    return emptySuggestionIdentifier;

                case TagSuggestion _:
                    return tagCellIdentifier;

                case TaskSuggestion _:
                    return taskCellIdentifier;

                case NoEntityInfoMessage _:
                    return noEntityInfoCellIdentifier;

                default:
                    return timeEntryCellIdentifier;
            }
        }

        protected override object GetCreateSuggestionItem()
            => IsSuggestingProjects
                ? $"{Resources.CreateProject} \"{Text}\""
                : $"{Resources.CreateTag} \"{Text}\"";
*/
        private NoEntityInfoMessage getNoEntityInfoMessage()
        {
            if (ShouldShowNoTagsInfoMessage)
                return noTagsInfoMessage;

            if (ShouldShowNoProjectsInfoMessage)
                return noProjectsInfoMessge;

            throw new InvalidOperationException("This method should not be called, when there is no info message to be shown");
        }

     /*   protected override WorkspaceGroupedCollection<AutocompleteSuggestion> CloneCollection(WorkspaceGroupedCollection<AutocompleteSuggestion> collection)
            => new WorkspaceGroupedCollection<AutocompleteSuggestion>(collection.WorkspaceName, collection.WorkspaceId, collection);
            */
    }
}

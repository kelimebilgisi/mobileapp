using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreGraphics;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Views;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class TimeEntriesLogViewSource
        : BaseTableViewSource<DaySummaryViewModel, LogItemViewModel>
    {
        private const int rowHeight = 64;
        private const int headerHeight = 48;

        private readonly Subject<LogItemViewModel> continueTapSubject = new Subject<LogItemViewModel>();
        private readonly Subject<LogItemViewModel> swipeToContinueSubject = new Subject<LogItemViewModel>();
        private readonly Subject<LogItemViewModel> swipeToDeleteSubject = new Subject<LogItemViewModel>();
        private readonly Subject<GroupId> toggleGroupExpansionSubject = new Subject<GroupId>();

        private readonly ReplaySubject<TimeEntriesLogViewCell> firstCellSubject = new ReplaySubject<TimeEntriesLogViewCell>(1);
        private readonly Subject<bool> isDraggingSubject = new Subject<bool>();

        //Using the old API so that delete action would work on pre iOS 11 devices
        private readonly UITableViewRowAction deleteTableViewRowAction;

        public const int SpaceBetweenSections = 20;

        public IObservable<LogItemViewModel> ContinueTap { get; }
        public IObservable<LogItemViewModel> SwipeToContinue { get; }
        public IObservable<LogItemViewModel> SwipeToDelete { get; }
        public IObservable<GroupId> ToggleGroupExpansion { get; }

        public IObservable<TimeEntriesLogViewCell> FirstCell { get; }
        public IObservable<bool> IsDragging { get; }

        public TimeEntriesLogViewSource()
        {
            if (!NSThread.Current.IsMainThread)
            {
                throw new InvalidOperationException($"{nameof(TimeEntriesLogViewSource)} must be created on the main thread");
            }

            if (!UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
            {
                deleteTableViewRowAction = createLegacySwipeDeleteAction();
            }

            ContinueTap = continueTapSubject.AsObservable();
            SwipeToContinue = swipeToContinueSubject.AsObservable();
            SwipeToDelete = swipeToDeleteSubject.AsObservable();
            ToggleGroupExpansion = toggleGroupExpansionSubject.AsObservable();

            FirstCell = firstCellSubject.AsObservable();
            IsDragging = isDraggingSubject.AsObservable();
        }

        public override UIView GetViewForFooter(UITableView tableView, nint section)
            => new UIView(new CGRect(0, 0, UIScreen.MainScreen.Bounds.Width, SpaceBetweenSections));

        public override nfloat GetHeightForHeader(UITableView tableView, nint section) => headerHeight;

        public override nfloat GetHeightForFooter(UITableView tableView, nint section) => SpaceBetweenSections;

        public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath) => rowHeight;

        // It needs this method, otherwise the ContentOffset will reset to 0 everytime the table reloads. ¯\_(ツ)_/¯
        public override nfloat EstimatedHeight(UITableView tableView, NSIndexPath indexPath) => rowHeight;

        public override UITableViewRowAction[] EditActionsForRow(UITableView tableView, NSIndexPath indexPath)
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
                return new[] { deleteTableViewRowAction };

            return new UITableViewRowAction[]{};
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = (TimeEntriesLogViewCell)tableView.DequeueReusableCell(TimeEntriesLogViewCell.Identifier);

            var model = ModelAt(indexPath);

            cell.ContinueButtonTap
                .Subscribe(() => continueTapSubject.OnNext(model))
                .DisposedBy(cell.DisposeBag);

            cell.ToggleGroup
                .Subscribe(() => toggleGroupExpansionSubject.OnNext(model.GroupId))
                .DisposedBy(cell.DisposeBag);

            if (indexPath.Row == 0 && indexPath.Section == 0)
                firstCellSubject.OnNext(cell);

            cell.Item = model;

            return cell;
        }

        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            var header = (TimeEntriesLogHeaderView)tableView.DequeueReusableHeaderFooterView(TimeEntriesLogHeaderView.Identifier);
            header.Update(HeaderOf((int)section));
            return header;
        }

        public override UISwipeActionsConfiguration GetLeadingSwipeActionsConfiguration(UITableView tableView, NSIndexPath indexPath)
            => createSwipeActionConfiguration(continueSwipeActionFor, indexPath);

        public override UISwipeActionsConfiguration GetTrailingSwipeActionsConfiguration(UITableView tableView, NSIndexPath indexPath)
            => createSwipeActionConfiguration(deleteSwipeActionFor, indexPath);

        public override void DraggingStarted(UIScrollView scrollView)
        {
            isDraggingSubject.OnNext(true);
        }

        public override void DraggingEnded(UIScrollView scrollView, bool willDecelerate)
        {
            isDraggingSubject.OnNext(false);
        }

        private void handleDeleteTableViewRowAction(UITableViewRowAction rowAction, NSIndexPath indexPath)
        {
            var item = ModelAt(indexPath);
            swipeToDeleteSubject.OnNext(item);
        }

        private UITableViewRowAction createLegacySwipeDeleteAction()
        {
            var deleteAction = UITableViewRowAction.Create(
                UITableViewRowActionStyle.Destructive,
                Resources.Delete,
                handleDeleteTableViewRowAction);
            deleteAction.BackgroundColor = Foundation.MvvmCross.Helper.Color.TimeEntriesLog.DeleteSwipeActionBackground.ToNativeColor();
            return deleteAction;
        }

        private UISwipeActionsConfiguration createSwipeActionConfiguration(
            Func<LogItemViewModel, UIContextualAction> factory, NSIndexPath indexPath)
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
                return null;

            var item = ModelAt(indexPath);
            if (item == null)
                return null;

            return UISwipeActionsConfiguration.FromActions(new[] { factory(item) });
        }

        private UIContextualAction continueSwipeActionFor(LogItemViewModel viewModel)
        {
            var continueAction = UIContextualAction.FromContextualActionStyle(
                UIContextualActionStyle.Normal,
                Resources.Continue,
                (action, sourceView, completionHandler) =>
                {
                    swipeToContinueSubject.OnNext(viewModel);
                    completionHandler.Invoke(finished: true);
                }
            );
            continueAction.BackgroundColor = Foundation.MvvmCross.Helper.Color.TimeEntriesLog.ContinueSwipeActionBackground.ToNativeColor();
            return continueAction;
        }

        private UIContextualAction deleteSwipeActionFor(LogItemViewModel viewModel)
        {
            var deleteAction = UIContextualAction.FromContextualActionStyle(
                UIContextualActionStyle.Destructive,
                Resources.Delete,
                (action, sourceView, completionHandler) =>
                {
                    swipeToDeleteSubject.OnNext(viewModel);
                    completionHandler.Invoke(finished: true);
                }
            );
            deleteAction.BackgroundColor = Foundation.MvvmCross.Helper.Color.TimeEntriesLog.DeleteSwipeActionBackground.ToNativeColor();
            return deleteAction;
        }
    }
}

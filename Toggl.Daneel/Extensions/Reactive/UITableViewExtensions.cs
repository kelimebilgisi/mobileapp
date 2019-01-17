using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive;
using Toggl.Daneel.Cells;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Reactive;
using UIKit;

namespace Toggl.Daneel.Extensions.Reactive
{
    public static class UITableViewExtensions
    {
        public static IDisposable Bind<TModel, TCell>(this IReactive<UITableView> tableView, ReactiveSectionedListTableViewSource<TModel, TCell> dataSource, IObservable<bool> suggestCreation = null)
            where TCell : BaseTableViewCell<TModel>
            => new ReactiveTableViewBinder<TModel, TCell>(tableView.Base, dataSource, suggestCreation);
    }
}

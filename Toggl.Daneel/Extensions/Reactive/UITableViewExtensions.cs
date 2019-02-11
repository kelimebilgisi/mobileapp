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
        public static IDisposable Bind<TModel, TCell>(this IReactive<UITableView> tableView, ReactiveSectionedListTableViewSource<TModel, TCell> dataSource)
            where TCell : BaseTableViewCell<TModel>
            => new ReactiveTableViewBinder<TModel, TCell>(tableView.Base, dataSource);

        public static IObserver<IEnumerable<CollectionSection<THeader, TModel>>> ReloadSections<THeader, TModel>(
            this IReactive<UITableView> reactive, BaseTableViewSource<THeader, TModel> dataSource)
        {
            return Observer.Create<IEnumerable<CollectionSection<THeader, TModel>>>(list =>
            {
                dataSource.SetSections(list);
                reactive.Base.ReloadData();
            });
        }

        public static IObserver<IEnumerable<TModel>> ReloadItems<THeader, TModel>(
            this IReactive<UITableView> reactive, BaseTableViewSource<THeader, TModel> dataSource)
        {
            return Observer.Create<IEnumerable<TModel>>(list =>
            {
                dataSource.SetItems(list);
                reactive.Base.ReloadData();
            });
        }
    }
}

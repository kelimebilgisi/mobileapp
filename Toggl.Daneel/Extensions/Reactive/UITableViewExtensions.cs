using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using CoreAnimation;
using CoreImage;
using CoreText;
using Foundation;
using Toggl.Daneel.ViewSources;
using Toggl.Daneel.ViewSources.Generic;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Reactive;
using UIKit;

namespace Toggl.Daneel.Extensions.Reactive
{
    public static class UITableViewExtensions
    {
        public static IObserver<IEnumerable<TSection>> ReloadSections<TSection, THeader, TModel>(
            this IReactive<UITableView> reactive, BaseTableViewSource<TSection, THeader, TModel> dataSource)
        where TSection : ISectionModel<THeader, TModel>, new()
        {
            return Observer.Create<IEnumerable<TSection>>(list =>
            {
                dataSource.SetSections(list);
                reactive.Base.ReloadData();
            });
        }

        public static IObserver<IEnumerable<TSection>> AnimateSections<TSection, THeader, TModel>(
            this IReactive<UITableView> reactive, BaseTableViewSource<TSection, THeader, TModel> dataSource)
            where TSection : IAnimatableSectionModel<THeader, TModel>, new()
            where TModel : IDiffable, IEquatable<TModel>
            where THeader : IDiffable
        {
            return Observer.Create<IEnumerable<TSection>>(finalSections =>
            {
                var initialSections = dataSource.Sections;
                if (initialSections == null || initialSections.Count == 0)
                {
                    dataSource.SetSections(finalSections);
                    reactive.Base.ReloadData();
                    return;
                }

                // if view is not in view hierarchy, performing batch updates will crash the app
                if (reactive.Base.Window == null)
                {
                    dataSource.SetSections(finalSections);
                    reactive.Base.ReloadData();
                    return;
                }

                var diff = new Diffing<TSection, THeader, TModel>(initialSections, finalSections);
                var changeset = diff.computeDifferences();

                foreach (var difference in changeset)
                {
                    reactive.Base.BeginUpdates();
                    dataSource.SetSections(difference.FinalSections);
                    reactive.Base.performChangesetUpdates(difference);
                    reactive.Base.EndUpdates();
                }
            });
        }

        public static IObserver<IEnumerable<TModel>> ReloadItems<TSection, THeader, TModel>(
            this IReactive<UITableView> reactive, BaseTableViewSource<TSection, THeader, TModel> dataSource)
        where TSection : SectionModel<THeader, TModel>, new()
        {
            return Observer.Create<IEnumerable<TModel>>(list =>
            {
                dataSource.SetItems(list);
                reactive.Base.ReloadData();
            });
        }

        private static void performChangesetUpdates<TSection, THeader, TModel>(this UITableView tableView, Diffing<TSection, THeader, TModel>.Changeset changes)
            where TSection : IAnimatableSectionModel<THeader, TModel>, new()
            where TModel : IDiffable, IEquatable<TModel>
            where THeader : IDiffable

        {
            NSIndexSet newIndexSet(List<int> indexes)
            {
                var indexSet = new NSMutableIndexSet();
                foreach (var i in indexes)
                {
                    indexSet.Add((nuint) i);
                }

                return indexSet as NSIndexSet;
            }

            tableView.DeleteSections(newIndexSet(changes.DeletedSections), UITableViewRowAnimation.Automatic);
            // Updated sections doesn't mean reload entire section, somebody needs to update the section view manually
            // otherwise all cells will be reloaded for nothing.
            tableView.InsertSections(newIndexSet(changes.InsertedSections), UITableViewRowAnimation.Automatic);

            foreach (var (from, to) in changes.MovedSections)
            {
                tableView.MoveSection(from, to);
            }
            tableView.DeleteRows(
                changes.DeletedItems.Select(item => NSIndexPath.FromRowSection(item.itemIndex, item.sectionIndex)).ToArray(),
                UITableViewRowAnimation.Automatic
            );

            tableView.InsertRows(
                changes.InsertedItems.Select(item =>
                    NSIndexPath.FromItemSection(item.itemIndex, item.sectionIndex)).ToArray(),
                UITableViewRowAnimation.Automatic
            );
            tableView.ReloadRows(
                changes.UpdatedItems.Select(item => NSIndexPath.FromRowSection(item.itemIndex, item.sectionIndex))
                    .ToArray(),
                // No animation so it doesn't fade showing the cells behind it
                UITableViewRowAnimation.None
            );

            foreach (var (from, to) in changes.MovedItems)
            {
                tableView.MoveRow(
                    NSIndexPath.FromRowSection(from.itemIndex, from.sectionIndex),
                    NSIndexPath.FromRowSection(to.itemIndex, to.sectionIndex)
                );
            }
        }
    }
}

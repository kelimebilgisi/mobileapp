using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CoreGraphics;
using Foundation;
using Toggl.Foundation.MvvmCross.Collections;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public abstract class BaseTableViewSource<THeader, TModel> : UITableViewSource
    {
        protected IImmutableList<CollectionSection<THeader, TModel>> Sections { get; private set; }

        public EventHandler<TModel> OnItemTapped { get; set; }
        public EventHandler<CGPoint> OnScrolled { get; set; }

        public BaseTableViewSource() : this((IEnumerable<TModel>)null)
        {
        }

        public BaseTableViewSource(IEnumerable<TModel> items)
        {
            SetItems(items);
        }

        public BaseTableViewSource(IEnumerable<CollectionSection<THeader, TModel>> sections)
        {
            SetSections(sections);
        }

        public void SetItems(IEnumerable<TModel> items)
        {
            var sections = items != null
                ? ImmutableList.Create(new CollectionSection<THeader, TModel>(default(THeader), items))
                : null;

            SetSections(sections);
        }

        public void SetSections(IEnumerable<CollectionSection<THeader, TModel>> sections)
        {
            Sections = sections?.ToImmutableList() ?? ImmutableList<CollectionSection<THeader, TModel>>.Empty;
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            tableView.DeselectRow(indexPath, true);
            OnItemTapped?.Invoke(this, ModelAt(indexPath));
        }

        public override void Scrolled(UIScrollView scrollView)
        {
            OnScrolled?.Invoke(this, scrollView.ContentOffset);
        }

        public override nint NumberOfSections(UITableView tableView)
            => Sections.Count;

        public override nint RowsInSection(UITableView tableview, nint section)
            => Sections[(int)section].Items.Count;

        protected THeader HeaderOf(nint section)
            => Sections[(int)section].Header;

        protected TModel ModelAt(NSIndexPath path)
            => Sections[path.Section].Items[path.Row];

        protected TModel ModelAtOrDefault(NSIndexPath path)
        {
            var section = Sections.ElementAtOrDefault(path.Section);
            if (section == null)
                return default(TModel);

            return section.Items.ElementAtOrDefault(path.Row);
        }
    }
}

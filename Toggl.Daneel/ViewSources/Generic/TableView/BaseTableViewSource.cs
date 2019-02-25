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
    public abstract class BaseTableViewSource<TSection, THeader, TModel> : UITableViewSource
    where TSection : ISectionModel<THeader, TModel>, new()
    {
        public List<TSection> Sections { get; private set; }

        public EventHandler<TModel> OnItemTapped { get; set; }
        public EventHandler<CGPoint> OnScrolled { get; set; }

        public BaseTableViewSource() : this((IEnumerable<TModel>)null)
        {
        }

        public BaseTableViewSource(IEnumerable<TModel> items)
        {
            SetItems(items);
        }

        public BaseTableViewSource(IEnumerable<TSection> sections)
        {
            SetSections(sections);
        }

        public void SetItems(IEnumerable<TModel> items)
        {
            IEnumerable<TSection> sections = new List<TSection>();
            if (items != null)
            {
                var newSection = new TSection();
                newSection.Initialize(default(THeader), items);
                sections = ImmutableList.Create(newSection);
            }

            SetSections(sections);
        }

        public void SetSections(IEnumerable<TSection> sections)
        {
            Sections = sections?.ToList() ?? new List<TSection>();
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
    }
}

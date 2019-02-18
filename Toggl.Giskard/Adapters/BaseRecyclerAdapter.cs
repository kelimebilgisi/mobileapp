using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Android.Runtime;
using Android.Support.V7.Util;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Giskard.ViewHolders;
using Toggl.Multivac.Extensions;
using Toggl.Foundation.MvvmCross.Interfaces;
using Handler = Android.OS.Handler;
using Toggl.Giskard.Adapters.DiffingStrategies;

namespace Toggl.Giskard.Adapters
{
    public abstract class BaseRecyclerAdapter<T> : RecyclerView.Adapter
        where T : IEquatable<T>
    {
        private readonly IDiffingStrategy<T> diffingStrategy;

        public IObservable<T> ItemTapObservable => itemTapSubject.AsObservable();

        private Subject<T> itemTapSubject = new Subject<T>();

        private IList<T> items = new List<T>();

        private IList<T> nextUpdate;

        private bool isUpdateRunning;

        private readonly object updateLock = new object();

        public virtual IList<T> Items
        {
            get => items;
            set => SetItems(value ?? new List<T>());
        }

        protected BaseRecyclerAdapter(IDiffingStrategy<T> diffingStrategy = null)
        {
            this.diffingStrategy = normalizeDiffingStrategy(diffingStrategy);

            HasStableIds = this.diffingStrategy.HasStableIds;
        }

        private IDiffingStrategy<T> normalizeDiffingStrategy(IDiffingStrategy<T> diffingStrategy)
        {
            if (diffingStrategy != null)
                return diffingStrategy;

            if (typeof(T).ImplementsOrDerivesFrom<IDiffableByIdentifier<T>>())
                return new IdentifierEqualityDiffingStrategy<T>();

            return new EquatableDiffingStrategy<T>();
        }

        protected BaseRecyclerAdapter(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var inflater = LayoutInflater.From(parent.Context);
            var viewHolder = CreateViewHolder(parent, inflater, viewType);
            viewHolder.TappedSubject = itemTapSubject;
            return viewHolder;
        }

        protected abstract BaseRecyclerViewHolder<T> CreateViewHolder(ViewGroup parent, LayoutInflater inflater,
            int viewType);

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var item = GetItem(position);
            ((BaseRecyclerViewHolder<T>)holder).Item = item;
        }

        public override int ItemCount => items.Count;

        public override long GetItemId(int position)
        {
            var item = items[position];
            return diffingStrategy.GetItemId(item);
        }

        public virtual T GetItem(int viewPosition)
            => items[viewPosition];

        protected virtual void SetItems(IList<T> newItems)
        {
            lock (updateLock)
            {
                if (!isUpdateRunning)
                {
                    isUpdateRunning = true;
                    processUpdate(newItems);
                }
                else
                {
                    nextUpdate = newItems;
                }
            }
        }

        private void processUpdate(IList<T> newItems)
        {
            var oldItems = items;
            var handler = new Handler();
            Task.Run(() =>
            {
                var diffResult = DiffUtil.CalculateDiff(new BaseDiffCallBack(oldItems, newItems, diffingStrategy));
                handler.Post(() =>
                {
                    dispatchUpdates(newItems, diffResult);
                });
            });
        }

        private void dispatchUpdates(IList<T> newItems, DiffUtil.DiffResult diffResult)
        {
            items = newItems;
            diffResult.DispatchUpdatesTo(this);
            lock (updateLock)
            {
                if (nextUpdate != null)
                {
                    processUpdate(nextUpdate);
                    nextUpdate = null;
                }
                else
                {
                    isUpdateRunning = false;
                }
            }
        }

        private sealed class BaseDiffCallBack : DiffUtil.Callback
        {
            private IList<T> oldItems;
            private IList<T> newItems;
            private IDiffingStrategy<T> diffingStrategy;

            public BaseDiffCallBack(IList<T> oldItems, IList<T> newItems, IDiffingStrategy<T> diffingStrategy)
            {
                this.oldItems = oldItems;
                this.newItems = newItems;
                this.diffingStrategy = diffingStrategy;
            }

            public override bool AreContentsTheSame(int oldItemPosition, int newItemPosition)
            {
                var oldItem = oldItems[oldItemPosition];
                var newItem = newItems[newItemPosition];

                return diffingStrategy.AreContentsTheSame(oldItem, newItem);
            }

            public override bool AreItemsTheSame(int oldItemPosition, int newItemPosition)
            {
                var oldItem = oldItems[oldItemPosition];
                var newItem = newItems[newItemPosition];

                return diffingStrategy.AreItemsTheSame(oldItem, newItem);
            }

            public override int NewListSize => newItems.Count;
            public override int OldListSize => oldItems.Count;
        }
    }
}

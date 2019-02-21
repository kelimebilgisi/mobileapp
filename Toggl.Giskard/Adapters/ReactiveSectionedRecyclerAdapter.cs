using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Android.OS;
using Android.Support.V7.Util;
using Android.Support.V7.Widget;
using Android.Views;
using Java.Lang;
using MvvmCross.Binding.Extensions;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Collections.Changes;
using Toggl.Giskard.ViewHolders;

namespace Toggl.Giskard.Adapters
{
    public abstract class ReactiveSectionedRecyclerAdapter<TModel, TModelWrapper, TSectionHeaderModel, TSectionHeaderWrapper, TItemViewHolder, TSectionViewHolder> : RecyclerView.Adapter
        where TItemViewHolder : BaseRecyclerViewHolder<TModelWrapper>
        where TSectionViewHolder : BaseRecyclerViewHolder<TSectionHeaderWrapper>
        where TSectionHeaderModel : class
    {
        public const int SectionViewType = 0;
        public const int ItemViewType = 1;

        private readonly object collectionUpdateLock = new object();
        private bool isUpdateRunning;
        private bool hasPendingUpdate;

        private IImmutableList<CollectionSection<TSectionHeaderModel, TModel>> items;
        private IReadOnlyList<FlatItemInfo> currentItems;

        public ReactiveSectionedRecyclerAdapter()
        {
            items = ImmutableList<CollectionSection<TSectionHeaderModel, TModel>>.Empty;
            currentItems = new FlatItemInfo[0];
        }

        public virtual int HeaderOffset { get; } = 0;

        public override int ItemCount => currentItems.Count + HeaderOffset;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if (viewType == ItemViewType)
            {
                return CreateItemViewHolder(parent);
            }

            return CreateHeaderViewHolder(parent);
        }

        public override int GetItemViewType(int position)
        {
            return currentItems[position - HeaderOffset].ViewType;
        }

        protected TModel getItemAt(int position)
        {
            return currentItems[position - HeaderOffset].Item;
        }

        public void UpdateCollection(IEnumerable<CollectionSection<TSectionHeaderModel, TModel>> items)
        {
            this.items = items.ToImmutableList();

            lock (collectionUpdateLock)
            {
                if (isUpdateRunning)
                {
                    hasPendingUpdate = true;
                    return;
                }

                isUpdateRunning = true;
            }

            startCurrentCollectionUpdate();
        }

        private void startCurrentCollectionUpdate()
        {
            var handler = new Handler();
            new Thread(() =>
            {
                var newImmutableItems = flattenItems(items);
                var diffResult = calculateDiffFromCurrentItems(newImmutableItems);
                handler.Post(() => dispatchUpdates(newImmutableItems, diffResult));
            }).Start();
        }

        private DiffUtil.DiffResult calculateDiffFromCurrentItems(IReadOnlyList<FlatItemInfo> newImmutableItems)
        {
            return DiffUtil.CalculateDiff(
                new HeaderOffsetAwareDiffCallback(currentItems,
                    newImmutableItems,
                    AreItemContentsTheSame,
                    AreSectionsRepresentationsTheSame,
                    HeaderOffset)
            );
        }

        private void dispatchUpdates(IReadOnlyList<FlatItemInfo> newImmutableItems, DiffUtil.DiffResult diffResult)
        {
            currentItems = newImmutableItems;
            diffResult.DispatchUpdatesTo(this);

            lock (collectionUpdateLock)
            {
                if (hasPendingUpdate)
                {
                    hasPendingUpdate = false;
                    startCurrentCollectionUpdate();
                }
                else
                {
                    isUpdateRunning = false;
                }
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            switch (holder)
            {
                case TItemViewHolder itemViewHolder:
                    itemViewHolder.Item = currentItems[position - HeaderOffset].WrappedItem;
                    break;

                case TSectionViewHolder sectionViewHolder:
                    sectionViewHolder.Item = currentItems[position - HeaderOffset].WrappedSection;
                    break;

                default:
                    if (TryBindCustomViewType(holder, position) == false)
                    {
                        throw new InvalidOperationException($"{holder.GetType().Name} was not bound to position {position}");
                    }

                    break;
            }
        }

        protected abstract bool TryBindCustomViewType(RecyclerView.ViewHolder holder, int position);

        protected abstract TSectionViewHolder CreateHeaderViewHolder(ViewGroup parent);

        protected abstract TItemViewHolder CreateItemViewHolder(ViewGroup parent);

        protected abstract long IdFor(TModel item);

        protected abstract long IdForSection(TSectionHeaderModel section);

        protected abstract TModelWrapper Wrap(TModel item);

        protected abstract TSectionHeaderWrapper Wrap(TSectionHeaderModel section);


        /*
         * The visual representation of the items are the same
         */
        protected abstract bool AreItemContentsTheSame(TModel item1, TModel item2);

        /*
         * The visual representation of the section label is the same
         */
        protected abstract bool AreSectionsRepresentationsTheSame(
            TSectionHeaderModel oneHeader, TSectionHeaderModel otherHeader, IReadOnlyList<TModel> one, IReadOnlyList<TModel> other);

        private struct FlatItemInfo
        {
            public int ViewType { get; }
            public TModel Item { get; }
            public TModelWrapper WrappedItem { get; }

            public IReadOnlyList<TModel> Section { get; }
            public TSectionHeaderModel SectionHeader { get; }
            public TSectionHeaderWrapper WrappedSection { get; }
            public long Id { get; }

            public FlatItemInfo(TModel item, Func<TModel, long> idProvider, Func<TModel, TModelWrapper> wrapper)
            {
                ViewType = ItemViewType;
                Item = item;
                Section = null;
                SectionHeader = null;
                Id = idProvider(item);
                WrappedItem = wrapper(item);
                WrappedSection = default(TSectionHeaderWrapper);
            }

            public FlatItemInfo(IReadOnlyList<TModel> section, TSectionHeaderModel header, Func<TSectionHeaderModel, long> idProvider, Func<TSectionHeaderModel, TSectionHeaderWrapper> wrapper)
            {
                ViewType = SectionViewType;
                Item = default(TModel);
                Section = section;
                SectionHeader = header;
                Id = idProvider(header);
                WrappedItem = default(TModelWrapper);
                WrappedSection = wrapper(header);
            }
        }

        private IReadOnlyList<FlatItemInfo> flattenItems(IEnumerable<CollectionSection<TSectionHeaderModel, TModel>> groups)
        {
            var flattenedTimeEntriesList = new List<FlatItemInfo>();

            foreach (var group in groups)
            {
                flattenedTimeEntriesList.Add(new FlatItemInfo(group.Items, group.Header, IdForSection, Wrap));
                flattenedTimeEntriesList.AddRange(group.Items.Select(item => new FlatItemInfo(item, IdFor, Wrap)).ToList());
            }

            return flattenedTimeEntriesList.ToImmutableList();
        }

        private sealed class HeaderOffsetAwareDiffCallback : DiffUtil.Callback
        {
            /*
             * To understand how the DiffUtil.Callback works, please check
             * https://developer.android.com/reference/android/support/v7/util/DiffUtil.Callback
             */
            private readonly IReadOnlyList<FlatItemInfo> oldItems;
            private readonly IReadOnlyList<FlatItemInfo> newItems;
            private readonly Func<TModel, TModel, bool> itemContentsAreTheSame;
            private readonly Func<TSectionHeaderModel, TSectionHeaderModel, IReadOnlyList<TModel>, IReadOnlyList<TModel>, bool> sectionContentsAreTheSame;
            private readonly int headerOffset;

            public HeaderOffsetAwareDiffCallback(
                IReadOnlyList<FlatItemInfo> oldItems,
                IReadOnlyList<FlatItemInfo> newItems,
                Func<TModel, TModel, bool> itemContentsAreTheSame,
                Func<TSectionHeaderModel, TSectionHeaderModel, IReadOnlyList<TModel>, IReadOnlyList<TModel>, bool> sectionContentsAreTheSame,
                int headerOffset)
            {
                this.oldItems = oldItems;
                this.newItems = newItems;
                this.itemContentsAreTheSame = itemContentsAreTheSame;
                this.sectionContentsAreTheSame = sectionContentsAreTheSame;
                this.headerOffset = headerOffset;
            }

            public override bool AreContentsTheSame(int oldItemPosition, int newItemPosition)
            {
                if (oldItemPosition < headerOffset || newItemPosition < headerOffset)
                {
                    return oldItemPosition == newItemPosition;
                }

                var oldItem = oldItems[oldItemPosition - headerOffset];
                var newItem = newItems[newItemPosition - headerOffset];

                if (oldItem.ViewType != newItem.ViewType) return false;

                if (oldItem.ViewType == ItemViewType)
                {
                    return itemContentsAreTheSame(oldItem.Item, newItem.Item);
                }

                return sectionContentsAreTheSame(
                    oldItem.SectionHeader,
                    newItem.SectionHeader,
                    oldItem.Section ?? ImmutableList<TModel>.Empty,
                    newItem.Section ?? ImmutableList<TModel>.Empty);
            }

            public override bool AreItemsTheSame(int oldItemPosition, int newItemPosition)
            {
                if (oldItemPosition < headerOffset || newItemPosition < headerOffset)
                {
                    return oldItemPosition == newItemPosition;
                }

                var oldItem = oldItems[oldItemPosition - headerOffset];
                var newItem = newItems[newItemPosition - headerOffset];

                return oldItem.ViewType == newItem.ViewType
                       && oldItem.Id == newItem.Id;
            }

            public override int NewListSize => newItems.Count + headerOffset;

            public override int OldListSize => oldItems.Count + headerOffset;
        }
    }
}

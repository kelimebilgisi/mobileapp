using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync.States.CleanUp;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.CleanUp
{
    public sealed class DeleteNonReferencedInaccessibleTagsStateTests
    {
        private readonly DeleteNonReferencedInaccessibleTagsState state;

        private readonly ITimeEntriesSource timeEntriesDataSource = Substitute.For<ITimeEntriesSource>();

        private readonly IDataSource<IThreadSafeTag, IDatabaseTag> tagsDataSource = Substitute.For<IDataSource<IThreadSafeTag, IDatabaseTag>>();

        public DeleteNonReferencedInaccessibleTagsStateTests()
        {
            state = new DeleteNonReferencedInaccessibleTagsState(tagsDataSource, timeEntriesDataSource);
        }

        [Fact, LogIfTooSlow]
        public async Task DeleteUnreferencedTagsInInaccessibleWorkspace()
        {
            var accessibleWorkspace = new MockWorkspace(1000);
            var inaccessibleWorkspace = new MockWorkspace(2000, isInaccessible: true);

            var tag1 = new MockTag(1001, accessibleWorkspace);
            var tag2 = new MockTag(1002, accessibleWorkspace, SyncStatus.SyncFailed);
            var tag3 = new MockTag(1003, accessibleWorkspace, SyncStatus.RefetchingNeeded);
            var tag4 = new MockTag(2001, inaccessibleWorkspace);
            var tag5 = new MockTag(2002, inaccessibleWorkspace, SyncStatus.SyncNeeded);
            var tag6 = new MockTag(2003, inaccessibleWorkspace, SyncStatus.RefetchingNeeded);
            var tag7 = new MockTag(2004, inaccessibleWorkspace);
            var tag8 = new MockTag(2005, inaccessibleWorkspace);

            var te1 = new MockTimeEntry(10001, accessibleWorkspace, tags: new[] { tag1 });
            var te2 = new MockTimeEntry(10002, accessibleWorkspace, tags: new[] { tag2 });
            var te3 = new MockTimeEntry(20001, inaccessibleWorkspace, tags: new[] { tag4 });
            var te4 = new MockTimeEntry(20002, inaccessibleWorkspace, tags: new[] { tag5 });

            var tags = new[] { tag1, tag2, tag3, tag4, tag5, tag6, tag7, tag8 };
            var timeEntries = new[] { te1, te2, te3, te4 };

            var unreferencedTags = new[] { tag7, tag8 };
            var neededTags = tags.Where(tag => !unreferencedTags.Contains(tag));

            configureDataSource(tags, timeEntries);

            await state.Start().SingleAsync();

            await tagsDataSource.Received().DeleteAll(Arg.Is<IEnumerable<IThreadSafeTag>>(arg =>
                arg.All(tag => unreferencedTags.Contains(tag)) &&
                arg.None(tag => neededTags.Contains(tag))));
        }

        private void configureDataSource(IThreadSafeTag[] tags, IThreadSafeTimeEntry[] timeEntries)
        {
            tagsDataSource
                .GetAll(Arg.Any<Func<IDatabaseTag, bool>>(), Arg.Is(true))
                .Returns(callInfo =>
                {
                    var predicate = callInfo[0] as Func<IDatabaseTag, bool>;
                    var filteredTags = tags.Where(predicate);
                    return Observable.Return(filteredTags.Cast<IThreadSafeTag>());
                });

            timeEntriesDataSource
                .GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>(), Arg.Is(true))
                .Returns(callInfo =>
                {
                    var predicate = callInfo[0] as Func<IDatabaseTimeEntry, bool>;
                    var filteredTimeEntries = timeEntries.Where(predicate);
                    return Observable.Return(filteredTimeEntries.Cast<IThreadSafeTimeEntry>());
                });
        }
    }
}

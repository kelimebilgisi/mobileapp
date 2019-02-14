using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Interactors
{
    internal class UpdateMultipleTimeEntriesInteractor : IInteractor<IObservable<IEnumerable<IThreadSafeTimeEntry>>>
    {
        private readonly EditTimeEntryDto dto;
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IInteractorFactory interactorFactory;
        private readonly long[] ids;

        public UpdateMultipleTimeEntriesInteractor(
            ITimeService timeService,
            ITogglDataSource dataSource,
            IInteractorFactory interactorFactory,
            EditTimeEntryDto dto, long[] ids)
        {
            Ensure.Argument.IsNotNull(dto, nameof(dto));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(ids, nameof(ids));

            this.dto = dto;
            this.dataSource = dataSource;
            this.timeService = timeService;
            this.interactorFactory = interactorFactory;
            this.ids = ids;
        }

        public IObservable<IEnumerable<IThreadSafeTimeEntry>> Execute()
            => interactorFactory.GetMultipleTimeEntriesById(ids).Execute()
                .SelectMany(CommonFunctions.Identity)
                .Select(convertToDto)
                .SelectMany(dto => interactorFactory.UpdateTimeEntry(dto).Execute())
                .ToList();

        private EditTimeEntryDto convertToDto(IThreadSafeTimeEntry timeEntry)
        {
            var updatedDto = dto;

            updatedDto.Id = timeEntry.Id;
            updatedDto.StartTime = timeEntry.Start;
            updatedDto.StopTime = timeEntry.Duration.HasValue
                ? timeEntry.Start + TimeSpan.FromSeconds(timeEntry.Duration.Value)
                : (DateTimeOffset?)null;

            return updatedDto;
        }
    }
}

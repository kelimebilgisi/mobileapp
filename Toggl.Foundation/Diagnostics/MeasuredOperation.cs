﻿namespace Toggl.Foundation.Diagnostics
{
    public enum MeasuredOperation
    {
        None,
        OpenStartView,
        StartTimeEntrySuggestionsLoadingTime,
        StartTimeEntrySuggestionsRenderingTime,
        OpenSelectTagsView,
        OpenSettingsView,
        EditTimeEntryFromCalendar,
        EditTimeEntryFromMainLog,
        OpenReportsViewForTheFirstTime,
        OpenCreateProjectViewFromStartTimeEntryView,
        OpenSelectProjectFromEditView,
        OpenReportsFromGiskard,
        CreateMainLogItemViewHolder,
        CreateMainLogSectionViewHolder,
        BindMainLogItemVH,
        BindMainLogSectionVH,
        BindMainLogSuggestionsVH,
        CreateMainLogSuggestionsViewHolder,
        MainActivityOnCreate,
        RandomForest2StepsProjectPrediction,
        RandomForest2StepsTimeEntryPrediction,
        RandomForest1StepTimeEntryPrediction
    }
}

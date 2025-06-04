namespace VivaldiCustomLauncher.BuildTrigger;

internal enum WorkflowStatus {

    COMPLETED,
    ACTION_REQUIRED,
    CANCELLED,
    FAILURE,
    NEUTRAL,
    SKIPPED,
    STALE,
    SUCCESS,
    TIMED_OUT,
    IN_PROGRESS,
    QUEUED,
    REQUESTED,
    WAITING

}
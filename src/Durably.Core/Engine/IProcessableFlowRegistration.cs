namespace Durably.Engine;
/// <summary>
/// Typed registration that can process an execution without reflection dispatch.
/// </summary>
internal interface IProcessableFlowRegistration : IFlowRegistration
{
    Task<FlowRunResult> ProcessAsync(
        ExecutionProcessor processor,
        ExecutionRecord record,
        string runnerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);
}

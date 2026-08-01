namespace Durably.Builder;
/// <summary>
/// Ambient information about the step currently executing. Passed to every step so it can log,
/// branch on attempt number, or correlate with the owning flow instance.
/// </summary>
public interface IStepContext
{
    /// <summary>The flow definition identity (derived from the flow CLR type).</summary>
    string FlowName { get; }

    /// <summary>The instance/business key this run is bound to (e.g. an order id).</summary>
    string InstanceId { get; }

    /// <summary>System-generated identity of this execution run.</summary>
    string RunId { get; }

    /// <summary>The key of the step currently executing.</summary>
    string StepKey { get; }

    /// <summary>1-based attempt counter for the current step (increments on each retry).</summary>
    int Attempt { get; }

    /// <summary>Stable key for idempotent side effects (flow + run + step).</summary>
    string IdempotencyKey { get; }
}

namespace Durably.Engine;
internal sealed class FlowRegistry : IFlowRegistry
{
    private readonly Dictionary<string, IFlowRegistration> _flows = new(StringComparer.Ordinal);

    public void Register(IFlowRegistration registration)
    {
        if (registration is null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        if (string.IsNullOrWhiteSpace(registration.Name))
        {
            throw new ArgumentException("Flow name is required.", nameof(registration));
        }

        _flows[registration.Name] = registration;
    }

    public bool TryGet(string flowName, out IFlowRegistration registration)
    {
        if (string.IsNullOrWhiteSpace(flowName))
        {
            registration = null!;
            return false;
        }

        return _flows.TryGetValue(flowName, out registration!);
    }
}

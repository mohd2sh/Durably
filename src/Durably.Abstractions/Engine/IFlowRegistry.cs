namespace Durably.Engine;
public interface IFlowRegistry
{
    void Register(IFlowRegistration registration);

    bool TryGet(string flowName, out IFlowRegistration registration);
}

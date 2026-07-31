namespace Durably.Engine;
public interface IFlowRegistration
{
    string Name { get; }

    Type StateType { get; }
}

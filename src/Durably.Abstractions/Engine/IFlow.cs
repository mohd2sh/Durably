namespace Durably.Engine;
public interface IFlow<TState>
{
    void Build(IFlowBuilder<TState> builder);
}

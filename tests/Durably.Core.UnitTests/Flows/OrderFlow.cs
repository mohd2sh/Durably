namespace Durably.Core.UnitTests;

public sealed class OrderFlow : IFlow<OrderState>
{
    public void Build(IFlowBuilder<OrderState> builder) => builder
        .Step<GenerateReport>()
        .Step<SendEmail>()
        .Step<Finalize>();
}

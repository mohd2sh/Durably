using Sample.AspNetCore.Api.Services;

namespace Sample.AspNetCore.Api.Workflows.Oop.OrderFinalize;

public sealed class GenerateReportStep : IStep<OrderFinalizeState>
{
    private readonly IReportService _reports;

    public GenerateReportStep(IReportService reports)
    {
        _reports = reports;
    }

    public async Task ExecuteAsync(OrderFinalizeState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.Report = await _reports.GenerateAsync(state.Order.Id, cancellationToken);
    }
}

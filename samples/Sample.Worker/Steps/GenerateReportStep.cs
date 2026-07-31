using Durably;
using Sample.Worker.Models;
using Sample.Worker.Services;

namespace Sample.Worker.Steps;

public sealed class GenerateReportStep : IStep<OrderFinalizeState>
{
    private readonly IReportService _reports;

    public GenerateReportStep(IReportService reports)
    {
        _reports = reports;
    }

    public async Task ExecuteAsync(OrderFinalizeState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.Report = await _reports.GenerateAsync(state.OrderId, cancellationToken);
    }
}

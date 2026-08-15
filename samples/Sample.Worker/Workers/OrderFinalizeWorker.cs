using Sample.Worker.Models;

namespace Sample.Worker.Workers;

/// <summary>
/// Periodically enqueues sample orders via <see cref="IFlowEngine.StartAsync"/>.
/// The library-hosted <c>DurablyWorkerService</c> claims and processes Pending work.
/// </summary>
public sealed class OrderFinalizeWorker : BackgroundService
{
    private readonly IFlowEngine _engine;
    private readonly IFlowBuilder<OrderFinalizeState> _flow;
    private readonly ILogger<OrderFinalizeWorker> _logger;
    private readonly IConfiguration _configuration;

    public OrderFinalizeWorker(
        IFlowEngine engine,
        IFlowBuilder<OrderFinalizeState> flow,
        ILogger<OrderFinalizeWorker> logger,
        IConfiguration configuration)
    {
        _engine = engine;
        _flow = flow;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(_configuration.GetValue("Worker:PollIntervalSeconds", 5));
        var orderIds = _configuration.GetSection("Worker:PendingOrderIds").Get<string[]>() ?? Array.Empty<string>();

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var orderId in orderIds)
            {
                await EnqueueOrderAsync(orderId, stoppingToken);
            }

            await Task.Delay(pollInterval, stoppingToken);
        }
    }

    private async Task EnqueueOrderAsync(string orderId, CancellationToken cancellationToken)
    {
        var state = new OrderFinalizeState
        {
            OrderId = orderId,
            CustomerEmail = $"{orderId}@example.com"
        };

        var result = await _engine.StartAsync(_flow, orderId, state, cancellationToken: cancellationToken);

        if (result.WasCreated)
        {
            _logger.LogInformation("Order {OrderId} enqueued (run {RunId}).", orderId, result.RunId);
        }
        else
        {
            _logger.LogDebug(
                "Order {OrderId} start outcome {Outcome} (run {RunId}).",
                orderId,
                result.Outcome,
                result.RunId);
        }
    }
}

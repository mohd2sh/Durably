using Sample.AspNetCore.Api.Services;
using Sample.AspNetCore.Api.Workflows.Oop.OrderFinalize;

namespace Sample.AspNetCore.Api.Registration;

public static class SampleServiceCollectionExtensions
{
    public static IServiceCollection AddSampleApplication(this IServiceCollection services)
    {
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<IEmailService, EmailService>();
        services.AddSingleton<IOrderService, OrderService>();
        services.AddSingleton<IPaymentGateway, PaymentGateway>();
        services.AddSingleton<ISubscriptionBilling, SubscriptionBilling>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IFlowSuccessHandler<OrderFinalizeState>, OrderFinalizeSuccessHandler>();
        services.AddSingleton<IFlowFailureHandler<OrderFinalizeState>, OrderFinalizeFailureHandler>();
        return services;
    }
}

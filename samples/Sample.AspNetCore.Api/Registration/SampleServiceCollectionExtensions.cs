using Durably;
using Sample.AspNetCore.Api.Handlers;
using Sample.AspNetCore.Api.Models;
using Sample.AspNetCore.Api.Services;

namespace Sample.AspNetCore.Api.Registration;

public static class SampleServiceCollectionExtensions
{
    public static IServiceCollection AddSampleApplication(this IServiceCollection services)
    {
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<IEmailService, EmailService>();
        services.AddSingleton<IOrderService, OrderService>();
        services.AddSingleton<IFlowSuccessHandler<OrderFinalizeState>, OrderFinalizeSuccessHandler>();
        services.AddSingleton<IFlowFailureHandler<OrderFinalizeState>, OrderFinalizeFailureHandler>();
        return services;
    }
}

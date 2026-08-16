namespace Sample.AspNetCore.Api.Workflows.Oop.NotificationDispatch;

public sealed class NotificationDispatchFlow : IFlow<NotificationDispatchState>
{
    public void Build(IFlowBuilder<NotificationDispatchState> builder) => builder
        .Step<PrepareNotificationStep>()
        .Choose(s => s.Priority)
            .When("urgent", b => b
                .Step<EscalateUrgentStep>()
                .Choose(s => s.Channel)
                    .When("sms", inner => inner.Step<SendSmsStep>("urgent-sms"))
                    .When("push", inner => inner.Step<SendPushStep>("urgent-push"))
                    .Otherwise(inner => inner.Step<SendEmailNotificationStep>("urgent-email"))
                .EndChoose())
            .When("normal", b => b
                .Choose(s => s.Channel)
                    .When("sms", inner => inner.Step<SendSmsStep>("normal-sms"))
                    .Otherwise(inner => inner.Step<SendEmailNotificationStep>("normal-email"))
                .EndChoose())
            .Otherwise(b => b.Step<SendEmailNotificationStep>("low-email"))
        .EndChoose()
        .Step<MarkDispatchedStep>()
        .OnSuccess(s => s.Completed = true);
}

using Durably;

namespace Sample.AspNetCore.Api.Workflows.Fluent.InvoiceReminder;

/// <summary>Marker type for a stable fluent flow identity (not an <see cref="IFlow{TState}"/>).</summary>
public sealed class InvoiceReminderFlow
{
}

/// <summary>
/// Pure fluent / lambda definition — no <see cref="IFlow{TState}"/> and no <see cref="IStep{TState}"/>.
/// Registered via <c>AddFlow</c> alongside the OOP assembly scan.
/// </summary>
public static class InvoiceReminderFlowDefinition
{
    public static IFlowBuilder<InvoiceReminderState> Build() =>
        Flow.For<InvoiceReminderFlow, InvoiceReminderState>()
            .Step("validate", async (state, ct) =>
            {
                if (string.IsNullOrWhiteSpace(state.InvoiceId))
                {
                    throw new InvalidOperationException("Invoice id is required.");
                }

                if (state.AmountDue <= 0)
                {
                    throw new InvalidOperationException("Amount due must be positive.");
                }

                state.Validated = true;
                state.Note = $"Validated invoice {state.InvoiceId}";
                await Task.CompletedTask;
            })
            .StepIf(
                s => s.DaysOverdue >= 30,
                "escalate-note",
                async (state, ct) =>
                {
                    state.Escalated = true;
                    state.Note = $"Escalated: {state.DaysOverdue} days overdue";
                    await Task.CompletedTask;
                })
            .Choose(s => s.Channel)
                .When("sms", b => b.Step("send-sms", async (state, ct) =>
                {
                    state.DispatchedVia = "sms";
                    state.ReminderSent = true;
                    state.Note = $"SMS reminder to {state.CustomerPhone}";
                    await Task.CompletedTask;
                }))
                .Otherwise(b => b.Step("send-email", async (state, ct) =>
                {
                    state.DispatchedVia = "email";
                    state.ReminderSent = true;
                    state.Note = $"Email reminder to {state.CustomerEmail}";
                    await Task.CompletedTask;
                }))
            .EndChoose()
            .Step("mark-sent", async (state, ct) =>
            {
                state.Completed = true;
                await Task.CompletedTask;
            })
            .OnSuccess(s => s.Note = $"Reminder sent via {s.DispatchedVia} for {s.InvoiceId}");
}

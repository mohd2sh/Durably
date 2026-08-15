namespace Sample.AspNetCore.Api.Workflows.Fluent.InvoiceReminder;

public sealed class InvoiceReminderState
{
    public string InvoiceId { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public string CustomerPhone { get; set; } = string.Empty;

    public int DaysOverdue { get; set; }

    public string Channel { get; set; } = "email";

    public decimal AmountDue { get; set; }

    public bool Validated { get; set; }

    public bool Escalated { get; set; }

    public string? DispatchedVia { get; set; }

    public bool ReminderSent { get; set; }

    public bool Completed { get; set; }

    public string? Note { get; set; }
}

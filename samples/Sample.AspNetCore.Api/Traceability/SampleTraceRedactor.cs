namespace Sample.AspNetCore.Api.Traceability;

/// <summary>Strips obvious PII tokens from trace JSON before persistence.</summary>
public sealed class SampleTraceRedactor : ITraceRedactor
{
    public void Redact(TraceRecord record)
    {
        record.InputJson = RedactJson(record.InputJson);
        record.OutputJson = RedactJson(record.OutputJson);
        if (!string.IsNullOrEmpty(record.ExceptionMessage)
            && record.ExceptionMessage.Contains('@', StringComparison.Ordinal))
        {
            record.ExceptionMessage = "[redacted]";
        }
    }

    private static string? RedactJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return json;
        }

        // Lightweight demo redaction: mask email-like tokens in serialized state.
        return System.Text.RegularExpressions.Regex.Replace(
            json,
            @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
            "[redacted-email]");
    }
}

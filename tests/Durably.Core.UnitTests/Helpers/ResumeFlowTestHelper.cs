namespace Durably.Core.UnitTests;

internal static class ResumeFlowTestHelper
{
    public sealed class Counters
    {
        public int Generate;
        public int Enrich;
        public int Email;
        public int Finalize;
        public bool FailOnce = true;
    }

    private sealed class ResumeTestFlow;

    public static (IFlowBuilder<OrderState> Flow, Counters Counters) CreateFailOnceEmailFlow()
    {
        var counters = new Counters();
        var flow = Flow.For<ResumeTestFlow, OrderState>()
            .Step("generate", (s, ct) =>
            {
                counters.Generate++;
                s.Report = "report";
                return Task.CompletedTask;
            })
            .Step("enrich", (s, ct) =>
            {
                counters.Enrich++;
                s.Report += "!";
                return Task.CompletedTask;
            })
            .Step("email", (s, ct) =>
            {
                counters.Email++;
                if (counters.FailOnce)
                {
                    counters.FailOnce = false;
                    throw new InvalidOperationException("smtp down");
                }

                s.EmailSent = true;
                return Task.CompletedTask;
            })
            .Step("finalize", (s, ct) =>
            {
                counters.Finalize++;
                s.Finalized = true;
                return Task.CompletedTask;
            });

        return (flow, counters);
    }
}

namespace Durably.Builder;
/// <summary>A multi-branch fork opened by <see cref="IFlowBuilder{TState}.Choose{TKey}"/>.</summary>
public interface IChoiceBuilder<TState, TKey>
{
    /// <summary>Define the steps taken when the selector equals <paramref name="value"/>.</summary>
    IChoiceBuilder<TState, TKey> When(TKey value, Action<IFlowBuilder<TState>> branch);

    /// <summary>Define the steps taken when no <c>When</c> matched.</summary>
    IChoiceBuilder<TState, TKey> Otherwise(Action<IFlowBuilder<TState>> branch);

    /// <summary>Close the fork and return to the parent pipeline.</summary>
    IFlowBuilder<TState> EndChoose();
}

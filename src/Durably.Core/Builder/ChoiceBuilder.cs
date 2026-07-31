using System.Linq;

namespace Durably.Builder;
internal sealed class ChoiceBuilder<TState, TKey> : IChoiceBuilder<TState, TKey>
{
    private static readonly IEqualityComparer<TKey> Comparer = EqualityComparer<TKey>.Default;

    private readonly FlowBuilder<TState> _parent;
    private readonly Func<TState, TKey> _selector;
    private readonly List<(TKey Value, Action<IFlowBuilder<TState>> Branch)> _whens = new();
    private Action<IFlowBuilder<TState>>? _otherwise;

    public ChoiceBuilder(FlowBuilder<TState> parent, Func<TState, TKey> selector)
    {
        _parent = parent;
        _selector = selector;
    }

    public IChoiceBuilder<TState, TKey> When(TKey value, Action<IFlowBuilder<TState>> branch)
    {
        if (branch is null)
        {
            throw new ArgumentNullException(nameof(branch));
        }

        _whens.Add((value, branch));
        return this;
    }

    public IChoiceBuilder<TState, TKey> Otherwise(Action<IFlowBuilder<TState>> branch)
    {
        _otherwise = branch ?? throw new ArgumentNullException(nameof(branch));
        return this;
    }

    public IFlowBuilder<TState> EndChoose()
    {
        foreach (var (value, branch) in _whens)
        {
            var captured = value;
            var sub = _parent.CreateBranch(s => Comparer.Equals(_selector(s), captured));
            branch(sub);
            _parent.AppendCompiledNodes(sub.Nodes);
        }

        if (_otherwise is not null)
        {
            var values = _whens.Select(w => w.Value).ToList();
            var sub = _parent.CreateBranch(s => !values.Any(v => Comparer.Equals(_selector(s), v)));
            _otherwise(sub);
            _parent.AppendCompiledNodes(sub.Nodes);
        }

        return _parent;
    }
}

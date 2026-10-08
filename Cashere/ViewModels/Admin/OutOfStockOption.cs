using Cashere.Models;

namespace Cashere.ViewModels.Admin;

public sealed record OutOfStockOption(string Label, OutOfStockBehavior? Value)
{
    public override string ToString() => Label;
}

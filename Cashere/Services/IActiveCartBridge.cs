using System;

namespace Cashere.Services;

public enum MobileCartMutationKind
{
    Add,
    Decrement,
    Remove,
    Clear
}

public record MobileCartMutation(MobileCartMutationKind Kind, int ProductId = 0, int Quantity = 0);

// In-process bridge from the embedded sync server to the desktop POS.
public interface IActiveCartBridge
{
    event Action<MobileCartMutation>? MobileCartChanged;
}

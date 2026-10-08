namespace Cashere.Services;

public enum PosSoundEvent
{
    SaleCompleted,
    CheckoutFailed
}

public interface IPosFeedbackService
{
    void Play(PosSoundEvent soundEvent);
}

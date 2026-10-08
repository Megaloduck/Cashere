using System.Threading.Tasks;

namespace Cashere.Services;

public interface IPosPaymentNotifier
{
    Task NotifySaleCompletedAsync(string saleNumber, decimal amount);
}

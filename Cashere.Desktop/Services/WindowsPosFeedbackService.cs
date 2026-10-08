using System.Runtime.InteropServices;
using Cashere.Services;

namespace Cashere.Desktop.Services;

// Uses the two standard Windows system sounds for checkout feedback. Keeping
// this adapter in the Windows desktop project leaves other targets free to
// provide their own audio implementation.
public sealed class WindowsPosFeedbackService : IPosFeedbackService
{
    private const uint Asterisk = 0x00000040;
    private const uint Exclamation = 0x00000030;

    [DllImport("user32.dll", SetLastError = false)]
    private static extern bool MessageBeep(uint beepType);

    public void Play(PosSoundEvent soundEvent)
    {
        _ = MessageBeep(soundEvent == PosSoundEvent.SaleCompleted ? Asterisk : Exclamation);
    }
}

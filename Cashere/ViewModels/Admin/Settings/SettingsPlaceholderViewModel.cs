using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.ViewModels.Admin.Settings;

// Stand-in for a settings category that doesn't have real logic behind it yet.
// Renders a short description plus the bullet list of what the category will
// eventually cover, so the full shape of Settings is visible in the UI even
// before each screen is built out. Every not-yet-built category shares this
// one class/view rather than getting its own near-duplicate pair.
public class SettingsPlaceholderViewModel : ViewModelBase
{
    public string Title { get; }
    public string Description { get; }
    public IReadOnlyList<string> PlannedItems { get; }

    public SettingsPlaceholderViewModel(string title, string description, IReadOnlyList<string> plannedItems)
    {
        Title = title;
        Description = description;
        PlannedItems = plannedItems;
    }
}
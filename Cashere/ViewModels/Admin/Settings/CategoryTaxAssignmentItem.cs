using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Cashere.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin.Settings;

// One row in the Business Info "Categories" tax-rate assignment grid - picks
// which TaxRate (if any) a Category resolves to. Saves on selection change
// rather than needing its own Save button, same "toggle flips, write
// happens" feel as ToggleSwitch-backed settings elsewhere in this app.
public partial class CategoryTaxAssignmentItem : ObservableObject
{
    private readonly Func<int, int?, Task> _onChanged;
    private bool _suppressSave;

    public int CategoryId { get; }
    public string CategoryName { get; }
    public ObservableCollection<TaxRate> AvailableTaxRates { get; }

    [ObservableProperty]
    private TaxRate? _selectedTaxRate;

    public CategoryTaxAssignmentItem(
        Category category, ObservableCollection<TaxRate> availableTaxRates, Func<int, int?, Task> onChanged)
    {
        CategoryId = category.Id;
        CategoryName = category.Name;
        AvailableTaxRates = availableTaxRates;
        _onChanged = onChanged;

        _suppressSave = true;
        SelectedTaxRate = category.TaxRate is null
            ? null
            : availableTaxRates.FirstOrDefault(t => t.Id == category.TaxRate.Id);
        _suppressSave = false;
    }

    partial void OnSelectedTaxRateChanged(TaxRate? value)
    {
        if (_suppressSave) return;
        _ = _onChanged(CategoryId, value?.Id);
    }

    [RelayCommand]
    private void ClearTaxRate() => SelectedTaxRate = null;
}

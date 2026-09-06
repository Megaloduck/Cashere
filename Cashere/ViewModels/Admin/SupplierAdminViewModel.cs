using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin;

public partial class SupplierAdminViewModel : ViewModelBase
{
    private readonly ISupplierAdminService _supplierAdmin;
    private int? _editingSupplierId;

    public ObservableCollection<Supplier> Suppliers { get; } = new();

    [ObservableProperty] private bool _isEditorOpen;
    [ObservableProperty] private string _editorTitle = "NEW SUPPLIER";
    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private string _formName = string.Empty;
    [ObservableProperty] private string _formContactPerson = string.Empty;
    [ObservableProperty] private string _formPhone = string.Empty;
    [ObservableProperty] private string _formAddress = string.Empty;
    [ObservableProperty] private string _formNotes = string.Empty;

    public SupplierAdminViewModel(ISupplierAdminService supplierAdmin)
    {
        _supplierAdmin = supplierAdmin;
    }

    public async Task LoadAsync()
    {
        var suppliers = await _supplierAdmin.GetAllSuppliersAsync();
        Suppliers.Clear();
        foreach (var supplier in suppliers) Suppliers.Add(supplier);
    }

    [RelayCommand]
    private void AddNew()
    {
        _editingSupplierId = null;
        EditorTitle = "NEW SUPPLIER";
        FormName = string.Empty;
        FormContactPerson = string.Empty;
        FormPhone = string.Empty;
        FormAddress = string.Empty;
        FormNotes = string.Empty;
        ErrorMessage = null;
        IsEditorOpen = true;
    }

    [RelayCommand]
    private void EditSupplier(Supplier? supplier)
    {
        if (supplier is null) return;

        _editingSupplierId = supplier.Id;
        EditorTitle = "EDIT SUPPLIER";
        FormName = supplier.Name;
        FormContactPerson = supplier.ContactPerson ?? string.Empty;
        FormPhone = supplier.Phone ?? string.Empty;
        FormAddress = supplier.Address ?? string.Empty;
        FormNotes = supplier.Notes ?? string.Empty;
        ErrorMessage = null;
        IsEditorOpen = true;
    }

    [RelayCommand]
    private async Task DeleteSupplier(Supplier? supplier)
    {
        if (supplier is null) return;

        try
        {
            await _supplierAdmin.DeleteSupplierAsync(supplier.Id);
            await LoadAsync();
        }
        catch (AdminValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(FormName))
        {
            ErrorMessage = "Supplier name is required.";
            return;
        }

        var input = new SupplierInput(
            FormName,
            string.IsNullOrWhiteSpace(FormContactPerson) ? null : FormContactPerson,
            string.IsNullOrWhiteSpace(FormPhone) ? null : FormPhone,
            string.IsNullOrWhiteSpace(FormAddress) ? null : FormAddress,
            string.IsNullOrWhiteSpace(FormNotes) ? null : FormNotes);

        try
        {
            if (_editingSupplierId is int id)
            {
                await _supplierAdmin.UpdateSupplierAsync(id, input);
            }
            else
            {
                await _supplierAdmin.CreateSupplierAsync(input);
            }

            IsEditorOpen = false;
            await LoadAsync();
        }
        catch (AdminValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void CancelEdit() => IsEditorOpen = false;
}
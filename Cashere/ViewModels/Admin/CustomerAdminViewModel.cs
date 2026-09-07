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

public partial class CustomerAdminViewModel : ViewModelBase
{
    private readonly ICustomerAdminService _customerAdmin;
    private List<Customer> _allCustomers = new();
    private int? _editingCustomerId;

    public ObservableCollection<Customer> FilteredCustomers { get; } = new();

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isEditorOpen;
    [ObservableProperty] private string _editorTitle = "NEW CUSTOMER";
    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private string _formName = string.Empty;
    [ObservableProperty] private string _formPhone = string.Empty;
    [ObservableProperty] private string _formEmail = string.Empty;
    [ObservableProperty] private string _formAddress = string.Empty;
    [ObservableProperty] private string _formNotes = string.Empty;

    public CustomerAdminViewModel(ICustomerAdminService customerAdmin)
    {
        _customerAdmin = customerAdmin;
    }

    public async Task LoadAsync()
    {
        _allCustomers = await _customerAdmin.GetAllCustomersAsync();
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<Customer> query = _allCustomers;
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(c =>
                c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (c.Phone is not null && c.Phone.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (c.Email is not null && c.Email.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        FilteredCustomers.Clear();
        foreach (var customer in query) FilteredCustomers.Add(customer);
    }

    [RelayCommand]
    private void AddNew()
    {
        _editingCustomerId = null;
        EditorTitle = "NEW CUSTOMER";
        FormName = string.Empty;
        FormPhone = string.Empty;
        FormEmail = string.Empty;
        FormAddress = string.Empty;
        FormNotes = string.Empty;
        ErrorMessage = null;
        IsEditorOpen = true;
    }

    [RelayCommand]
    private void EditCustomer(Customer? customer)
    {
        if (customer is null) return;

        _editingCustomerId = customer.Id;
        EditorTitle = "EDIT CUSTOMER";
        FormName = customer.Name;
        FormPhone = customer.Phone ?? string.Empty;
        FormEmail = customer.Email ?? string.Empty;
        FormAddress = customer.Address ?? string.Empty;
        FormNotes = customer.Notes ?? string.Empty;
        ErrorMessage = null;
        IsEditorOpen = true;
    }

    [RelayCommand]
    private async Task DeleteCustomer(Customer? customer)
    {
        if (customer is null) return;

        try
        {
            await _customerAdmin.DeleteCustomerAsync(customer.Id);
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
            ErrorMessage = "Customer name is required.";
            return;
        }

        var input = new CustomerInput(
            FormName,
            string.IsNullOrWhiteSpace(FormPhone) ? null : FormPhone,
            string.IsNullOrWhiteSpace(FormEmail) ? null : FormEmail,
            string.IsNullOrWhiteSpace(FormAddress) ? null : FormAddress,
            string.IsNullOrWhiteSpace(FormNotes) ? null : FormNotes);

        try
        {
            if (_editingCustomerId is int id)
            {
                await _customerAdmin.UpdateCustomerAsync(id, input);
            }
            else
            {
                await _customerAdmin.CreateCustomerAsync(input);
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
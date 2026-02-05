using StockApplication.Models;
using StockApplication.Repositories;

namespace StockApplication.Views;

[QueryProperty(nameof(SupplierId), "id")]
public partial class SupplierEditPage : ContentPage
{
    private readonly Repository<Supplier> _supplierRepository;
    private int _supplierId;
    private bool _isNewSupplier = true;

    public int SupplierId
    {
        get => _supplierId;
        set
        {
            _supplierId = value;
            LoadSupplier();
        }
    }

    public SupplierEditPage(Repository<Supplier> supplierRepository)
    {
        InitializeComponent();
        
        _supplierRepository = supplierRepository;
        
        // Set up event handlers
        SaveButton.Clicked += OnSaveClicked;
        CancelButton.Clicked += OnCancelClicked;
    }

    private async void LoadSupplier()
    {
        if (_supplierId <= 0)
        {
            // New supplier
            _isNewSupplier = true;
            PageTitleLabel.Text = "Add New Supplier";
            Title = "Add Supplier";
            return;
        }
        
        try
        {
            // Existing supplier
            _isNewSupplier = false;
            PageTitleLabel.Text = "Edit Supplier";
            Title = "Edit Supplier";
            
            var supplier = await _supplierRepository.GetByIdAsync(_supplierId);
            if (supplier != null)
            {
                // Populate the form
                SupplierNameEntry.Text = supplier.Sup_name;
                PhoneEntry.Text = supplier.Sup_Phone;
                EmailEntry.Text = supplier.Sup_Email;
                AddressEditor.Text = supplier.Sup_Address;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load supplier: {ex.Message}", "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(SupplierNameEntry.Text))
        {
            await DisplayAlert("Validation Error", "Supplier name is required.", "OK");
            return;
        }
        
        try
        {
            // Create supplier object
            var supplier = new Supplier
            {
                SupplierID = _supplierId,
                Sup_name = SupplierNameEntry.Text,
                Sup_Phone = PhoneEntry.Text,
                Sup_Email = EmailEntry.Text,
                Sup_Address = AddressEditor.Text
            };
            
            if (_isNewSupplier)
            {
                // Insert new supplier
                await _supplierRepository.InsertAsync(supplier);
                await DisplayAlert("Success", "Supplier added successfully.", "OK");
            }
            else
            {
                // Update existing supplier
                await _supplierRepository.UpdateAsync(supplier);
                await DisplayAlert("Success", "Supplier updated successfully.", "OK");
            }
            
            // Navigate back
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save supplier: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        // Confirm cancel if there are changes
        bool hasChanges = !string.IsNullOrEmpty(SupplierNameEntry.Text) || 
                         !string.IsNullOrEmpty(PhoneEntry.Text) ||
                         !string.IsNullOrEmpty(EmailEntry.Text) ||
                         !string.IsNullOrEmpty(AddressEditor.Text);
        
        if (hasChanges)
        {
            bool cancel = await DisplayAlert("Confirm", 
                "Are you sure you want to cancel? Any unsaved changes will be lost.", 
                "Yes", "No");
            
            if (!cancel)
                return;
        }
        
        // Navigate back
        await Shell.Current.GoToAsync("..");
    }
}
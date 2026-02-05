using StockApplication.Models;
using StockApplication.Repositories;
using System.Collections.ObjectModel;

namespace StockApplication.Views;

public partial class SupplierListPage : ContentPage
{
    private readonly Repository<Supplier> _supplierRepository;
    private ObservableCollection<Supplier> _suppliers = new();

    public SupplierListPage(Repository<Supplier> supplierRepository)
    {
        InitializeComponent();
        
        _supplierRepository = supplierRepository;
        
        // Set up the collection view
        SupplierCollectionView.ItemsSource = _suppliers;
        
        // Set up refresh view
        SupplierRefreshView.Command = new Command(async () => await LoadSuppliersAsync());
        
        // Set up event handlers
        AddSupplierButton.Clicked += OnAddSupplierClicked;
        SearchEntry.TextChanged += OnSearchTextChanged;
        SupplierCollectionView.SelectionChanged += OnSupplierSelectionChanged;
        
        // Find and set up Edit and Delete button event handlers
        var editButtons = SupplierCollectionView.GetVisualTreeDescendants().OfType<Button>().Where(b => b.Text == "Edit");
        foreach (var button in editButtons)
        {
            button.Clicked += OnEditSupplierClicked;
        }
        
        var deleteButtons = SupplierCollectionView.GetVisualTreeDescendants().OfType<Button>().Where(b => b.Text == "Delete");
        foreach (var button in deleteButtons)
        {
            button.Clicked += OnDeleteSupplierClicked;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Load suppliers
        await LoadSuppliersAsync();
    }

    private async Task LoadSuppliersAsync()
    {
        try
        {
            SupplierRefreshView.IsRefreshing = true;
            
            // Get all suppliers
            var suppliers = await _supplierRepository.GetAllAsync();
            
            // Update collection
            _suppliers.Clear();
            foreach (var supplier in suppliers)
            {
                _suppliers.Add(supplier);
            }
            
            // Update total count
            TotalItemsLabel.Text = $"Total Suppliers: {_suppliers.Count}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load suppliers: {ex.Message}", "OK");
        }
        finally
        {
            SupplierRefreshView.IsRefreshing = false;
        }
    }

    private async void OnAddSupplierClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("Suppliers/Add");
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            await LoadSuppliersAsync();
            return;
        }
        
        var searchTerm = e.NewTextValue.ToLowerInvariant();
        var allSuppliers = await _supplierRepository.GetAllAsync();
        
        var filteredSuppliers = allSuppliers.Where(s => 
            s.Sup_name.ToLowerInvariant().Contains(searchTerm) ||
            (s.Sup_Email != null && s.Sup_Email.ToLowerInvariant().Contains(searchTerm)) ||
            (s.Sup_Phone != null && s.Sup_Phone.ToLowerInvariant().Contains(searchTerm)));
        
        _suppliers.Clear();
        foreach (var supplier in filteredSuppliers)
        {
            _suppliers.Add(supplier);
        }
        
        TotalItemsLabel.Text = $"Search Results: {_suppliers.Count}";
    }

    private async void OnSupplierSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Supplier selectedSupplier)
        {
            // Navigate to edit page with the selected supplier ID
            SupplierCollectionView.SelectedItem = null;
            await Shell.Current.GoToAsync($"Suppliers/Edit?id={selectedSupplier.SupplierID}");
        }
    }
    
    // New method for Edit button
    private async void OnEditSupplierClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Supplier supplier)
        {
            // Navigate to edit page with the selected supplier ID
            await Shell.Current.GoToAsync($"Suppliers/Edit?id={supplier.SupplierID}");
        }
    }
    
    // New method for Delete button
    private async void OnDeleteSupplierClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Supplier supplier)
        {
            // Confirm deletion
            bool confirm = await DisplayAlert("Delete Supplier", 
                $"Are you sure you want to delete {supplier.Sup_name}?", 
                "Yes", "No");
                
            if (confirm)
            {
                try
                {
                    // Delete the supplier - corrected to pass the supplier object
                    await _supplierRepository.DeleteAsync(supplier);
                    
                    // Refresh the list
                    await LoadSuppliersAsync();
                    
                    // Show success message
                    await DisplayAlert("Success", "Supplier deleted successfully.", "OK");
                }
                catch (Exception ex)
                {
                    // Show error message
                    await DisplayAlert("Error", $"Failed to delete supplier: {ex.Message}", "OK");
                }
            }
        }
    }
}
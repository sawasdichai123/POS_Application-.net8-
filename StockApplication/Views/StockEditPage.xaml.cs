using StockApplication.Models;
using StockApplication.Repositories;
using System.Collections.ObjectModel;

namespace StockApplication.Views;

[QueryProperty(nameof(StockId), "id")]
public partial class StockEditPage : ContentPage
{
    private readonly StockRepository _stockRepository;
    private readonly Repository<Supplier> _supplierRepository;
    private ObservableCollection<Supplier> _suppliers = new();
    private int _stockId;
    private bool _isNewStock = true;

    public int StockId
    {
        get => _stockId;
        set
        {
            _stockId = value;
            // Don't call LoadStock here to avoid race conditions with OnAppearing
            _isNewStock = value <= 0;
        }
    }

    public StockEditPage(StockRepository stockRepository, Repository<Supplier> supplierRepository)
    {
        InitializeComponent();
        
        _stockRepository = stockRepository;
        _supplierRepository = supplierRepository;
        
        // Set suppliers as source for the picker
        SupplierPicker.ItemsSource = _suppliers;
        
        // Set up event handlers
        SaveButton.Clicked += OnSaveClicked;
        CancelButton.Clicked += OnCancelClicked;
        AddSupplierButton.Clicked += OnAddSupplierClicked;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        try
        {
            // Load suppliers first
            await LoadSuppliersAsync();
            
            // Set UI for new or edit mode
            if (_isNewStock)
            {
                // New stock item
                PageTitleLabel.Text = "Add New Stock Item";
                Title = "Add Stock";
                ExpiryDatePicker.Date = DateTime.Now.AddMonths(6); // Default expiry
                // Clear form
                StockNameEntry.Text = string.Empty;
                TypeEntry.Text = string.Empty;
                QuantityEntry.Text = string.Empty;
                UnitPicker.SelectedIndex = -1;
                SupplierPicker.SelectedIndex = -1;
            }
            else
            {
                // Existing stock - load it
                PageTitleLabel.Text = "Edit Stock Item";
                Title = "Edit Stock";
                await LoadStockSafely();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error initializing page: {ex.Message}", "OK");
        }
    }

    private async Task LoadSuppliersAsync()
    {
        try
        {
            var suppliers = await _supplierRepository.GetAllAsync();
            
            _suppliers.Clear();
            foreach (var supplier in suppliers)
            {
                _suppliers.Add(supplier);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load suppliers: {ex.Message}", "OK");
        }
    }

    private async Task LoadStockSafely()
    {
        try
        {
            // Get all stocks and find the one we want
            var allStocks = await _stockRepository.GetAllStocksAsync();
            var stock = allStocks.FirstOrDefault(s => s.StockID == _stockId);
            
            if (stock == null)
            {
                await DisplayAlert("Error", "Stock item not found.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }
            
            // Populate the form
            StockNameEntry.Text = stock.Stock_Name;
            TypeEntry.Text = stock.Type;
            QuantityEntry.Text = stock.Quantity.ToString();
            
            if (!string.IsNullOrEmpty(stock.Unit))
            {
                var unitItems = UnitPicker.ItemsSource as IList<string>;
                if (unitItems != null)
                {
                    int unitIndex = unitItems.IndexOf(stock.Unit);
                    if (unitIndex >= 0)
                    {
                        UnitPicker.SelectedIndex = unitIndex;
                    }
                }
            }
            
            if (stock.ExpiryDate.HasValue)
            {
                ExpiryDatePicker.Date = stock.ExpiryDate.Value;
            }
            
            if (stock.SupplierID.HasValue && _suppliers.Count > 0)
            {
                int supplierIndex = -1;
                for (int i = 0; i < _suppliers.Count; i++)
                {
                    if (_suppliers[i].SupplierID == stock.SupplierID.Value)
                    {
                        supplierIndex = i;
                        break;
                    }
                }
                
                if (supplierIndex >= 0)
                {
                    SupplierPicker.SelectedIndex = supplierIndex;
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load stock item: {ex.Message}", "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(StockNameEntry.Text))
        {
            await DisplayAlert("Validation Error", "Stock name is required.", "OK");
            return;
        }
        
        if (!double.TryParse(QuantityEntry.Text, out double quantity))
        {
            await DisplayAlert("Validation Error", "Please enter a valid quantity.", "OK");
            return;
        }
        
        try
        {
            // Create stock object
            var stock = new Stock
            {
                StockID = _isNewStock ? 0 : _stockId, // Use 0 for new items to let SQLite assign an ID
                Stock_Name = StockNameEntry.Text,
                Type = TypeEntry.Text,
                Quantity = quantity,
                Unit = UnitPicker.SelectedItem?.ToString(),
                ExpiryDate = ExpiryDatePicker.Date,
                UpdatedAt = DateTime.Now
            };
            
            // Set supplier if selected
            if (SupplierPicker.SelectedIndex >= 0 && SupplierPicker.SelectedIndex < _suppliers.Count)
            {
                stock.SupplierID = _suppliers[SupplierPicker.SelectedIndex].SupplierID;
            }
            
            if (_isNewStock)
            {
                // Insert new stock
                await _stockRepository.InsertAsync(stock);
                await DisplayAlert("Success", "Stock item added successfully.", "OK");
            }
            else
            {
                // Update existing stock
                await _stockRepository.UpdateAsync(stock);
                await DisplayAlert("Success", "Stock item updated successfully.", "OK");
            }
            
            // Navigate back
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save stock item: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        // Confirm cancel if there are changes
        bool hasChanges = !string.IsNullOrEmpty(StockNameEntry.Text) || 
                         !string.IsNullOrEmpty(TypeEntry.Text) ||
                         !string.IsNullOrEmpty(QuantityEntry.Text);
        
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

    private async void OnAddSupplierClicked(object sender, EventArgs e)
    {
        // Navigate to add supplier page
        await Shell.Current.GoToAsync("Suppliers/Add");
    }
}
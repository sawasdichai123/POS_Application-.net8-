using StockApplication.Models;
using StockApplication.Repositories;
using StockApplication.Services;
using System.Collections.ObjectModel;

namespace StockApplication.Views;

public partial class StockListPage : ContentPage
{
    private readonly StockRepository _stockRepository;
    private readonly Repository<Supplier> _supplierRepository;
    private ObservableCollection<StockItemViewModel> _stockItems = new();
    private List<Supplier> _suppliers;

    public StockListPage(StockRepository stockRepository, Repository<Supplier> supplierRepository)
    {
        InitializeComponent();
        
        _stockRepository = stockRepository;
        _supplierRepository = supplierRepository;
        
        // Set up the collection view
        StockCollectionView.ItemsSource = _stockItems;
        
        // Set up refresh view
        StockRefreshView.Command = new Command(async () => await LoadStockItemsAsync());
        
        // Set up event handlers
        AddStockButton.Clicked += OnAddStockClicked;
        FilterButton.Clicked += OnFilterClicked;
        SearchEntry.TextChanged += OnSearchTextChanged;
        StockCollectionView.SelectionChanged += OnStockSelectionChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Load suppliers once for reference
        _suppliers = await _supplierRepository.GetAllAsync();
        
        // Load stock items
        await LoadStockItemsAsync();
    }

    private async Task LoadStockItemsAsync()
    {
        try
        {
            StockRefreshView.IsRefreshing = true;
            
            // Get all stock items
            var stocks = await _stockRepository.GetAllAsync();
            
            // Convert to view models
            _stockItems.Clear();
            foreach (var stock in stocks)
            {
                var supplier = _suppliers.FirstOrDefault(s => s.SupplierID == stock.SupplierID);
                
                _stockItems.Add(new StockItemViewModel
                {
                    StockID = stock.StockID,
                    Stock_Name = stock.Stock_Name,
                    Type = stock.Type,
                    Quantity = stock.Quantity,
                    Unit = stock.Unit,
                    ExpiryDate = stock.ExpiryDate,
                    SupplierID = stock.SupplierID,
                    SupplierName = supplier?.Sup_name ?? "Unknown",
                    UpdatedAt = stock.UpdatedAt
                });
            }
            
            // Update total items count
            TotalItemsLabel.Text = $"Total Items: {_stockItems.Count}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load stock items: {ex.Message}", "OK");
        }
        finally
        {
            StockRefreshView.IsRefreshing = false;
        }
    }

    private async void OnAddStockClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("Stock/Add");
    }

    private async void OnEditButtonClicked(object sender, EventArgs e)
    {
        // Get the associated stock item from the button's command parameter
        var button = sender as Button;
        var stockItem = button.CommandParameter as StockItemViewModel;
        
        if (stockItem != null)
        {
            // Navigate to edit page
            await Shell.Current.GoToAsync($"Stock/Edit?id={stockItem.StockID}");
        }
    }

    private async void OnDeleteButtonClicked(object sender, EventArgs e)
    {
        // Get the associated stock item from the button's command parameter
        var button = sender as Button;
        var stockItem = button.CommandParameter as StockItemViewModel;
        
        if (stockItem != null)
        {
            bool confirm = await DisplayAlert("Confirm Delete", 
                $"Are you sure you want to delete {stockItem.Stock_Name}?", "Yes", "No");
            
            if (confirm)
            {
                try
                {
                    // Use DeleteByIdAsync instead of DeleteAsync
                    await _stockRepository.DeleteByIdAsync(stockItem.StockID);
                    
                    // Refresh the list
                    await LoadStockItemsAsync();
                    
                    await DisplayAlert("Success", "Item deleted successfully", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to delete item: {ex.Message}", "OK");
                }
            }
        }
    }

    private async void OnFilterClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet("Filter By", "Cancel", null, 
            "All Items", "Low Stock", "Expiring Soon", "Recently Updated");
        
        switch (action)
        {
            case "All Items":
                await LoadStockItemsAsync();
                break;
            
            case "Low Stock":
                await LoadLowStockItemsAsync();
                break;
            
            case "Expiring Soon":
                await LoadExpiringItemsAsync();
                break;
            
            case "Recently Updated":
                await LoadRecentlyUpdatedItemsAsync();
                break;
        }
    }

    private async Task LoadLowStockItemsAsync()
    {
        try
        {
            StockRefreshView.IsRefreshing = true;
            
            var lowStocks = await _stockRepository.GetLowStockAsync(10);
            
            _stockItems.Clear();
            foreach (var stock in lowStocks)
            {
                var supplier = _suppliers.FirstOrDefault(s => s.SupplierID == stock.SupplierID);
                
                _stockItems.Add(new StockItemViewModel
                {
                    StockID = stock.StockID,
                    Stock_Name = stock.Stock_Name,
                    Type = stock.Type,
                    Quantity = stock.Quantity,
                    Unit = stock.Unit,
                    ExpiryDate = stock.ExpiryDate,
                    SupplierID = stock.SupplierID,
                    SupplierName = supplier?.Sup_name ?? "Unknown",
                    UpdatedAt = stock.UpdatedAt
                });
            }
            
            TotalItemsLabel.Text = $"Low Stock Items: {_stockItems.Count}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load low stock items: {ex.Message}", "OK");
        }
        finally
        {
            StockRefreshView.IsRefreshing = false;
        }
    }

    private async Task LoadExpiringItemsAsync()
    {
        try
        {
            StockRefreshView.IsRefreshing = true;
            
            var expiringStocks = await _stockRepository.GetNearExpiryAsync(30);
            
            _stockItems.Clear();
            foreach (var stock in expiringStocks)
            {
                var supplier = _suppliers.FirstOrDefault(s => s.SupplierID == stock.SupplierID);
                
                _stockItems.Add(new StockItemViewModel
                {
                    StockID = stock.StockID,
                    Stock_Name = stock.Stock_Name,
                    Type = stock.Type,
                    Quantity = stock.Quantity,
                    Unit = stock.Unit,
                    ExpiryDate = stock.ExpiryDate,
                    SupplierID = stock.SupplierID,
                    SupplierName = supplier?.Sup_name ?? "Unknown",
                    UpdatedAt = stock.UpdatedAt
                });
            }
            
            TotalItemsLabel.Text = $"Expiring Items: {_stockItems.Count}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load expiring items: {ex.Message}", "OK");
        }
        finally
        {
            StockRefreshView.IsRefreshing = false;
        }
    }

    private async Task LoadRecentlyUpdatedItemsAsync()
    {
        try
        {
            StockRefreshView.IsRefreshing = true;
            
            var recentStocks = await _stockRepository.FindAsync(s => s.UpdatedAt > DateTime.Now.AddDays(-7));
            
            _stockItems.Clear();
            foreach (var stock in recentStocks.OrderByDescending(s => s.UpdatedAt))
            {
                var supplier = _suppliers.FirstOrDefault(s => s.SupplierID == stock.SupplierID);
                
                _stockItems.Add(new StockItemViewModel
                {
                    StockID = stock.StockID,
                    Stock_Name = stock.Stock_Name,
                    Type = stock.Type,
                    Quantity = stock.Quantity,
                    Unit = stock.Unit,
                    ExpiryDate = stock.ExpiryDate,
                    SupplierID = stock.SupplierID,
                    SupplierName = supplier?.Sup_name ?? "Unknown",
                    UpdatedAt = stock.UpdatedAt
                });
            }
            
            TotalItemsLabel.Text = $"Recently Updated: {_stockItems.Count}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load recently updated items: {ex.Message}", "OK");
        }
        finally
        {
            StockRefreshView.IsRefreshing = false;
        }
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            await LoadStockItemsAsync();
            return;
        }
        
        var searchTerm = e.NewTextValue.ToLowerInvariant();
        var allStocks = await _stockRepository.GetAllAsync();
        
        var filteredStocks = allStocks.Where(s => 
            s.Stock_Name.ToLowerInvariant().Contains(searchTerm) ||
            (s.Type != null && s.Type.ToLowerInvariant().Contains(searchTerm)));
        
        _stockItems.Clear();
        foreach (var stock in filteredStocks)
        {
            var supplier = _suppliers.FirstOrDefault(s => s.SupplierID == stock.SupplierID);
            
            _stockItems.Add(new StockItemViewModel
            {
                StockID = stock.StockID,
                Stock_Name = stock.Stock_Name,
                Type = stock.Type,
                Quantity = stock.Quantity,
                Unit = stock.Unit,
                ExpiryDate = stock.ExpiryDate,
                SupplierID = stock.SupplierID,
                SupplierName = supplier?.Sup_name ?? "Unknown",
                UpdatedAt = stock.UpdatedAt
            });
        }
        
        TotalItemsLabel.Text = $"Search Results: {_stockItems.Count}";
    }

    private async void OnStockSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is StockItemViewModel selectedStock)
        {
            // Navigate to edit page with the selected stock ID
            StockCollectionView.SelectedItem = null;
            await Shell.Current.GoToAsync($"Stock/Edit?id={selectedStock.StockID}");
        }
    }
}

// View model for displaying stock items with additional properties
public class StockItemViewModel
{
    public int StockID { get; set; }
    public string Stock_Name { get; set; }
    public string Type { get; set; }
    public double Quantity { get; set; }
    public string Unit { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? SupplierID { get; set; }
    public string SupplierName { get; set; }
    public DateTime UpdatedAt { get; set; }
}
using StockApplication.Models;
using StockApplication.Repositories;
using StockApplication.ViewModels;
using System.Collections.ObjectModel;

namespace StockApplication.Views;

public partial class DashboardPage : ContentPage
{
    private readonly StockRepository _stockRepository;
    private readonly Repository<Supplier> _supplierRepository;
    private readonly Repository<Order> _orderRepository;
    
    // Collections for the dashboard
    private ObservableCollection<Stock> _recentUpdates = new();
    private ObservableCollection<Stock> _expiringItems = new();

    public DashboardPage(StockRepository stockRepository, 
                         Repository<Supplier> supplierRepository,
                         Repository<Order> orderRepository)
    {
        InitializeComponent();
        
        _stockRepository = stockRepository;
        _supplierRepository = supplierRepository;
        _orderRepository = orderRepository;
        
        // Set collections as source for collection views
        RecentUpdatesCollection.ItemsSource = _recentUpdates;
        ExpiringItemsCollection.ItemsSource = _expiringItems;
        
        // Set up event handlers
        AddStockButton.Clicked += OnAddStockClicked;
        AddSupplierButton.Clicked += OnAddSupplierClicked;
        CreateOrderButton.Clicked += OnCreateOrderClicked;
        ViewReportsButton.Clicked += OnViewReportsClicked;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDashboardDataAsync();
    }

    private async Task LoadDashboardDataAsync()
    {
        try
        {
            // Load total stock count
            var stockCount = await _stockRepository.CountAsync();
            TotalStockLabel.Text = stockCount.ToString();
            
            // Load low stock items count
            var lowStockItems = await _stockRepository.GetLowStockAsync(10);
            LowStockLabel.Text = lowStockItems.Count.ToString();
            
            // With our modified FindAsync method, we can safely use the LINQ expression
            // It will now properly handle the DateTime.AddDays(-7) by filtering in memory
            var recentStocks = await _stockRepository.FindAsync(s => s.UpdatedAt > DateTime.Now.AddDays(-7));
                
            _recentUpdates.Clear();
            foreach (var stock in recentStocks)
            {
                // Add an UpdateMessage property for display
                stock.GetType().GetProperty("UpdateMessage")?.SetValue(stock, 
                    $"Updated quantity: {stock.Quantity} {stock.Unit}");
                _recentUpdates.Add(stock);
            }
            
            // Load expiring items
            var expiringStocks = await _stockRepository.GetNearExpiryAsync(30);
            _expiringItems.Clear();
            foreach (var stock in expiringStocks.OrderBy(s => s.ExpiryDate).Take(5))
            {
                _expiringItems.Add(stock);
            }
            
            // Update status label
            StatusLabel.Text = $"Last updated: {DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load dashboard data: {ex.Message}", "OK");
        }
    }
    
    private async void OnAddStockClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("Stock/Add");
    }
    
    private async void OnAddSupplierClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("Suppliers/Add");
    }
    
    private async void OnCreateOrderClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("Orders/Add");
    }
    
    private async void OnViewReportsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("Reports");
    }
}
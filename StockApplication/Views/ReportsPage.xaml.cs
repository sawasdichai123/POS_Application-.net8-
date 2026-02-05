using StockApplication.Repositories;
using StockApplication.Models;
using StockApplication.Views.Reports;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using System.IO;

namespace StockApplication.Views;

public partial class ReportsPage : ContentPage
{
    private readonly StockRepository _stockRepository;
    private readonly Repository<Order> _orderRepository;
    private readonly Repository<OrderDetail> _orderDetailRepository;
    private readonly Repository<MenuItem> _menuItemRepository;
    private readonly Repository<Customer> _customerRepository;
    private readonly Repository<Supplier> _supplierRepository;
    
    public ReportsPage(
        StockRepository stockRepository,
        Repository<Order> orderRepository,
        Repository<OrderDetail> orderDetailRepository,
        Repository<MenuItem> menuItemRepository,
        Repository<Customer> customerRepository,
        Repository<Supplier> supplierRepository)
    {
        InitializeComponent();
        
        _stockRepository = stockRepository;
        _orderRepository = orderRepository;
        _orderDetailRepository = orderDetailRepository;
        _menuItemRepository = menuItemRepository;
        _customerRepository = customerRepository;
        _supplierRepository = supplierRepository;
        
        // Set up event handlers
        InventorySummaryButton.Clicked += OnInventorySummaryClicked;
        LowStockButton.Clicked += OnLowStockClicked;
        ExpiryReportButton.Clicked += OnExpiryReportClicked;
        SalesSummaryButton.Clicked += OnSalesSummaryClicked;
        PopularItemsButton.Clicked += OnPopularItemsClicked;
        ExportCsvButton.Clicked += OnExportCsvClicked;
        ExportPdfButton.Clicked += OnExportPdfClicked;
    }
    
    private async void OnInventorySummaryClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new InventorySummaryReportPage(_stockRepository, _supplierRepository));
    }
    
    private async void OnLowStockClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LowStockReportPage(_stockRepository, _supplierRepository));
    }
    
    private async void OnExpiryReportClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ExpiryReportPage(_stockRepository));
    }
    
    private async void OnSalesSummaryClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SalesSummaryReportPage(_orderRepository, _customerRepository));
    }
    
    private async void OnPopularItemsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new PopularItemsReportPage(_orderDetailRepository, _menuItemRepository, _orderRepository));
    }
    
    private async void OnExportCsvClicked(object sender, EventArgs e)
    {
        // Show options for which report to export
        string action = await DisplayActionSheet(
            "Export Which Report?", 
            "Cancel", 
            null, 
            "Inventory Summary", 
            "Low Stock", 
            "Expiry Report", 
            "Sales Summary", 
            "Popular Items");
            
        if (action == "Cancel" || string.IsNullOrEmpty(action))
            return;
            
        // Navigate to the appropriate report page
        switch (action)
        {
            case "Inventory Summary":
                await Navigation.PushAsync(new InventorySummaryReportPage(_stockRepository, _supplierRepository));
                break;
                
            case "Low Stock":
                await Navigation.PushAsync(new LowStockReportPage(_stockRepository, _supplierRepository));
                break;
                
            case "Expiry Report":
                await Navigation.PushAsync(new ExpiryReportPage(_stockRepository));
                break;
                
            case "Sales Summary":
                await Navigation.PushAsync(new SalesSummaryReportPage(_orderRepository, _customerRepository));
                break;
                
            case "Popular Items":
                await Navigation.PushAsync(new PopularItemsReportPage(_orderDetailRepository, _menuItemRepository, _orderRepository));
                break;
        }
    }
    
    private async void OnExportPdfClicked(object sender, EventArgs e)
    {
        // Show options for which report to export as PDF
        string action = await DisplayActionSheet(
            "Export Which Report as PDF?", 
            "Cancel", 
            null, 
            "Inventory Summary", 
            "Low Stock", 
            "Expiry Report", 
            "Sales Summary", 
            "Popular Items");
            
        if (action == "Cancel" || string.IsNullOrEmpty(action))
            return;
            
        // Navigate to the appropriate report page
        switch (action)
        {
            case "Inventory Summary":
                var inventoryPage = new InventorySummaryReportPage(_stockRepository, _supplierRepository);
                await Navigation.PushAsync(inventoryPage);
                break;
                
            case "Low Stock":
                var lowStockPage = new LowStockReportPage(_stockRepository, _supplierRepository);
                await Navigation.PushAsync(lowStockPage);
                break;
                
            case "Expiry Report":
                var expiryPage = new ExpiryReportPage(_stockRepository);
                await Navigation.PushAsync(expiryPage);
                break;
                
            case "Sales Summary":
                var salesPage = new SalesSummaryReportPage(_orderRepository, _customerRepository);
                await Navigation.PushAsync(salesPage);
                break;
                
            case "Popular Items":
                var popularItemsPage = new PopularItemsReportPage(_orderDetailRepository, _menuItemRepository, _orderRepository);
                await Navigation.PushAsync(popularItemsPage);
                break;
        }
    }
}
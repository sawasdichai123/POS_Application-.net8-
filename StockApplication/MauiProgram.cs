using Microsoft.Extensions.Logging;
using StockApplication.Models;
using StockApplication.Repositories;
using StockApplication.Services;
using StockApplication.Views;
using StockApplication.ViewModels;
using CommunityToolkit.Maui;
using StockApplication.Views.Reports;

namespace StockApplication;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit() // This must be directly chained after UseMauiApp
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register Services
        builder.Services.AddSingleton<DatabaseService>();
        
        // Register Repositories
        builder.Services.AddSingleton<Repository<Supplier>>();
        builder.Services.AddSingleton<StockRepository>();
        builder.Services.AddSingleton<Repository<MenuItem>>();
        builder.Services.AddSingleton<Repository<Customer>>();
        builder.Services.AddSingleton<Repository<Staff>>();
        builder.Services.AddSingleton<Repository<Order>>();
        builder.Services.AddSingleton<Repository<OrderDetail>>();
        builder.Services.AddSingleton<Repository<Payment>>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<UserRepository>();
        
        // Register ViewModels
        builder.Services.AddSingleton<StockListViewModel>();
        builder.Services.AddTransient<StockEditViewModel>();
        
        // Register Pages
        builder.Services.AddSingleton<DashboardPage>();
        
        // Stock Pages
        builder.Services.AddSingleton<StockListPage>();
        builder.Services.AddTransient<StockEditPage>();
        
        // Supplier Pages
        builder.Services.AddSingleton<SupplierListPage>();
        builder.Services.AddTransient<SupplierEditPage>();
        
        // Order Pages
        builder.Services.AddSingleton<OrderListPage>();
        builder.Services.AddTransient<OrderEditPage>();
        
        // Customer Pages
        builder.Services.AddSingleton<CustomerListPage>();
        builder.Services.AddTransient<CustomerEditPage>();
        builder.Services.AddTransient<CustomerOrdersPage>();
        
        // Staff Pages
        builder.Services.AddSingleton<StaffListPage>();
        builder.Services.AddTransient<StaffEditPage>();
        
        // Menu Item Pages
        builder.Services.AddSingleton<MenuItemListPage>();
        builder.Services.AddTransient<MenuItemEditPage>();
        
        // Payment Pages
        builder.Services.AddSingleton<PaymentListPage>();
        builder.Services.AddTransient<PaymentEditPage>();
        
        // Reports Page
        builder.Services.AddSingleton<ReportsPage>();

        // Add this line to the MauiProgram.cs builder.Services.AddTransient section
        builder.Services.AddTransient<CheckoutPage>();  

        // Add this line to your MauiProgram.cs file where you register your pages
        
        builder.Services.AddTransient<OrderDetailsPage>();
        builder.Services.AddSingleton<Repository<Table>>();

        // Register Report Pages
        builder.Services.AddTransient<InventorySummaryReportPage>();
        builder.Services.AddTransient<LowStockReportPage>();
        builder.Services.AddTransient<ExpiryReportPage>();
        builder.Services.AddTransient<SalesSummaryReportPage>();
        builder.Services.AddTransient<PopularItemsReportPage>();

        // Register Report Pages
        builder.Services.AddTransient<StockApplication.Views.Reports.InventorySummaryReportPage>();
        builder.Services.AddTransient<StockApplication.Views.Reports.LowStockReportPage>();
        builder.Services.AddTransient<StockApplication.Views.Reports.ExpiryReportPage>();
        builder.Services.AddTransient<StockApplication.Views.Reports.SalesSummaryReportPage>();
        builder.Services.AddTransient<StockApplication.Views.Reports.PopularItemsReportPage>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<UserManagementPage>();
        builder.Services.AddTransient<UserEditPage>();
        builder.Services.AddTransient<ChangePasswordPage>();
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
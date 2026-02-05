using StockApplication.Services;
using StockApplication.Views;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace StockApplication
{
    public partial class AppShell : Shell
    {
        private readonly AuthService _authService;
        
        public Command LogoutCommand { get; }
        public Command ChangePasswordCommand { get; }
        
        public bool IsManager => _authService.IsManager;
        
        public AppShell(AuthService authService)
        {
            InitializeComponent();
            
            _authService = authService;
            
            // Register routes for navigation
            RegisterRoutes();
            
            // Set up commands
            LogoutCommand = new Command(Logout);
            ChangePasswordCommand = new Command(async () => await GoToChangePasswordAsync());
            
            // Set binding context to this for command binding
            BindingContext = this;
            
            // Apply role-based access control
            ApplyRoleBasedAccess();
            
            // Listen for logout events
            _authService.UserLoggedOut += OnUserLoggedOut;
        }
        
        private void RegisterRoutes()
        {
            // Stock routes
            Routing.RegisterRoute("Stock/Add", typeof(StockEditPage));
            Routing.RegisterRoute("Stock/Edit", typeof(StockEditPage));
            
            // Supplier routes
            Routing.RegisterRoute("Suppliers/Add", typeof(SupplierEditPage));
            Routing.RegisterRoute("Suppliers/Edit", typeof(SupplierEditPage));
            
            // Order routes
            Routing.RegisterRoute("Orders/Add", typeof(OrderEditPage));
            Routing.RegisterRoute("Orders/Edit", typeof(OrderEditPage));
            Routing.RegisterRoute("Orders/Details", typeof(OrderDetailsPage));
            
            // Customer routes
            Routing.RegisterRoute("Customers/Add", typeof(CustomerEditPage));
            Routing.RegisterRoute("Customers/Edit", typeof(CustomerEditPage));
            Routing.RegisterRoute("Customers/Orders", typeof(CustomerOrdersPage));
            
            // Staff routes
            Routing.RegisterRoute("Staff/Add", typeof(StaffEditPage));
            Routing.RegisterRoute("Staff/Edit", typeof(StaffEditPage));
            
            // Menu Item routes
            Routing.RegisterRoute("Menu/Add", typeof(MenuItemEditPage));
            Routing.RegisterRoute("Menu/Edit", typeof(MenuItemEditPage));
            
            // Payment routes
            Routing.RegisterRoute("Payments/Add", typeof(PaymentEditPage));
            
            // Report routes
            Routing.RegisterRoute("Reports", typeof(ReportsPage));
            
            // Checkout route
            Routing.RegisterRoute("Checkout", typeof(CheckoutPage));
            
            // Login route
            Routing.RegisterRoute("Login", typeof(LoginPage));
            
            // User management routes
            Routing.RegisterRoute("Users", typeof(UserManagementPage));
            Routing.RegisterRoute("Users/Edit", typeof(UserEditPage));
            Routing.RegisterRoute("Account/ChangePassword", typeof(ChangePasswordPage));

            // Add this in your AppShell constructor or initialization
            Routing.RegisterRoute("useredit", typeof(UserEditPage));
        }
        
        private void ApplyRoleBasedAccess()
        {
            bool isManager = _authService.IsManager;
            
            // Update binding for UI elements that depend on role
            OnPropertyChanged(nameof(IsManager));
            
            if (!isManager) // If user is staff (not manager)
            {
                // Get all FlyoutItems except the main tabs (top navigation)
                var restrictedItems = Items.Where(item => 
                    item is FlyoutItem flyout && 
                    flyout.Route != "main"
                ).ToList();
                
                // Remove all flyout menu items for staff users
                foreach (var item in restrictedItems)
                {
                    Items.Remove(item);
                }
            }
        }
        
        private Tab FindTabByRoute(string route)
        {
            foreach (var item in Items)
            {
                if (item is FlyoutItem flyoutItem)
                {
                    foreach (var tab in flyoutItem.Items)
                    {
                        if (tab is Tab tabItem)
                        {
                            foreach (var content in tabItem.Items)
                            {
                                if (content is ShellContent shellContent && shellContent.Route == route)
                                {
                                    return tabItem;
                                }
                            }
                        }
                    }
                }
            }
            
            return null;
        }
        
        private async void OnUserLoggedOut(object sender, EventArgs e)
        {
            // Navigate back to login page when user logs out
            Application.Current.MainPage = new NavigationPage(new LoginPage(_authService));
        }
        
        // Method to handle logout
        public void Logout()
        {
            _authService.Logout();
        }
        
        // Method to navigate to the Change Password page
        private async Task GoToChangePasswordAsync()
        {
            await Shell.Current.GoToAsync("Account/ChangePassword");
        }
    }
}
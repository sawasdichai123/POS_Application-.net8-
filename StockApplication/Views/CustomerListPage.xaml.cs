using StockApplication.Models;
using StockApplication.Repositories;
using System.Collections.ObjectModel;

namespace StockApplication.Views;

public partial class CustomerListPage : ContentPage
{
    private readonly Repository<Customer> _customerRepository;
    private readonly Repository<Order> _orderRepository;
    private ObservableCollection<Customer> _customers = new();

    public CustomerListPage(Repository<Customer> customerRepository, Repository<Order> orderRepository)
    {
        InitializeComponent();
        
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
        
        // Set up the collection view
        CustomerCollectionView.ItemsSource = _customers;
        
        // Set up refresh view
        CustomerRefreshView.Command = new Command(async () => await LoadCustomersAsync());
        
        // Set up event handlers
        AddCustomerButton.Clicked += OnAddCustomerClicked;
        SearchEntry.TextChanged += OnSearchTextChanged;
        CustomerCollectionView.SelectionChanged += OnCustomerSelectionChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Load customers
        await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            CustomerRefreshView.IsRefreshing = true;
            
            // Get all customers
            var customers = await _customerRepository.GetAllAsync();
            
            // Update collection
            _customers.Clear();
            foreach (var customer in customers)
            {
                _customers.Add(customer);
            }
            
            // Update total count
            TotalItemsLabel.Text = $"Total Customers: {_customers.Count}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load customers: {ex.Message}", "OK");
        }
        finally
        {
            CustomerRefreshView.IsRefreshing = false;
        }
    }

    private async void OnAddCustomerClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("Customers/Add");
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            await LoadCustomersAsync();
            return;
        }
        
        var searchTerm = e.NewTextValue.ToLowerInvariant();
        var allCustomers = await _customerRepository.GetAllAsync();
        
        var filteredCustomers = allCustomers.Where(c => 
            c.Cus_Name.ToLowerInvariant().Contains(searchTerm) ||
            (c.Cus_Phone != null && c.Cus_Phone.ToLowerInvariant().Contains(searchTerm)) ||
            (c.Email != null && c.Email.ToLowerInvariant().Contains(searchTerm)));
        
        _customers.Clear();
        foreach (var customer in filteredCustomers)
        {
            _customers.Add(customer);
        }
        
        TotalItemsLabel.Text = $"Search Results: {_customers.Count}";
    }

    private async void OnCustomerSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Customer selectedCustomer)
        {
            // Navigate to edit page with the selected customer ID
            CustomerCollectionView.SelectedItem = null;
            await Shell.Current.GoToAsync($"Customers/Edit?id={selectedCustomer.MemberID}");
        }
    }
    
    private async void OnEditCustomerClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Customer customer)
        {
            // Navigate to edit page with the selected customer ID
            await Shell.Current.GoToAsync($"Customers/Edit?id={customer.MemberID}");
        }
    }
    
    private async void OnViewOrdersClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Customer customer)
        {
            // Navigate to customer orders page
            await Shell.Current.GoToAsync($"Customers/Orders?id={customer.MemberID}");
        }
    }
    
    private async void OnDeleteCustomerClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Customer customer)
        {
            // Check if the customer has orders
            var customerOrders = await _orderRepository.FindAsync(o => o.MemberID == customer.MemberID);
            
            if (customerOrders.Any())
            {
                // Customer has orders, show warning
                bool confirm = await DisplayAlert("Warning", 
                    $"{customer.Cus_Name} has existing orders. Deleting this customer will not delete their order history. Continue?", 
                    "Yes", "No");
                    
                if (!confirm)
                    return;
            }
            else
            {
                // Simple confirmation
                bool confirm = await DisplayAlert("Delete Customer", 
                    $"Are you sure you want to delete {customer.Cus_Name}?", 
                    "Yes", "No");
                    
                if (!confirm)
                    return;
            }
            
            try
            {
                // Delete the customer
                await _customerRepository.DeleteAsync(customer);
                
                // Refresh the list
                await LoadCustomersAsync();
                
                // Show success message
                await DisplayAlert("Success", "Customer deleted successfully.", "OK");
            }
            catch (Exception ex)
            {
                // Show error message
                await DisplayAlert("Error", $"Failed to delete customer: {ex.Message}", "OK");
            }
        }
    }
}
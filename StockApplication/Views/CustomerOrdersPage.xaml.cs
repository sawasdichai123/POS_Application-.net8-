using StockApplication.Models;
using StockApplication.Repositories;
using System.Collections.ObjectModel;

namespace StockApplication.Views;

[QueryProperty(nameof(CustomerId), "id")]
public partial class CustomerOrdersPage : ContentPage
{
    private readonly Repository<Customer> _customerRepository;
    private readonly Repository<Order> _orderRepository;
    private int _customerId;
    private Customer _customer;
    
    private ObservableCollection<OrderViewModel> _orders = new();

    public int CustomerId
    {
        get => _customerId;
        set
        {
            _customerId = value;
            LoadCustomerData();
        }
    }

    public CustomerOrdersPage(Repository<Customer> customerRepository, Repository<Order> orderRepository)
    {
        InitializeComponent();
        
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
        
        // Set up collection view
        CustomerOrdersCollection.ItemsSource = _orders;
        
        // Set up refresh view
        OrdersRefreshView.Command = new Command(async () => await LoadOrdersAsync());
        
        // Set up event handlers
        BackButton.Clicked += OnBackClicked;
        NewOrderButton.Clicked += OnNewOrderClicked;
    }

    private async void LoadCustomerData()
    {
        try
        {
            // Load customer data
            _customer = await _customerRepository.GetByIdAsync(_customerId);
            
            if (_customer != null)
            {
                // Update UI with customer info
                CustomerNameLabel.Text = _customer.Cus_Name;
                CustomerPointsLabel.Text = $"Points: {_customer.Points}";
                
                // Load orders for this customer
                await LoadOrdersAsync();
            }
            else
            {
                // Customer not found
                await DisplayAlert("Error", "Customer not found.", "OK");
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load customer data: {ex.Message}", "OK");
        }
    }

    private async Task LoadOrdersAsync()
    {
        try
        {
            OrdersRefreshView.IsRefreshing = true;
            
            // Get all orders for this customer
            var customerOrders = await _orderRepository.FindAsync(o => o.MemberID == _customerId);
            
            // Convert to view models
            _orders.Clear();
            foreach (var order in customerOrders)
            {
                // Set status color
                string statusColor = "#9E9E9E"; // Default gray
                switch (order.PaymentStatus)
                {
                    case "Pending":
                        statusColor = "#FF9800"; // Orange
                        break;
                    case "Paid":
                        statusColor = "#4CAF50"; // Green
                        break;
                    case "Cancelled":
                        statusColor = "#F44336"; // Red
                        break;
                }
                
                _orders.Add(new OrderViewModel
                {
                    OrderID = order.OrderID,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    PaymentStatus = order.PaymentStatus,
                    StatusColor = statusColor
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load customer orders: {ex.Message}", "OK");
        }
        finally
        {
            OrdersRefreshView.IsRefreshing = false;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnNewOrderClicked(object sender, EventArgs e)
    {
        // Navigate to new order page, pre-filling the customer ID
        await Shell.Current.GoToAsync($"Orders/Add?customerId={_customerId}");
    }

    private async void OnViewOrderDetailsClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is OrderViewModel order)
        {
            // Navigate to order details
            await Shell.Current.GoToAsync($"Orders/Edit?id={order.OrderID}");
        }
    }
}
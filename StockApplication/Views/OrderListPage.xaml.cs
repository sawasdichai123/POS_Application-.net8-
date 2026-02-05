using StockApplication.Models;
using StockApplication.Repositories;
using System.Collections.ObjectModel;

namespace StockApplication.Views;

public partial class OrderListPage : ContentPage
{
    private readonly Repository<Order> _orderRepository;
    private readonly Repository<Customer> _customerRepository;
    private readonly Repository<Staff> _staffRepository;
    private readonly Repository<OrderDetail> _orderDetailRepository;
    
    private ObservableCollection<OrderViewModel> _orders = new();
    private Dictionary<int, string> _customerNames = new();
    private Dictionary<int, string> _staffNames = new();
    private Dictionary<int, int> _orderItemCounts = new();
    private string _currentFilter = "All"; // Track current filter

    public OrderListPage(
        Repository<Order> orderRepository,
        Repository<Customer> customerRepository,
        Repository<Staff> staffRepository,
        Repository<OrderDetail> orderDetailRepository)
    {
        InitializeComponent();
        
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _staffRepository = staffRepository;
        _orderDetailRepository = orderDetailRepository;
        
        // Set up the collection view
        OrderCollectionView.ItemsSource = _orders;
        
        // Set up refresh view
        OrderRefreshView.Command = new Command(async () => await LoadOrdersAsync(_currentFilter));
        
        // Set up event handlers
        AddOrderButton.Clicked += OnAddOrderClicked;
        OrderCollectionView.SelectionChanged += OnOrderSelectionChanged;
        
        // Filter buttons - updated to save the filter type
        AllFilterButton.Clicked += (s, e) => {
            _currentFilter = "All";
            LoadOrdersAsync(_currentFilter);
        };
        
        PendingFilterButton.Clicked += (s, e) => {
            _currentFilter = "Pending";
            LoadOrdersAsync(_currentFilter);
        };
        
        PaidFilterButton.Clicked += (s, e) => {
            _currentFilter = "Paid";
            LoadOrdersAsync(_currentFilter);
        };
        
        CancelledFilterButton.Clicked += (s, e) => {
            _currentFilter = "Cancelled";
            LoadOrdersAsync(_currentFilter);
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Load reference data
        await LoadReferenceDataAsync();
        
        // Load orders with current filter
        await LoadOrdersAsync(_currentFilter);
    }

    private async Task LoadReferenceDataAsync()
    {
        try
        {
            // Load customers
            var customers = await _customerRepository.GetAllAsync();
            _customerNames.Clear();
            foreach (var customer in customers)
            {
                _customerNames[customer.MemberID] = customer.Cus_Name;
            }
            
            // Load staff
            var staffList = await _staffRepository.GetAllAsync();
            _staffNames.Clear();
            foreach (var staff in staffList)
            {
                _staffNames[staff.StaffID] = staff.Staff_Name;
            }
            
            // Load order item counts
            var orderDetails = await _orderDetailRepository.GetAllAsync();
            _orderItemCounts.Clear();
            foreach (var detail in orderDetails)
            {
                if (!_orderItemCounts.ContainsKey(detail.OrderID))
                {
                    _orderItemCounts[detail.OrderID] = 0;
                }
                _orderItemCounts[detail.OrderID] += 1;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load reference data: {ex.Message}", "OK");
        }
    }

    private async Task LoadOrdersAsync(string filter)
    {
        try
        {
            // Update filter button styles
            UpdateFilterButtonStyles(filter);
            
            OrderRefreshView.IsRefreshing = true;
            
            // Get orders based on filter
            List<Order> orders;
            if (filter == "All")
            {
                orders = await _orderRepository.GetAllAsync();
            }
            else
            {
                orders = await _orderRepository.FindAsync(o => o.PaymentStatus == filter);
            }
            
            // Create view models
            _orders.Clear();
            foreach (var order in orders)
            {
                string customerName = "Unknown";
                if (order.MemberID.HasValue && _customerNames.ContainsKey(order.MemberID.Value))
                {
                    customerName = _customerNames[order.MemberID.Value];
                }
                
                string staffName = "Unknown";
                if (order.StaffID.HasValue && _staffNames.ContainsKey(order.StaffID.Value))
                {
                    staffName = _staffNames[order.StaffID.Value];
                }
                
                int itemsCount = 0;
                if (_orderItemCounts.ContainsKey(order.OrderID))
                {
                    itemsCount = _orderItemCounts[order.OrderID];
                }
                
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
                    CustomerName = customerName,
                    StaffName = staffName,
                    ItemsCount = itemsCount,
                    StatusColor = statusColor,
                    // Add a property to control checkout button visibility
                    CanCheckout = order.PaymentStatus == "Pending"
                });
            }
            
            // Update total count
            TotalItemsLabel.Text = $"Total Orders: {_orders.Count}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load orders: {ex.Message}", "OK");
        }
        finally
        {
            OrderRefreshView.IsRefreshing = false;
        }
    }

    private void UpdateFilterButtonStyles(string selectedFilter)
    {
        // Reset all buttons to the default/inactive style
        AllFilterButton.BackgroundColor = Color.Parse("#E0E0E0");
        AllFilterButton.TextColor = Color.Parse("#616161");
        
        PendingFilterButton.BackgroundColor = Color.Parse("#E0E0E0");
        PendingFilterButton.TextColor = Color.Parse("#616161");
        
        PaidFilterButton.BackgroundColor = Color.Parse("#E0E0E0");
        PaidFilterButton.TextColor = Color.Parse("#616161");
        
        CancelledFilterButton.BackgroundColor = Color.Parse("#E0E0E0");
        CancelledFilterButton.TextColor = Color.Parse("#616161");
        
        // Set the active style for the selected filter button
        // Using a consistent color for all active buttons
        Color activeBackgroundColor = Color.Parse("#3F51B5"); // Primary blue color for all active buttons
        Color activeTextColor = Colors.White;
        
        switch (selectedFilter)
        {
            case "All":
                AllFilterButton.BackgroundColor = activeBackgroundColor;
                AllFilterButton.TextColor = activeTextColor;
                break;
            case "Pending":
                PendingFilterButton.BackgroundColor = activeBackgroundColor;
                PendingFilterButton.TextColor = activeTextColor;
                break;
            case "Paid":
                PaidFilterButton.BackgroundColor = activeBackgroundColor;
                PaidFilterButton.TextColor = activeTextColor;
                break;
            case "Cancelled":
                CancelledFilterButton.BackgroundColor = activeBackgroundColor;
                CancelledFilterButton.TextColor = activeTextColor;
                break;
        }
    }

    private async void OnAddOrderClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("Orders/Add");
    }

    private async void OnOrderSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is OrderViewModel selectedOrder)
        {
            // Clear selection
            OrderCollectionView.SelectedItem = null;
            
            // Navigate to edit page
            await Shell.Current.GoToAsync($"Orders/Edit?id={selectedOrder.OrderID}");
        }
    }

    // Fixed event handlers for the buttons
    private async void OnEditClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is OrderViewModel order)
        {
            // Navigate to edit page with the selected order ID
            await Shell.Current.GoToAsync($"Orders/Edit?id={order.OrderID}");
        }
    }

    private async void OnDetailsClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is OrderViewModel order)
        {
            // Navigate to the dedicated details page
            await Shell.Current.GoToAsync($"Orders/Details?id={order.OrderID}");
        }
    }

    private async void OnCheckoutClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is OrderViewModel order)
        {
            // Navigate to checkout page with the selected order ID
            await Shell.Current.GoToAsync($"Checkout?id={order.OrderID}");
        }
    }
}

// View model for displaying orders with additional properties
public class OrderViewModel
{
    public int OrderID { get; set; }
    public DateTime OrderDate { get; set; }
    public double TotalAmount { get; set; }
    public string PaymentStatus { get; set; }
    public string CustomerName { get; set; }
    public string StaffName { get; set; }
    public int ItemsCount { get; set; }
    public string StatusColor { get; set; }
    
    // Added property to control checkout button visibility
    public bool CanCheckout { get; set; }
}
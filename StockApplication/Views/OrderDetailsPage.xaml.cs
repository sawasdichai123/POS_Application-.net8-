using StockApplication.Models;
using StockApplication.Repositories;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StockApplication.Views;

[QueryProperty(nameof(OrderId), "id")]
public partial class OrderDetailsPage : ContentPage
{
    private readonly Repository<Order> _orderRepository;
    private readonly Repository<Customer> _customerRepository;
    private readonly Repository<Staff> _staffRepository;
    private readonly Repository<OrderDetail> _orderDetailRepository;
    private readonly Repository<MenuItem> _menuItemRepository;
    private readonly Repository<Table> _tableRepository;
    
    private ObservableCollection<OrderItemViewModel> _orderItems = new();
    private double _totalAmount = 0;
    
    public string OrderId { get; set; }
    private Order _currentOrder;
    
    public OrderDetailsPage(
        Repository<Order> orderRepository,
        Repository<Customer> customerRepository,
        Repository<Staff> staffRepository,
        Repository<OrderDetail> orderDetailRepository,
        Repository<MenuItem> menuItemRepository,
        Repository<Table> tableRepository)
    {
        InitializeComponent();
        
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _staffRepository = staffRepository;
        _orderDetailRepository = orderDetailRepository;
        _menuItemRepository = menuItemRepository;
        _tableRepository = tableRepository;
        
        // Set up the collection view
        OrderItemsCollectionView.ItemsSource = _orderItems;
        
        // Set up event handlers
        BackButton.Clicked += OnBackButtonClicked;
        EditButton.Clicked += OnEditButtonClicked;
        CheckoutButton.Clicked += OnCheckoutButtonClicked;
        
        // Subscribe to collection changed events to update total automatically
        _orderItems.CollectionChanged += (sender, e) => UpdateOrderTotal();
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (!string.IsNullOrEmpty(OrderId) && int.TryParse(OrderId, out int orderId))
        {
            await LoadOrderDetailsAsync(orderId);
        }
        else
        {
            await DisplayAlert("Error", "Invalid order ID", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }
    
    private async Task LoadOrderDetailsAsync(int orderId)
    {
        try
        {
            // Show loading indicator
            IsBusy = true;
            
            // Load order
            _currentOrder = await _orderRepository.GetByIdAsync(orderId);
            if (_currentOrder == null)
            {
                await DisplayAlert("Error", "Order not found", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }
            
            // Update UI with order details
            OrderNumberLabel.Text = $"Order #{_currentOrder.OrderID}";
            OrderDateLabel.Text = $"Order Date: {_currentOrder.OrderDate:MM/dd/yyyy}";
            
            // Update status and color
            StatusLabel.Text = _currentOrder.PaymentStatus;
            switch (_currentOrder.PaymentStatus)
            {
                case "Pending":
                    StatusBorder.BackgroundColor = Color.Parse("#FF9800");
                    CheckoutButton.IsVisible = true;
                    break;
                case "Paid":
                    StatusBorder.BackgroundColor = Color.Parse("#4CAF50");
                    CheckoutButton.IsVisible = false;
                    break;
                case "Cancelled":
                    StatusBorder.BackgroundColor = Color.Parse("#F44336");
                    CheckoutButton.IsVisible = false;
                    EditButton.IsVisible = false;
                    break;
                default:
                    StatusBorder.BackgroundColor = Color.Parse("#9E9E9E");
                    CheckoutButton.IsVisible = false;
                    break;
            }
            
            // Load customer
            if (_currentOrder.MemberID.HasValue)
            {
                var customer = await _customerRepository.GetByIdAsync(_currentOrder.MemberID.Value);
                CustomerNameLabel.Text = customer?.Cus_Name ?? "Unknown";
            }
            else
            {
                CustomerNameLabel.Text = "Walk-in Customer";
            }
            
            // Load staff
            if (_currentOrder.StaffID.HasValue)
            {
                var staff = await _staffRepository.GetByIdAsync(_currentOrder.StaffID.Value);
                StaffNameLabel.Text = staff?.Staff_Name ?? "Unknown";
            }
            else
            {
                StaffNameLabel.Text = "Unknown";
            }
            
            // Load table
            if (_currentOrder.TableID.HasValue)
            {
                var table = await _tableRepository.GetByIdAsync(_currentOrder.TableID.Value);
                TableNumberLabel.Text = table != null ? 
                    (string.IsNullOrEmpty(table.TableName) ? $"Table #{table.TableID}" : table.TableName) : 
                    "No table assigned";
            }
            else
            {
                TableNumberLabel.Text = "No table assigned";
            }
            
            // Load notes if available
            try
            {
                if (!string.IsNullOrWhiteSpace(_currentOrder.Notes))
                {
                    NotesLabel.Text = _currentOrder.Notes;
                    NotesFrame.IsVisible = true;
                }
            }
            catch
            {
                // Notes property might not exist yet
                NotesFrame.IsVisible = false;
            }
            
            // Load order items
            await LoadOrderItemsAsync(orderId);
            
            // Hide loading indicator
            IsBusy = false;
        }
        catch (Exception ex)
        {
            IsBusy = false;
            await DisplayAlert("Error", $"Failed to load order details: {ex.Message}", "OK");
        }
    }
    
    private async Task LoadOrderItemsAsync(int orderId)
    {
        try
        {
            _orderItems.Clear();
            
            // Get all order details for this order
            var orderDetails = await _orderDetailRepository.FindAsync(od => od.OrderID == orderId);
            
            // Get all menu items for lookup
            var menuItems = await _menuItemRepository.GetAllAsync();
            var menuItemDict = menuItems.ToDictionary(m => m.MenuID);
            
            // Create view models
            foreach (var detail in orderDetails)
            {
                // Try to get the menu item
                if (menuItemDict.TryGetValue(detail.MenuID, out var menuItem))
                {
                    var orderItem = new OrderItemViewModel
                    {
                        MenuID = menuItem.MenuID,
                        Menu_Name = menuItem.Menu_Name,
                        Price = menuItem.Price,
                        Quantity = detail.Quantity,
                        Subtotal = detail.Subtotal
                    };
                    
                    // Ensure the subtotal is calculated correctly
                    if (orderItem.Subtotal <= 0)
                    {
                        orderItem.Subtotal = orderItem.Price * orderItem.Quantity;
                    }
                    
                    _orderItems.Add(orderItem);
                }
                else
                {
                    // If menu item not found, still display the order item with limited info
                    var orderItem = new OrderItemViewModel
                    {
                        MenuID = detail.MenuID,
                        Menu_Name = $"Item #{detail.MenuID}",
                        Price = detail.Subtotal / detail.Quantity, // Estimate price from subtotal
                        Quantity = detail.Quantity,
                        Subtotal = detail.Subtotal
                    };
                    
                    // Ensure the subtotal is calculated correctly
                    if (orderItem.Subtotal <= 0)
                    {
                        orderItem.Subtotal = orderItem.Price * orderItem.Quantity;
                    }
                    
                    _orderItems.Add(orderItem);
                }
            }
            
            // Update the total items count
            ItemsCountLabel.Text = _orderItems.Count.ToString();
            
            // Update the total amount
            UpdateOrderTotal();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load order items: {ex.Message}", "OK");
        }
    }
    
    private void UpdateOrderTotal()
    {
        _totalAmount = _orderItems.Sum(i => i.Subtotal);
        
        MainThread.BeginInvokeOnMainThread(() => {
            TotalAmountLabel.Text = $"${_totalAmount:F2}";
        });
    }
    
    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
    
    private async void OnEditButtonClicked(object sender, EventArgs e)
    {
        if (_currentOrder != null)
        {
            await Shell.Current.GoToAsync($"Orders/Edit?id={_currentOrder.OrderID}");
        }
    }
    
    private async void OnCheckoutButtonClicked(object sender, EventArgs e)
    {
        if (_currentOrder != null)
        {
            await Shell.Current.GoToAsync($"Checkout?id={_currentOrder.OrderID}");
        }
    }
}

// View model for displaying order items with property change notification
public class OrderItemViewModel : INotifyPropertyChanged
{
    private int _quantity;
    private double _subtotal;

    public int MenuID { get; set; }
    public string Menu_Name { get; set; }
    public double Price { get; set; }
    
    public int Quantity 
    { 
        get => _quantity;
        set
        {
            if (_quantity != value)
            {
                _quantity = value;
                OnPropertyChanged();
            }
        }
    }
    
    public double Subtotal 
    { 
        get => _subtotal;
        set
        {
            if (_subtotal != value)
            {
                _subtotal = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
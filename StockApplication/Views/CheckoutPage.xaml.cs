using StockApplication.Models;
using StockApplication.Repositories;
using StockApplication.ViewModels;
using System.Collections.ObjectModel;

namespace StockApplication.Views;

[QueryProperty(nameof(OrderId), "id")]
public partial class CheckoutPage : ContentPage
{
    private readonly Repository<Order> _orderRepository;
    private readonly Repository<Customer> _customerRepository;
    private readonly Repository<OrderDetail> _orderDetailRepository;
    private readonly Repository<MenuItem> _menuItemRepository;
    private readonly Repository<Payment> _paymentRepository;
    private readonly Repository<Table> _tableRepository;
    
    private int _orderId;
    private Order _currentOrder;
    private Customer _selectedCustomer;
    private Table _selectedTable;
    private ObservableCollection<OrderItemViewModel> _orderItems = new();
    private ObservableCollection<Table> _tables = new();
    private ObservableCollection<Customer> _filteredCustomers = new();
    
    private double _subtotal = 0;
    private double _discount = 0;
    private double _tax = 0;
    private double _total = 0;
    private string _selectedPaymentMethod = "Cash";
    private bool _isTakeAway = false;
    private string[] _paymentMethods = { "Cash", "Mobile Banking", "QR Code" };
    
    private const double MEMBER_DISCOUNT_PERCENTAGE = 0.05; // 5% discount for members

    public int OrderId
    {
        get => _orderId;
        set
        {
            _orderId = value;
            if (_orderId > 0)
            {
                LoadOrderAsync(_orderId);
            }
        }
    }

    public CheckoutPage(
        Repository<Order> orderRepository,
        Repository<Customer> customerRepository,
        Repository<OrderDetail> orderDetailRepository,
        Repository<MenuItem> menuItemRepository,
        Repository<Payment> paymentRepository,
        Repository<Table> tableRepository)
    {
        InitializeComponent();
        
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _orderDetailRepository = orderDetailRepository;
        _menuItemRepository = menuItemRepository;
        _paymentRepository = paymentRepository;
        _tableRepository = tableRepository;
        
        // Set up data sources
        OrderItemsCollection.ItemsSource = _orderItems;
        TablePicker.ItemsSource = _tables;
        CustomerCollectionView.ItemsSource = _filteredCustomers;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Load tables
        await LoadTablesAsync();
        
        // Load customers for search
        await LoadCustomersAsync();

        // Force UI update after loading data
        MainThread.BeginInvokeOnMainThread(() => {
            if (_selectedTable != null)
            {
                SelectedTableLabel.Text = $"Selected: {_selectedTable.TableName}";
                SelectedTableLabel.TextColor = Color.FromArgb("#4CAF50");
            }
        });
    }

    private async Task LoadTablesAsync()
    {
        try
        {
            var tables = await _tableRepository.GetAllAsync();
            
            // Create a custom comparer for sorting tables
            var tableComparer = new Comparison<Table>((t1, t2) => 
            {
                // Put Take Away at the end
                if (t1.TableName == "Take Away") return 1;
                if (t2.TableName == "Take Away") return -1;
                
                // Extract table numbers for numerical sorting
                if (t1.TableName.StartsWith("Table ") && t2.TableName.StartsWith("Table "))
                {
                    if (int.TryParse(t1.TableName.Substring(6), out int num1) && 
                        int.TryParse(t2.TableName.Substring(6), out int num2))
                    {
                        return num1.CompareTo(num2);
                    }
                }
                
                // Default to string comparison
                return string.Compare(t1.TableName, t2.TableName, StringComparison.Ordinal);
            });
            
            // Sort the tables
            var sortedTables = tables.ToList();
            sortedTables.Sort(tableComparer);
            
            // Clear the collection and add the sorted tables
            MainThread.BeginInvokeOnMainThread(() => {
                _tables.Clear();
                foreach (var table in sortedTables)
                {
                    _tables.Add(table);
                }
                
                Console.WriteLine($"Loaded {_tables.Count} tables");
            });
            
            // If order already has a table associated, select it
            if (_currentOrder != null && _currentOrder.TableID.HasValue)
            {
                var orderTable = _tables.FirstOrDefault(t => t.TableID == _currentOrder.TableID.Value);
                if (orderTable != null)
                {
                    int index = _tables.IndexOf(orderTable);
                    MainThread.BeginInvokeOnMainThread(() => {
                        try {
                            TablePicker.SelectedIndex = index;
                            _selectedTable = orderTable;
                            UpdateSelectedTableLabel();
                            
                            // Check if it's Take Away and update UI accordingly
                            _isTakeAway = orderTable.TableName == "Take Away";
                            UpdateUIForTableSelection();
                            
                            Console.WriteLine($"Set selected table: {orderTable.TableName}, index: {index}");
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"Error setting selected table: {ex.Message}");
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in LoadTablesAsync: {ex.Message}");
            await DisplayAlert("Error", $"Failed to load tables: {ex.Message}", "OK");
        }
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            // Load all customers for searching
            var allCustomers = await _customerRepository.GetAllAsync();
            
            MainThread.BeginInvokeOnMainThread(() => {
                _filteredCustomers.Clear();
                foreach (var customer in allCustomers)
                {
                    _filteredCustomers.Add(customer);
                }
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load customers: {ex.Message}", "OK");
        }
    }

    private async void LoadOrderAsync(int orderId)
    {
        try
        {
            // Show loading indicator
            IsBusy = true;
            
            // Load order details
            _currentOrder = await _orderRepository.GetByIdAsync(orderId);
            if (_currentOrder == null)
            {
                await DisplayAlert("Error", "Order not found", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }
            
            // Set order information
            OrderIdLabel.Text = _currentOrder.OrderID.ToString();
            OrderDateLabel.Text = _currentOrder.OrderDate.ToString("MM/dd/yyyy");
            
            // Load customer if available
            if (_currentOrder.MemberID.HasValue)
            {
                _selectedCustomer = await _customerRepository.GetByIdAsync(_currentOrder.MemberID.Value);
                if (_selectedCustomer != null)
                {
                    // Set member radio button
                    MemberRadioButton.IsChecked = true;
                    GuestRadioButton.IsChecked = false;
                    
                    // Update customer display
                    SelectedCustomerLabel.Text = $"{_selectedCustomer.Cus_Name} (ID: {_selectedCustomer.MemberID})";
                    ClearCustomerButton.IsVisible = true;
                }
                else
                {
                    // Set to guest if customer not found
                    GuestRadioButton.IsChecked = true;
                    MemberRadioButton.IsChecked = false;
                    SelectedCustomerLabel.Text = "Guest";
                    ClearCustomerButton.IsVisible = false;
                }
            }
            else
            {
                // Set to guest
                GuestRadioButton.IsChecked = true;
                MemberRadioButton.IsChecked = false;
                SelectedCustomerLabel.Text = "Guest";
                ClearCustomerButton.IsVisible = false;
            }
            
            // Load order items
            await LoadOrderItemsAsync();
            
            // Calculate totals
            CalculateTotals();
            
            // If tables are already loaded, try to select the table
            if (_tables.Count > 0 && _currentOrder.TableID.HasValue)
            {
                var orderTable = _tables.FirstOrDefault(t => t.TableID == _currentOrder.TableID.Value);
                if (orderTable != null)
                {
                    int index = _tables.IndexOf(orderTable);
                    MainThread.BeginInvokeOnMainThread(() => {
                        try {
                            TablePicker.SelectedIndex = index;
                            _selectedTable = orderTable;
                            UpdateSelectedTableLabel();
                            
                            // Check if it's Take Away and update UI accordingly
                            _isTakeAway = orderTable.TableName == "Take Away";
                            UpdateUIForTableSelection();
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"Error setting selected table: {ex.Message}");
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in LoadOrderAsync: {ex.Message}");
            await DisplayAlert("Error", $"Failed to load order: {ex.Message}", "OK");
        }
        finally
        {
            // Hide loading indicator
            IsBusy = false;
        }
    }

    private async Task LoadOrderItemsAsync()
    {
        try
        {
            // Clear current items
            _orderItems.Clear();
            
            // Get order details
            var orderDetails = await _orderDetailRepository.FindAsync(od => od.OrderID == _orderId);
            if (orderDetails == null || !orderDetails.Any())
            {
                Console.WriteLine($"No order details found for order ID {_orderId}");
                return;
            }
            
            Console.WriteLine($"Found {orderDetails.Count()} order details for order ID {_orderId}");
            
            // Get all menu items for lookup
            var menuItems = await _menuItemRepository.GetAllAsync();
            
            // Create view models
            foreach (var detail in orderDetails)
            {
                var menuItem = menuItems.FirstOrDefault(m => m.MenuID == detail.MenuID);
                if (menuItem != null)
                {
                    var orderItemVM = new OrderItemViewModel
                    {
                        MenuID = menuItem.MenuID,
                        Menu_Name = menuItem.Menu_Name,
                        Price = menuItem.Price,
                        Quantity = detail.Quantity,
                        Subtotal = detail.Subtotal
                    };
                    
                    MainThread.BeginInvokeOnMainThread(() => {
                        _orderItems.Add(orderItemVM);
                    });
                    
                    Console.WriteLine($"Added item: {menuItem.Menu_Name}, Qty: {detail.Quantity}, Subtotal: {detail.Subtotal}");
                }
                else
                {
                    Console.WriteLine($"Menu item not found for MenuID: {detail.MenuID}");
                }
            }
            
            // Force UI update
            MainThread.BeginInvokeOnMainThread(() => {
                // This triggers a refresh of the CollectionView
                var temp = OrderItemsCollection.ItemsSource;
                OrderItemsCollection.ItemsSource = null;
                OrderItemsCollection.ItemsSource = temp;
                
                Console.WriteLine($"OrderItems collection now has {_orderItems.Count} items");
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in LoadOrderItemsAsync: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            await DisplayAlert("Error", $"Failed to load order items: {ex.Message}", "OK");
        }
    }

    private void CalculateTotals()
    {
        // Calculate subtotal
        _subtotal = _orderItems.Sum(i => i.Subtotal);
        
        // Calculate discount (5% for members)
        _discount = 0;
        if (_selectedCustomer != null)
        {
            _discount = Math.Round(_subtotal * MEMBER_DISCOUNT_PERCENTAGE, 2);
            DiscountGrid.IsVisible = true;
            DiscountLabel.Text = $"-฿{_discount:F2}";
        }
        else
        {
            DiscountGrid.IsVisible = false;
        }
        
        // Calculate tax (10% after discount)
        _tax = Math.Round((_subtotal - _discount) * 0.1, 2);
        
        // Calculate total
        _total = _subtotal - _discount + _tax;
        
        // Update UI
        MainThread.BeginInvokeOnMainThread(() => {
            SubtotalLabel.Text = $"฿{_subtotal:F2}";
            TaxLabel.Text = $"฿{_tax:F2}";
            TotalLabel.Text = $"฿{_total:F2}";
            
            Console.WriteLine($"Calculated totals: Subtotal={_subtotal}, Discount={_discount}, Tax={_tax}, Total={_total}");
        });
    }

    private void OnTableSelectionChanged(object sender, EventArgs e)
    {
        if (TablePicker.SelectedItem is Table table)
        {
            _selectedTable = table;
            UpdateSelectedTableLabel();
            
            // Check if it's Take Away and update UI accordingly
            _isTakeAway = table.TableName == "Take Away";
            UpdateUIForTableSelection();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => {
                SelectedTableLabel.Text = "No table selected";
                SelectedTableLabel.TextColor = Color.FromArgb("#616161");
            });
        }
    }
    
    private void UpdateSelectedTableLabel()
    {
        if (_selectedTable != null)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                SelectedTableLabel.Text = $"Selected: {_selectedTable.TableName}";
                SelectedTableLabel.TextColor = Color.FromArgb("#4CAF50");
            });
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => {
                SelectedTableLabel.Text = "No table selected";
                SelectedTableLabel.TextColor = Color.FromArgb("#616161");
            });
        }
    }
    
    private void UpdateUIForTableSelection()
    {
        if (_isTakeAway)
        {
            // For Take Away, automatically select Cash and hide other payment methods
            MainThread.BeginInvokeOnMainThread(() => {
                CashRadioButton.IsChecked = true;
                _selectedPaymentMethod = "Cash";
                
                // Hide mobile banking and QR Code options
                MobileBankingRadioButton.IsEnabled = false;
                QRCodeRadioButton.IsEnabled = false;
                
                // Update button text to reflect auto-payment
                CompletePaymentButton.Text = "Complete Take Away Order";
            });
        }
        else
        {
            // For regular tables, enable all payment methods
            MainThread.BeginInvokeOnMainThread(() => {
                MobileBankingRadioButton.IsEnabled = true;
                QRCodeRadioButton.IsEnabled = true;
                
                // Reset to cash if it was previously disabled
                if (!CashRadioButton.IsEnabled)
                {
                    CashRadioButton.IsChecked = true;
                    _selectedPaymentMethod = "Cash";
                }
                
                // Update button text to normal
                CompletePaymentButton.Text = "Complete Payment";
            });
        }
    }

    private void OnPaymentMethodChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return; // Only handle when a radio button is checked
        
        // Simply update the selected payment method without showing details
        if (sender == CashRadioButton)
        {
            _selectedPaymentMethod = "Cash";
        }
        else if (sender == MobileBankingRadioButton)
        {
            _selectedPaymentMethod = "Mobile Banking";
        }
        else if (sender == QRCodeRadioButton)
        {
            _selectedPaymentMethod = "QR Code";
        }
    }

    private void OnCustomerTypeChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender == GuestRadioButton && e.Value)
        {
            // Guest selected
            MainThread.BeginInvokeOnMainThread(() => {
                MemberSelectionPanel.IsVisible = false;
                SelectedCustomerLabel.Text = "Guest";
                ClearCustomerButton.IsVisible = false;
                _selectedCustomer = null;
            });
            
            // Recalculate totals to remove any discount
            CalculateTotals();
        }
        else if (sender == MemberRadioButton && e.Value)
        {
            // Member selected
            MainThread.BeginInvokeOnMainThread(() => {
                MemberSelectionPanel.IsVisible = true;
                if (_selectedCustomer == null)
                {
                    SelectedCustomerLabel.Text = "No member selected";
                    ClearCustomerButton.IsVisible = false;
                }
            });
        }
    }

    // Customer search functionality
    private async void OnCustomerSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                // Reset to full list
                var allCustomers = await _customerRepository.GetAllAsync();
                
                MainThread.BeginInvokeOnMainThread(() => {
                    _filteredCustomers.Clear();
                    foreach (var customer in allCustomers)
                    {
                        _filteredCustomers.Add(customer);
                    }
                });
            }
            else
            {
                // Filter the list
                var searchTerm = e.NewTextValue.ToLowerInvariant();
                var allCustomers = await _customerRepository.GetAllAsync();
                
                var filteredList = allCustomers.Where(c => 
                    c.Cus_Name.ToLowerInvariant().Contains(searchTerm) || 
                    c.MemberID.ToString().Contains(searchTerm) ||
                    (c.Cus_Phone != null && c.Cus_Phone.ToLowerInvariant().Contains(searchTerm))).ToList();
                
                MainThread.BeginInvokeOnMainThread(() => {
                    _filteredCustomers.Clear();
                    foreach (var customer in filteredList)
                    {
                        _filteredCustomers.Add(customer);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in OnCustomerSearchTextChanged: {ex.Message}");
        }
    }

    // Customer selection changed
    private void OnCustomerSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Customer selectedCustomer)
        {
            // Update the selected customer
            _selectedCustomer = selectedCustomer;
            
            MainThread.BeginInvokeOnMainThread(() => {
                SelectedCustomerLabel.Text = $"{selectedCustomer.Cus_Name} (ID: {selectedCustomer.MemberID})";
                ClearCustomerButton.IsVisible = true;
                
                // Clear the selection for better UX
                CustomerCollectionView.SelectedItem = null;
            });
            
            // Recalculate totals to apply discount
            CalculateTotals();
        }
    }

    // Clear customer selection
    private void OnClearCustomerClicked(object sender, EventArgs e)
    {
        _selectedCustomer = null;
        
        MainThread.BeginInvokeOnMainThread(() => {
            SelectedCustomerLabel.Text = "No member selected";
            ClearCustomerButton.IsVisible = false;
        });
        
        // Recalculate totals to remove discount
        CalculateTotals();
    }

    private async void OnCompletePaymentClicked(object sender, EventArgs e)
    {
        try
        {
            // Validate table selection
            if (_selectedTable == null)
            {
                await DisplayAlert("Validation Error", "Please select a table", "OK");
                return;
            }
            
            // Validate that order has items
            if (_orderItems.Count == 0)
            {
                await DisplayAlert("Validation Error", "Order has no items. Please add items before checkout.", "OK");
                return;
            }
            
            // Show loading indicator
            IsBusy = true;
            
            // Update order with new customer info and table info if changed
            bool orderUpdated = false;
            
            if (_currentOrder.MemberID != (_selectedCustomer?.MemberID ?? null))
            {
                _currentOrder.MemberID = _selectedCustomer?.MemberID;
                orderUpdated = true;
            }
            
            if (_currentOrder.TableID != _selectedTable.TableID)
            {
                _currentOrder.TableID = _selectedTable.TableID;
                orderUpdated = true;
            }
            
            // MODIFIED: Always set payment status to "Paid" after successful payment
            if (_currentOrder.PaymentStatus != "Paid")
            {
                _currentOrder.PaymentStatus = "Paid";
                orderUpdated = true;
            }
            
            if (orderUpdated)
            {
                await _orderRepository.UpdateAsync(_currentOrder);
            }
            
            // Create payment record
            var payment = new Payment
            {
                OrderID = _orderId,
                MemberID = _selectedCustomer?.MemberID,
                Total = _total,
                PaymentMethod = _selectedPaymentMethod,
                PaymentDate = DateTime.Now,
                PointsEarned = CalculatePointsEarned(_total),
                TableID = _selectedTable.TableID
            };
            
            // Save payment to database
            await _paymentRepository.InsertAsync(payment);
            
            // Update customer points if applicable
            if (_selectedCustomer != null)
            {
                int pointsEarned = CalculatePointsEarned(_total);
                _selectedCustomer.Points += pointsEarned;
                await _customerRepository.UpdateAsync(_selectedCustomer);
            }
            
            // Hide loading indicator
            IsBusy = false;
            
            // Show success message with appropriate details
            string successTitle = _isTakeAway ? "Take Away Order Completed" : "Payment Completed";
            string successMessage = $"Your order (#{_orderId}) has been processed successfully.\n\n" +
                                  $"Table: {_selectedTable.TableName}\n" +
                                  $"Payment Method: {_selectedPaymentMethod}\n" +
                                  $"Total: ฿{_total:F2}";
            
            if (_isTakeAway)
            {
                successMessage += "\n\nYour order will be ready for pickup shortly.";
            }
            
            await DisplayAlert(successTitle, successMessage, "OK");
            
            // Navigate back to orders
            await Shell.Current.GoToAsync("//orders");
        }
        catch (Exception ex)
        {
            IsBusy = false;
            Console.WriteLine($"Error in OnCompletePaymentClicked: {ex.Message}");
            await DisplayAlert("Error", $"Operation failed: {ex.Message}", "OK");
        }
    }
    
    private int CalculatePointsEarned(double amount)
    {
        // Simple points calculation: 1 point per dollar
        int points = (int)Math.Floor(amount);
        return points;
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        // Ask for confirmation if any fields are filled
        bool hasInput = _selectedCustomer != null ||
                        (_selectedTable != null && _selectedTable.TableName != "Take Away") ||
                        (_selectedPaymentMethod != "Cash");
        
        if (hasInput)
        {
            bool cancel = await DisplayAlert("Cancel Checkout", 
                "Are you sure you want to cancel? All entered information will be lost.", 
                "Yes", "No");
            
            if (!cancel) return;
        }
        
        // Navigate back
        await Shell.Current.GoToAsync("..");
    }
}
using StockApplication.Models;
using StockApplication.Repositories;
using StockApplication.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace StockApplication.Views;

[QueryProperty(nameof(OrderId), "id")]
public partial class OrderEditPage : ContentPage
{
    private readonly Repository<Order> _orderRepository;
    private readonly Repository<Staff> _staffRepository;
    private readonly Repository<MenuItem> _menuItemRepository;
    private readonly Repository<OrderDetail> _orderDetailRepository;
    private readonly Repository<Table> _tableRepository;
    private readonly Repository<Payment> _paymentRepository;
    
    private int _orderId;
    private bool _isNewOrder = true;
    private ObservableCollection<OrderItemViewModel> _orderItems;
    private ObservableCollection<Staff> _staffList = new();
    private ObservableCollection<Table> _tables = new();
    private double _totalAmount = 0;
    
    // Currently selected table
    private Table _selectedTable = null;
    private bool _isTakeAway = false;
    private string _selectedPaymentMethod = "Cash"; // Default payment method

    // Command properties for item operations
    public Command<OrderItemViewModel> IncreaseQuantityCommand { get; private set; }
    public Command<OrderItemViewModel> DecreaseQuantityCommand { get; private set; }
    public Command<OrderItemViewModel> RemoveItemCommand { get; private set; }

    // Payment method options
    private string[] _paymentMethods = { "Cash", "Mobile Banking", "QR Code" };

    public int OrderId
    {
        get => _orderId;
        set
        {
            _orderId = value;
            if (_orderId > 0)
            {
                _isNewOrder = false;
            }
        }
    }

    public OrderEditPage(
        Repository<Order> orderRepository,
        Repository<Staff> staffRepository,
        Repository<MenuItem> menuItemRepository,
        Repository<OrderDetail> orderDetailRepository,
        Repository<Table> tableRepository,
        Repository<Payment> paymentRepository)
    {
        InitializeComponent();
        
        _orderRepository = orderRepository;
        _staffRepository = staffRepository;
        _menuItemRepository = menuItemRepository;
        _orderDetailRepository = orderDetailRepository;
        _tableRepository = tableRepository;
        _paymentRepository = paymentRepository;
        
        // Initialize order items collection
        _orderItems = new ObservableCollection<OrderItemViewModel>();
        
        // Set up data sources
        OrderItemsCollection.ItemsSource = _orderItems;
        TablePicker.ItemsSource = _tables;
        
        // Initialize commands
        IncreaseQuantityCommand = new Command<OrderItemViewModel>(OnIncreaseQuantity);
        DecreaseQuantityCommand = new Command<OrderItemViewModel>(OnDecreaseQuantity);
        RemoveItemCommand = new Command<OrderItemViewModel>(OnRemoveItem);
        
        // Set binding context to this page for command binding
        this.BindingContext = this;
        
        // Subscribe to collection changed events
        InitializeCollectionSubscription();
        
        // Set default date to today
        OrderDatePicker.Date = DateTime.Now;
        
        // Set up event handlers
        AddItemButton.Clicked += OnAddItemClicked;
        SaveButton.Clicked += OnSaveClicked;
        CancelButton.Clicked += OnCancelClicked;
        TablePicker.SelectedIndexChanged += OnTableSelectionChanged;
        PaymentStatusPicker.SelectedIndexChanged += OnPaymentStatusChanged;
        
        // Create payment method section
        CreatePaymentMethodSection();
    }

    private void CreatePaymentMethodSection()
    {
        // Create a payment method section to be added to the order details frame
        var paymentMethodStackLayout = new VerticalStackLayout
        {
            Spacing = 15,
            IsVisible = false, // Initially hidden
        };
        
        // Add a label
        paymentMethodStackLayout.Add(new Label
        {
            Text = "Payment Method",
            FontSize = 14,
            TextColor = Color.FromArgb("#616161")
        });
        
        // Create a picker for payment methods
        var paymentMethodPicker = new Picker
        {
            Title = "Select payment method",
            ItemsSource = _paymentMethods
        };
        
        // Set default selection to Cash
        paymentMethodPicker.SelectedIndex = 0;
        
        // Handle selection changes
        paymentMethodPicker.SelectedIndexChanged += (sender, args) =>
        {
            if (paymentMethodPicker.SelectedIndex >= 0)
            {
                _selectedPaymentMethod = _paymentMethods[paymentMethodPicker.SelectedIndex];
            }
        };
        
        // Wrap picker in a frame for styling
        var pickerFrame = new Frame
        {
            Padding = new Thickness(0),
            HasShadow = false,
            BorderColor = Color.FromArgb("#E0E0E0"),
            CornerRadius = 5,
            Content = paymentMethodPicker
        };
        
        paymentMethodStackLayout.Add(pickerFrame);
        
        // Add the payment method section to the layout
        // Note: We need to find the order details frame and add it there
        var orderDetailsFrame = Content.FindByName<Frame>("OrderDetailsFrame");
        var orderDetailsLayout = orderDetailsFrame?.Content as VerticalStackLayout;
        
        if (orderDetailsLayout != null)
        {
            orderDetailsLayout.Add(paymentMethodStackLayout);
        }
        
        // Save a reference to the payment method section for later visibility toggling
        PaymentMethodSection = paymentMethodStackLayout;
    }
    
    // Reference to the payment method section for showing/hiding
    private VerticalStackLayout PaymentMethodSection;

    private void InitializeCollectionSubscription()
    {
        // Subscribe to collection changed events to update total automatically
        _orderItems.CollectionChanged += (sender, e) => UpdateOrderTotal();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Initialize the UI
        await InitializeUIAsync();
        
        // Load order data if in edit mode
        if (!_isNewOrder)
        {
            await LoadOrderAsync();
        }
        
        // Ensure the total is displayed correctly
        UpdateOrderTotal();
    }
    
    private async Task InitializeUIAsync()
    {
        try
        {
            // Show loading indicator
            IsBusy = true;
            
            // Set page title
            if (_isNewOrder)
            {
                PageTitleLabel.Text = "Create New Order";
                Title = "New Order";
            }
            else
            {
                PageTitleLabel.Text = "Edit Order";
                Title = "Edit Order";
            }
            
            // Load staff members
            var staffList = await _staffRepository.GetAllAsync();
            _staffList.Clear();
            foreach (var staff in staffList)
            {
                _staffList.Add(staff);
            }
            StaffPicker.ItemsSource = _staffList;
            
            // Load tables
            await LoadTablesAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error initializing page: {ex.Message}", "OK");
        }
        finally
        {
            // Hide loading indicator
            IsBusy = false;
        }
    }
    
    private async Task LoadTablesAsync()
    {
        try
        {
            // Get all tables
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
            
            // Update UI
            _tables.Clear();
            foreach (var table in sortedTables)
            {
                _tables.Add(table);
            }

            Debug.WriteLine($"Loaded {_tables.Count} tables");
            foreach (var table in _tables)
            {
                Debug.WriteLine($"Table ID: {table.TableID}, Name: {table.TableName}");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load tables: {ex.Message}", "OK");
        }
    }
    
    private void OnTableSelectionChanged(object sender, EventArgs e)
    {
        if (TablePicker.SelectedItem is Table table)
        {
            _selectedTable = table;
            _isTakeAway = table.TableName == "Take Away";
            
            // Update UI based on table type
            UpdateUIForTableSelection();
            
            Debug.WriteLine($"Table selected: {table.TableName}, IsTakeAway: {_isTakeAway}");
        }
    }
    
    private void OnPaymentStatusChanged(object sender, EventArgs e)
    {
        // Show or hide payment method section based on status
        if (PaymentStatusPicker.SelectedItem?.ToString() == "Paid")
        {
            // Show payment method section when status is Paid
            if (PaymentMethodSection != null)
            {
                PaymentMethodSection.IsVisible = true;
            }
        }
        else
        {
            // Hide payment method section for other statuses
            if (PaymentMethodSection != null)
            {
                PaymentMethodSection.IsVisible = false;
            }
        }
    }

    private void UpdateUIForTableSelection()
    {
        if (_isTakeAway)
        {
            // For Take Away, set status to "Paid" and disable the picker
            int paidIndex = -1;
            var statusItems = PaymentStatusPicker.ItemsSource as IList<string>;
            if (statusItems != null)
            {
                paidIndex = statusItems.IndexOf("Paid");
                if (paidIndex >= 0)
                {
                    PaymentStatusPicker.SelectedIndex = paidIndex;
                }
            }
            PaymentStatusPicker.IsEnabled = false;
            
            // Make payment method section visible
            if (PaymentMethodSection != null)
            {
                PaymentMethodSection.IsVisible = true;
            }
        }
        else
        {
            // For regular tables, set status to "Pending" and enable the picker
            int pendingIndex = -1;
            var statusItems = PaymentStatusPicker.ItemsSource as IList<string>;
            if (statusItems != null)
            {
                pendingIndex = statusItems.IndexOf("Pending");
                if (pendingIndex >= 0)
                {
                    PaymentStatusPicker.SelectedIndex = pendingIndex;
                }
            }
            PaymentStatusPicker.IsEnabled = true;
            
            // Hide payment method section for pending status
            if (PaymentMethodSection != null)
            {
                PaymentMethodSection.IsVisible = false;
            }
        }
    }

    private async Task LoadOrderAsync()
    {
        try
        {
            // Show loading indicator
            IsBusy = true;
            
            var order = await _orderRepository.GetByIdAsync(_orderId);
            if (order != null)
            {
                // Set order details
                OrderDatePicker.Date = order.OrderDate;
                
                // Set payment status
                if (!string.IsNullOrEmpty(order.PaymentStatus))
                {
                    var statusItems = PaymentStatusPicker.ItemsSource as IList<string>;
                    if (statusItems != null)
                    {
                        int statusIndex = statusItems.IndexOf(order.PaymentStatus);
                        if (statusIndex >= 0)
                        {
                            PaymentStatusPicker.SelectedIndex = statusIndex;
                            
                            // Show/hide payment method section based on status
                            if (PaymentMethodSection != null)
                            {
                                PaymentMethodSection.IsVisible = order.PaymentStatus == "Paid";
                            }
                        }
                    }
                }
                
                // Load notes if available
                try {
                    NotesEditor.Text = order.Notes;
                } catch {
                    // Notes property might not exist yet
                }
                
                // Set staff picker
                if (order.StaffID.HasValue)
                {
                    int staffIndex = -1;
                    for (int i = 0; i < _staffList.Count; i++)
                    {
                        if (_staffList[i].StaffID == order.StaffID.Value)
                        {
                            staffIndex = i;
                            break;
                        }
                    }
                    
                    if (staffIndex >= 0)
                    {
                        StaffPicker.SelectedIndex = staffIndex;
                    }
                }
                
                // Set table picker
                if (order.TableID.HasValue)
                {
                    int tableIndex = -1;
                    for (int i = 0; i < _tables.Count; i++)
                    {
                        if (_tables[i].TableID == order.TableID.Value)
                        {
                            tableIndex = i;
                            _selectedTable = _tables[i];
                            // Check if it's a Take Away table
                            _isTakeAway = _selectedTable.TableName == "Take Away";
                            break;
                        }
                    }
                    
                    if (tableIndex >= 0)
                    {
                        TablePicker.SelectedIndex = tableIndex;
                        
                        // Update UI based on table type
                        UpdateUIForTableSelection();
                    }
                }
                
                // Load payment method if this is a paid order
                if (order.PaymentStatus == "Paid")
                {
                    var payment = await _paymentRepository.FindAsync(p => p.OrderID == _orderId);
                    if (payment != null && payment.Any())
                    {
                        // Get the payment method from existing payment
                        _selectedPaymentMethod = payment.First().PaymentMethod;
                        
                        // Set the payment method picker (find it within the PaymentMethodSection)
                        var paymentMethodPicker = PaymentMethodSection?.Children
                            .OfType<Frame>()
                            .FirstOrDefault()?.Content as Picker;
                            
                        if (paymentMethodPicker != null)
                        {
                            int methodIndex = Array.IndexOf(_paymentMethods, _selectedPaymentMethod);
                            if (methodIndex >= 0)
                            {
                                paymentMethodPicker.SelectedIndex = methodIndex;
                            }
                        }
                    }
                }
                
                // Load order items
                await LoadOrderItemsAsync();
            }
        }
        catch (Exception ex)
        {
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
            // Get order details
            var orderDetails = await _orderDetailRepository.FindAsync(od => od.OrderID == _orderId);
            
            // Get menu items
            var menuItems = await _menuItemRepository.GetAllAsync();
            
            // Clear current items
            MainThread.BeginInvokeOnMainThread(() => {
                _orderItems.Clear();
            
                // Add items to collection
                foreach (var detail in orderDetails)
                {
                    var menuItem = menuItems.FirstOrDefault(m => m.MenuID == detail.MenuID);
                    if (menuItem != null)
                    {
                        var orderItem = new OrderItemViewModel
                        {
                            MenuID = menuItem.MenuID,
                            Menu_Name = menuItem.Menu_Name,
                            Price = menuItem.Price,
                            Quantity = detail.Quantity
                        };
                        
                        // Calculate subtotal directly
                        orderItem.Subtotal = orderItem.Price * orderItem.Quantity;
                        
                        _orderItems.Add(orderItem);
                    }
                }
            });
            
            // Update the total
            UpdateOrderTotal();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load order items: {ex.Message}", "OK");
        }
    }

    private async void OnAddItemClicked(object sender, EventArgs e)
    {
        try
        {
            // Show loading indicator for heavy operations
            IsBusy = true;
            
            // Get all menu items
            var menuItems = await _menuItemRepository.GetAllAsync();
            
            // Hide loading indicator
            IsBusy = false;
            
            // Create list of item names for picker - REMOVED "Create New Menu Item" option
            var itemNamesList = menuItems.Select(m => m.Menu_Name).ToList();
            var itemNames = itemNamesList.ToArray();
            
            // Show action sheet to select item
            string result = await DisplayActionSheet("Select Menu Item", "Cancel", null, itemNames);
            
            if (result != "Cancel" && !string.IsNullOrEmpty(result))
            {
                // Find selected menu item
                var selectedItem = menuItems.FirstOrDefault(m => m.Menu_Name == result);
                if (selectedItem != null)
                {
                    // Check if item already in order
                    var existingItem = _orderItems.FirstOrDefault(i => i.MenuID == selectedItem.MenuID);
                    if (existingItem != null)
                    {
                        // Increment quantity
                        existingItem.Quantity++;
                        // Calculate subtotal directly
                        existingItem.Subtotal = existingItem.Price * existingItem.Quantity;
                    }
                    else
                    {
                        // Add new item
                        var newOrderItem = new OrderItemViewModel
                        {
                            MenuID = selectedItem.MenuID,
                            Menu_Name = selectedItem.Menu_Name,
                            Price = selectedItem.Price,
                            Quantity = 1
                        };
                        
                        // Calculate subtotal directly
                        newOrderItem.Subtotal = newOrderItem.Price * newOrderItem.Quantity;
                        
                        _orderItems.Add(newOrderItem);
                    }
                    
                    // Update total
                    UpdateOrderTotal();
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to add item: {ex.Message}", "OK");
            IsBusy = false;
        }
    }
    
    // Command handlers for order items
    private void OnIncreaseQuantity(OrderItemViewModel item)
    {
        if (item != null)
        {
            item.Quantity++;
            // Calculate subtotal directly
            item.Subtotal = item.Price * item.Quantity;
            
            // Update order total
            UpdateOrderTotal();
        }
    }

    private void OnDecreaseQuantity(OrderItemViewModel item)
    {
        if (item != null)
        {
            if (item.Quantity > 1)
            {
                item.Quantity--;
                // Calculate subtotal directly
                item.Subtotal = item.Price * item.Quantity;
                
                // Update order total
                UpdateOrderTotal();
            }
            else
            {
                // If quantity would become 0, remove the item instead
                _orderItems.Remove(item);
                // Update order total - handled by collection changed event
            }
        }
    }

    private void OnRemoveItem(OrderItemViewModel item)
    {
        if (item != null)
        {
            _orderItems.Remove(item);
            // Update order total - handled by collection changed event
        }
    }

    private void UpdateOrderTotal()
    {
        // Calculate subtotals for all items
        foreach (var item in _orderItems)
        {
            item.Subtotal = item.Price * item.Quantity;
        }
        
        _totalAmount = _orderItems.Sum(i => i.Subtotal);
        
        MainThread.BeginInvokeOnMainThread(() => {
            TotalLabel.Text = $"Total: ฿{_totalAmount:F2}";
        });
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            // Validate staff selection
            if (StaffPicker.SelectedItem == null)
            {
                await DisplayAlert("Validation Error", "Please select a staff member.", "OK");
                return;
            }
            
            // Validate table selection
            if (TablePicker.SelectedItem == null)
            {
                await DisplayAlert("Validation Error", "Please select a table.", "OK");
                return;
            }
            
            if (_orderItems.Count == 0)
            {
                await DisplayAlert("Validation Error", "Please add at least one item to the order.", "OK");
                return;
            }
            
            // Get payment status
            string paymentStatus = PaymentStatusPicker.SelectedItem?.ToString() ?? "Pending";
            
            // For Paid status, validate payment method selection
            if (paymentStatus == "Paid")
            {
                // Make sure we have a payment method
                if (string.IsNullOrEmpty(_selectedPaymentMethod))
                {
                    await DisplayAlert("Validation Error", "Please select a payment method.", "OK");
                    return;
                }
            }
            
            // Show loading indicator
            IsBusy = true;
            
            // Get selected staff and table
            var selectedStaff = StaffPicker.SelectedItem as Staff;
            var selectedTable = TablePicker.SelectedItem as Table;
            
            // Determine payment status based on table selection
            if (_isTakeAway)
            {
                // Take Away orders are always marked as "Paid"
                paymentStatus = "Paid";
            }
            
            // Create/update order
            var order = new Order
            {
                OrderID = _orderId,
                OrderDate = OrderDatePicker.Date,
                PaymentStatus = paymentStatus,
                TotalAmount = _totalAmount,
                StaffID = selectedStaff?.StaffID,
                MemberID = null, // No customer selection
                Notes = NotesEditor.Text,
                TableID = selectedTable?.TableID  // Save the selected table
            };
            
            if (_isNewOrder)
            {
                // Insert new order
                await _orderRepository.InsertAsync(order);
                _orderId = order.OrderID; // Get the newly created ID
            }
            else
            {
                // Update existing order
                await _orderRepository.UpdateAsync(order);
                
                // Delete existing order details
                var existingDetails = await _orderDetailRepository.FindAsync(od => od.OrderID == _orderId);
                foreach (var detail in existingDetails)
                {
                    await _orderDetailRepository.DeleteAsync(detail);
                }
            }
            
            // Save order details
            foreach (var item in _orderItems)
            {
                var orderDetail = new OrderDetail
                {
                    OrderID = _orderId,
                    MenuID = item.MenuID,
                    Quantity = item.Quantity,
                    Subtotal = item.Subtotal
                };
                
                await _orderDetailRepository.InsertAsync(orderDetail);
            }
            
            // Handle payment record for Paid status
            if (paymentStatus == "Paid")
            {
                // Check if payment already exists
                var existingPayment = await _paymentRepository.FindAsync(p => p.OrderID == _orderId);
                
                if (existingPayment == null || !existingPayment.Any())
                {
                    // Create new payment record
                    var payment = new Payment
                    {
                        OrderID = _orderId,
                        MemberID = null, // No member for now
                        PaymentMethod = _selectedPaymentMethod,
                        PaymentDate = DateTime.Now,
                        Total = _totalAmount,
                        PointsEarned = 0, // No points for now
                        TableID = selectedTable?.TableID
                    };
                    
                    await _paymentRepository.InsertAsync(payment);
                    Debug.WriteLine($"Created payment record for order #{_orderId} with method {_selectedPaymentMethod}");
                }
                else
                {
                    // Update existing payment
                    var payment = existingPayment.First();
                    payment.Total = _totalAmount;
                    payment.PaymentDate = DateTime.Now;
                    payment.TableID = selectedTable?.TableID;
                    payment.PaymentMethod = _selectedPaymentMethod;
                    
                    await _paymentRepository.UpdateAsync(payment);
                    Debug.WriteLine($"Updated payment record for order #{_orderId} with method {_selectedPaymentMethod}");
                }
            }
            
            // Hide loading indicator
            IsBusy = false;
            
            // Prepare success message
            string message = _isNewOrder ? "Order created successfully." : "Order updated successfully.";
            if (_isTakeAway)
            {
                message += " Take Away order has been marked as paid automatically.";
            }
            else if (paymentStatus == "Paid")
            {
                message += $" Payment completed using {_selectedPaymentMethod}.";
            }
            
            await DisplayAlert("Success", message, "OK");
            
            // Navigate back
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            IsBusy = false;
            await DisplayAlert("Error", $"Failed to save order: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        // Confirm cancel if there are changes
        bool hasChanges = _orderItems.Count > 0 || 
                         StaffPicker.SelectedItem != null || 
                         TablePicker.SelectedItem != null ||
                         !string.IsNullOrEmpty(NotesEditor.Text);
        
        if (hasChanges)
        {
            bool cancel = await DisplayAlert("Confirm", 
                "Are you sure you want to cancel? Any unsaved changes will be lost.", 
                "Yes", "No");
            
            if (!cancel)
                return;
        }
        
        // Navigate back
        await Shell.Current.GoToAsync("..");
    }
}
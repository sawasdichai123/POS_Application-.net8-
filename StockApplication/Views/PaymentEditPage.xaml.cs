using StockApplication.Models;
using StockApplication.Repositories;

namespace StockApplication.Views;

[QueryProperty(nameof(OrderId), "orderId")]
public partial class PaymentEditPage : ContentPage
{
    private readonly Repository<Payment> _paymentRepository;
    private readonly Repository<Order> _orderRepository;
    private readonly Repository<Customer> _customerRepository;
    
    private int _orderId;
    private Order _currentOrder;
    private Customer _currentCustomer;

    public int OrderId
    {
        get => _orderId;
        set
        {
            _orderId = value;
            if (_orderId > 0)
            {
                // Load order details
                OrderNumberEntry.Text = _orderId.ToString();
                LoadOrderDetailsAsync(_orderId);
            }
        }
    }

    public PaymentEditPage(
        Repository<Payment> paymentRepository,
        Repository<Order> orderRepository,
        Repository<Customer> customerRepository)
    {
        InitializeComponent();
        
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        
        // Set default date and time to now
        PaymentDatePicker.Date = DateTime.Now;
        PaymentTimePicker.Time = DateTime.Now.TimeOfDay;
        
        // Set up event handlers
        FindOrderButton.Clicked += OnFindOrderClicked;
        CalculatePointsButton.Clicked += OnCalculatePointsClicked;
        SaveButton.Clicked += OnSaveClicked;
        CancelButton.Clicked += OnCancelClicked;
        
        // Default payment method
        PaymentMethodPicker.SelectedIndex = 0; // Cash
    }

    private async void OnFindOrderClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(OrderNumberEntry.Text) || !int.TryParse(OrderNumberEntry.Text, out int orderId))
        {
            await DisplayAlert("Invalid Order", "Please enter a valid order number.", "OK");
            return;
        }
        
        await LoadOrderDetailsAsync(orderId);
    }

    private async Task LoadOrderDetailsAsync(int orderId)
    {
        try
        {
            // Find the order
            _currentOrder = await _orderRepository.GetByIdAsync(orderId);
            
            if (_currentOrder == null)
            {
                // Order not found
                await DisplayAlert("Order Not Found", $"Order #{orderId} was not found.", "OK");
                OrderDetailsPanel.IsVisible = false;
                _currentCustomer = null;
                return;
            }
            
            // Display order details
            OrderDateLabel.Text = _currentOrder.OrderDate.ToString("MMM dd, yyyy");
            OrderTotalLabel.Text = $"${_currentOrder.TotalAmount:F2}";
            
            // Set the amount to the order total
            AmountEntry.Text = _currentOrder.TotalAmount.ToString();
            
            // Load customer info if available
            if (_currentOrder.MemberID.HasValue)
            {
                _currentCustomer = await _customerRepository.GetByIdAsync(_currentOrder.MemberID.Value);
                if (_currentCustomer != null)
                {
                    CustomerLabel.Text = _currentCustomer.Cus_Name;
                }
                else
                {
                    CustomerLabel.Text = "Unknown Customer";
                    _currentCustomer = null;
                }
            }
            else
            {
                CustomerLabel.Text = "Guest";
                _currentCustomer = null;
            }
            
            // Show the details panel
            OrderDetailsPanel.IsVisible = true;
            
            // Calculate points
            CalculatePoints();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load order details: {ex.Message}", "OK");
        }
    }

    private void OnCalculatePointsClicked(object sender, EventArgs e)
    {
        CalculatePoints();
    }

    private void CalculatePoints()
    {
        if (string.IsNullOrWhiteSpace(AmountEntry.Text) || !double.TryParse(AmountEntry.Text, out double amount))
        {
            return;
        }
        
        // Simple points calculation: 1 point per $1 spent
        int points = (int)Math.Floor(amount);
        
        // Apply bonus points if the customer exists
        if (_currentCustomer != null)
        {
            // Additional bonus for customers with more than 100 points (loyalty bonus)
            if (_currentCustomer.Points >= 100)
            {
                points = (int)(points * 1.1); // 10% bonus
            }
        }
        
        PointsEntry.Text = points.ToString();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(OrderNumberEntry.Text) || !int.TryParse(OrderNumberEntry.Text, out int orderId))
        {
            await DisplayAlert("Validation Error", "Please enter a valid order number.", "OK");
            return;
        }
        
        if (_currentOrder == null)
        {
            await DisplayAlert("Validation Error", "Please find a valid order first.", "OK");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(AmountEntry.Text) || !double.TryParse(AmountEntry.Text, out double amount))
        {
            await DisplayAlert("Validation Error", "Please enter a valid payment amount.", "OK");
            return;
        }
        
        if (PaymentMethodPicker.SelectedIndex < 0)
        {
            await DisplayAlert("Validation Error", "Please select a payment method.", "OK");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(PointsEntry.Text) || !int.TryParse(PointsEntry.Text, out int points))
        {
            points = 0;
        }
        
        try
        {
            // Create payment object
            var payment = new Payment
            {
                OrderID = _currentOrder.OrderID,
                MemberID = _currentOrder.MemberID,
                Total = amount,
                PaymentMethod = PaymentMethodPicker.SelectedItem.ToString(),
                PointsEarned = points,
                PaymentDate = PaymentDatePicker.Date.Add(PaymentTimePicker.Time)
            };
            
            // Save payment
            await _paymentRepository.InsertAsync(payment);
            
            // Update order payment status
            _currentOrder.PaymentStatus = "Paid";
            await _orderRepository.UpdateAsync(_currentOrder);
            
            // Update customer points if applicable
            if (_currentCustomer != null && points > 0)
            {
                _currentCustomer.Points += points;
                await _customerRepository.UpdateAsync(_currentCustomer);
            }
            
            await DisplayAlert("Success", "Payment recorded successfully.", "OK");
            
            // Navigate back
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save payment: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        // Confirm cancel if there are changes
        bool hasChanges = !string.IsNullOrEmpty(OrderNumberEntry.Text) || 
                         !string.IsNullOrEmpty(AmountEntry.Text) ||
                         PaymentMethodPicker.SelectedIndex >= 0;
        
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
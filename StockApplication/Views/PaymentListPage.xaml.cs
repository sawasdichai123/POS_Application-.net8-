using StockApplication.Models;
using StockApplication.Repositories;
using System.Collections.ObjectModel;

namespace StockApplication.Views;

public partial class PaymentListPage : ContentPage
{
    private readonly Repository<Payment> _paymentRepository;
    private readonly Repository<Order> _orderRepository;
    private readonly Repository<Customer> _customerRepository;
    
    private ObservableCollection<PaymentViewModel> _payments = new();
    private Dictionary<int, string> _customerNames = new();
    private string _currentMethod = "All";

    public PaymentListPage(
        Repository<Payment> paymentRepository,
        Repository<Order> orderRepository,
        Repository<Customer> customerRepository)
    {
        InitializeComponent();
        
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        
        // Set up collection view
        PaymentCollectionView.ItemsSource = _payments;
        
        // Set up refresh view
        PaymentRefreshView.Command = new Command(async () => await LoadPaymentsAsync());
        
        // Set up event handlers
        AddPaymentButton.Clicked += OnAddPaymentClicked;
        ApplyFilterButton.Clicked += OnApplyFilterClicked;
        
        // Payment method filters
        AllMethodButton.Clicked += (s, e) => FilterByPaymentMethod("All");
        CashMethodButton.Clicked += (s, e) => FilterByPaymentMethod("Cash");
        CreditCardMethodButton.Clicked += (s, e) => FilterByPaymentMethod("Credit Card");
        OtherMethodButton.Clicked += (s, e) => FilterByPaymentMethod("Other");
        
        // Set default date range (last 30 days)
        StartDatePicker.Date = DateTime.Now.AddDays(-30);
        EndDatePicker.Date = DateTime.Now;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Load customer names for reference
        await LoadCustomerNamesAsync();
        
        // Load payments
        await LoadPaymentsAsync();
    }

    private async Task LoadCustomerNamesAsync()
    {
        try
        {
            // Get all customers
            var customers = await _customerRepository.GetAllAsync();
            
            // Store in dictionary for quick lookup
            _customerNames.Clear();
            foreach (var customer in customers)
            {
                _customerNames[customer.MemberID] = customer.Cus_Name;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load customer data: {ex.Message}", "OK");
        }
    }

    private async Task LoadPaymentsAsync()
    {
        try
        {
            PaymentRefreshView.IsRefreshing = true;
            
            // Get all payments
            var allPayments = await _paymentRepository.GetAllAsync();
            
            // Apply date filters
            var startDate = StartDatePicker.Date.Date;
            var endDate = EndDatePicker.Date.Date.AddDays(1).AddSeconds(-1); // End of the selected day
            
            var filteredPayments = allPayments.Where(p => 
                p.PaymentDate >= startDate && 
                p.PaymentDate <= endDate);
            
            // Apply method filter if not "All"
            if (_currentMethod != "All")
            {
                filteredPayments = filteredPayments.Where(p => p.PaymentMethod == _currentMethod);
            }
            
            // Convert to view models
            _payments.Clear();
            double totalRevenue = 0;
            int totalPoints = 0;
            
            foreach (var payment in filteredPayments.OrderByDescending(p => p.PaymentDate))
            {
                string customerName = "Guest";
                if (payment.MemberID.HasValue && _customerNames.ContainsKey(payment.MemberID.Value))
                {
                    customerName = _customerNames[payment.MemberID.Value];
                }
                
                _payments.Add(new PaymentViewModel
                {
                    PaymentID = payment.PaymentID,
                    OrderID = payment.OrderID,
                    CustomerName = customerName,
                    PaymentDate = payment.PaymentDate,
                    Total = payment.Total,
                    PaymentMethod = payment.PaymentMethod,
                    PointsEarned = payment.PointsEarned
                });
                
                // Add to totals
                totalRevenue += payment.Total;
                totalPoints += payment.PointsEarned;
            }
            
            // Update summary labels
            TotalPaymentsLabel.Text = _payments.Count.ToString();
            TotalRevenueLabel.Text = $"${totalRevenue:F2}";
            TotalPointsLabel.Text = totalPoints.ToString();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load payments: {ex.Message}", "OK");
        }
        finally
        {
            PaymentRefreshView.IsRefreshing = false;
        }
    }

    private void UpdateMethodButtonStyles(string selectedMethod)
    {
        // Reset all buttons
        AllMethodButton.BackgroundColor = Color.Parse("#E0E0E0");
        AllMethodButton.TextColor = Color.Parse("#616161");
        
        CashMethodButton.BackgroundColor = Color.Parse("#E0E0E0");
        CashMethodButton.TextColor = Color.Parse("#616161");
        
        CreditCardMethodButton.BackgroundColor = Color.Parse("#E0E0E0");
        CreditCardMethodButton.TextColor = Color.Parse("#616161");
        
        OtherMethodButton.BackgroundColor = Color.Parse("#E0E0E0");
        OtherMethodButton.TextColor = Color.Parse("#616161");
        
        // Set selected button
        Color selectedColor = Color.Parse("#3F51B5");
        Color selectedTextColor = Colors.White;
        
        switch (selectedMethod)
        {
            case "All":
                AllMethodButton.BackgroundColor = selectedColor;
                AllMethodButton.TextColor = selectedTextColor;
                break;
            case "Cash":
                CashMethodButton.BackgroundColor = selectedColor;
                CashMethodButton.TextColor = selectedTextColor;
                break;
            case "Credit Card":
                CreditCardMethodButton.BackgroundColor = selectedColor;
                CreditCardMethodButton.TextColor = selectedTextColor;
                break;
            case "Other":
                OtherMethodButton.BackgroundColor = selectedColor;
                OtherMethodButton.TextColor = selectedTextColor;
                break;
        }
    }

    private async void FilterByPaymentMethod(string method)
    {
        _currentMethod = method;
        UpdateMethodButtonStyles(method);
        await LoadPaymentsAsync();
    }

    private async void OnAddPaymentClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("Payments/Add");
    }

    private async void OnApplyFilterClicked(object sender, EventArgs e)
    {
        await LoadPaymentsAsync();
    }

    private async void OnViewOrderClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is PaymentViewModel payment)
        {
            // Navigate to order details
            await Shell.Current.GoToAsync($"Orders/Edit?id={payment.OrderID}");
        }
    }
}

public class PaymentViewModel
{
    public int PaymentID { get; set; }
    public int OrderID { get; set; }
    public string CustomerName { get; set; }
    public DateTime PaymentDate { get; set; }
    public double Total { get; set; }
    public string PaymentMethod { get; set; }
    public int PointsEarned { get; set; }
}
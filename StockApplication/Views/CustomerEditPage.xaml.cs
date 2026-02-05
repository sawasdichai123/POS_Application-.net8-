using StockApplication.Models;
using StockApplication.Repositories;

namespace StockApplication.Views;

[QueryProperty(nameof(CustomerId), "id")]
public partial class CustomerEditPage : ContentPage
{
    private readonly Repository<Customer> _customerRepository;
    private int _customerId;
    private bool _isNewCustomer = true;

    public int CustomerId
    {
        get => _customerId;
        set
        {
            _customerId = value;
            LoadCustomer();
        }
    }

    public CustomerEditPage(Repository<Customer> customerRepository)
    {
        InitializeComponent();
        
        _customerRepository = customerRepository;
        
        // Set default date to today minus 20 years
        DateOfBirthPicker.Date = DateTime.Now.AddYears(-20);
        
        // Set up event handlers
        SaveButton.Clicked += OnSaveClicked;
        CancelButton.Clicked += OnCancelClicked;
    }

    private async void LoadCustomer()
    {
        if (_customerId <= 0)
        {
            // New customer
            _isNewCustomer = true;
            PageTitleLabel.Text = "Add New Customer";
            Title = "Add Customer";
            return;
        }
        
        try
        {
            // Existing customer
            _isNewCustomer = false;
            PageTitleLabel.Text = "Edit Customer";
            Title = "Edit Customer";
            
            var customer = await _customerRepository.GetByIdAsync(_customerId);
            if (customer != null)
            {
                // Populate the form
                CustomerNameEntry.Text = customer.Cus_Name;
                PhoneEntry.Text = customer.Cus_Phone;
                EmailEntry.Text = customer.Email;
                PointsEntry.Text = customer.Points.ToString();
                
                if (customer.DateOfBirth.HasValue)
                {
                    DateOfBirthPicker.Date = customer.DateOfBirth.Value;
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load customer: {ex.Message}", "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(CustomerNameEntry.Text))
        {
            await DisplayAlert("Validation Error", "Customer name is required.", "OK");
            return;
        }
        
        // Validate points
        int points = 0;
        if (!string.IsNullOrWhiteSpace(PointsEntry.Text) && !int.TryParse(PointsEntry.Text, out points))
        {
            await DisplayAlert("Validation Error", "Please enter a valid number for points.", "OK");
            return;
        }
        
        try
        {
            // Create customer object
            var customer = new Customer
            {
                MemberID = _customerId,
                Cus_Name = CustomerNameEntry.Text.Trim(),
                Cus_Phone = PhoneEntry.Text?.Trim(),
                Email = EmailEntry.Text?.Trim(),
                DateOfBirth = DateOfBirthPicker.Date,
                Points = points
            };
            
            if (_isNewCustomer)
            {
                // Insert new customer
                await _customerRepository.InsertAsync(customer);
                await DisplayAlert("Success", "Customer added successfully.", "OK");
            }
            else
            {
                // Update existing customer
                await _customerRepository.UpdateAsync(customer);
                await DisplayAlert("Success", "Customer updated successfully.", "OK");
            }
            
            // Navigate back
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save customer: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        // Confirm cancel if there are changes
        bool hasChanges = !string.IsNullOrEmpty(CustomerNameEntry.Text) || 
                         !string.IsNullOrEmpty(PhoneEntry.Text) ||
                         !string.IsNullOrEmpty(EmailEntry.Text) ||
                         !string.IsNullOrEmpty(PointsEntry.Text);
        
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
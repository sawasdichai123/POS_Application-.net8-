using StockApplication.Models;
using StockApplication.Repositories;

namespace StockApplication.Views;

[QueryProperty(nameof(StaffId), "id")]
public partial class StaffEditPage : ContentPage
{
    private readonly Repository<Staff> _staffRepository;
    private int _staffId;
    private bool _isNewStaff = true;

    // Predefined position options for the picker
    private readonly List<string> _positionOptions = new List<string>
    {
        "Manager",
        "Cashier",
        "Head Chef",
        "Sous Chef"
        
    };

    public int StaffId
    {
        get => _staffId;
        set
        {
            _staffId = value;
            LoadStaff();
        }
    }

    public StaffEditPage(Repository<Staff> staffRepository)
    {
        InitializeComponent();
        
        _staffRepository = staffRepository;
        
        // Set default hire date to today
        HireDatePicker.Date = DateTime.Now;
        
        // Populate position picker
        foreach (var position in _positionOptions)
        {
            PositionPicker.Items.Add(position);
        }
        
        // Set up event handlers
        SaveButton.Clicked += OnSaveClicked;
        CancelButton.Clicked += OnCancelClicked;
    }

    private async void LoadStaff()
    {
        if (_staffId <= 0)
        {
            // New staff member
            _isNewStaff = true;
            PageTitleLabel.Text = "Add New Staff Member";
            Title = "Add Staff";
            return;
        }
        
        try
        {
            // Existing staff member
            _isNewStaff = false;
            PageTitleLabel.Text = "Edit Staff Member";
            Title = "Edit Staff";
            
            var staff = await _staffRepository.GetByIdAsync(_staffId);
            if (staff != null)
            {
                // Populate the form
                StaffNameEntry.Text = staff.Staff_Name;
                
                // Set position in picker if it exists in the options list
                if (!string.IsNullOrEmpty(staff.Position))
                {
                    int positionIndex = _positionOptions.IndexOf(staff.Position);
                    if (positionIndex >= 0)
                    {
                        PositionPicker.SelectedIndex = positionIndex;
                    }
                }
                
                PhoneEntry.Text = staff.Staff_Phone;
                SalaryEntry.Text = staff.Salary?.ToString() ?? "";
                
                if (staff.HireDate.HasValue)
                {
                    HireDatePicker.Date = staff.HireDate.Value;
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load staff member: {ex.Message}", "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(StaffNameEntry.Text))
        {
            await DisplayAlert("Validation Error", "Staff name is required.", "OK");
            return;
        }
        
        // Validate salary (if provided)
        double? salary = null;
        if (!string.IsNullOrWhiteSpace(SalaryEntry.Text))
        {
            if (!double.TryParse(SalaryEntry.Text, out double parsedSalary))
            {
                await DisplayAlert("Validation Error", "Please enter a valid salary amount.", "OK");
                return;
            }
            salary = parsedSalary;
        }
        
        try
        {
            // Get selected position
            string position = null;
            if (PositionPicker.SelectedIndex >= 0)
            {
                position = _positionOptions[PositionPicker.SelectedIndex];
            }
            
            // Create staff object
            var staff = new Staff
            {
                StaffID = _staffId,
                Staff_Name = StaffNameEntry.Text.Trim(),
                Position = position,
                Staff_Phone = PhoneEntry.Text?.Trim(),
                Salary = salary,
                HireDate = HireDatePicker.Date
            };
            
            if (_isNewStaff)
            {
                // Insert new staff
                await _staffRepository.InsertAsync(staff);
                await DisplayAlert("Success", "Staff member added successfully.", "OK");
            }
            else
            {
                // Update existing staff
                await _staffRepository.UpdateAsync(staff);
                await DisplayAlert("Success", "Staff member updated successfully.", "OK");
            }
            
            // Navigate back
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save staff member: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        // Confirm cancel if there are changes
        bool hasChanges = !string.IsNullOrEmpty(StaffNameEntry.Text) || 
                         PositionPicker.SelectedIndex >= 0 ||
                         !string.IsNullOrEmpty(PhoneEntry.Text) ||
                         !string.IsNullOrEmpty(SalaryEntry.Text);
        
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
    
    private void OnIncrementSalaryClicked(object sender, EventArgs e)
    {
        // Get current value or default to 0
        if (!double.TryParse(SalaryEntry.Text, out double currentValue))
        {
            currentValue = 0;
        }
        
        // Increment by 1000 (can be adjusted as needed)
        currentValue += 1000;
        
        // Update the salary entry with formatted value
        SalaryEntry.Text = currentValue.ToString("F2");
    }
    
    private void OnDecrementSalaryClicked(object sender, EventArgs e)
    {
        // Get current value or default to 0
        if (!double.TryParse(SalaryEntry.Text, out double currentValue))
        {
            currentValue = 0;
        }
        
        // Decrement by 1000 (can be adjusted as needed)
        currentValue = Math.Max(0, currentValue - 1000); // Ensure it doesn't go below 0
        
        // Update the salary entry with formatted value
        SalaryEntry.Text = currentValue.ToString("F2");
    }
}
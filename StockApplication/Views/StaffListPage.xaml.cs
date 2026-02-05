using StockApplication.Models;
using StockApplication.Repositories;
using StockApplication.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StockApplication.Views;

public partial class StaffListPage : ContentPage
{
    private readonly Repository<Staff> _staffRepository;
    private readonly UserRepository _userRepository;
    private readonly AuthService _authService;
    private ObservableCollection<StaffViewModel> _staffList = new();

    public StaffListPage(Repository<Staff> staffRepository, UserRepository userRepository, AuthService authService)
    {
        InitializeComponent();
        
        _staffRepository = staffRepository;
        _userRepository = userRepository;
        _authService = authService;
        
        // Set up the collection view
        StaffCollectionView.ItemsSource = _staffList;
        
        // Set up refresh view
        StaffRefreshView.Command = new Command(async () => await LoadStaffAsync());
        
        // Set up event handlers
        AddStaffButton.Clicked += OnAddStaffClicked;
        SearchEntry.TextChanged += OnSearchTextChanged;
        StaffCollectionView.SelectionChanged += OnStaffSelectionChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Load staff members
        await LoadStaffAsync();
    }

    private async Task LoadStaffAsync()
    {
        try
        {
            StaffRefreshView.IsRefreshing = true;
            
            // Get all staff members
            var staffList = await _staffRepository.GetAllAsync();
            
            // Get all users
            var userList = await _userRepository.GetAllUsersAsync();
            
            // Create view models
            _staffList.Clear();
            foreach (var staff in staffList)
            {
                var associatedUser = userList.FirstOrDefault(u => u.StaffID == staff.StaffID);
                
                var staffVM = new StaffViewModel
                {
                    // Staff properties
                    StaffID = staff.StaffID,
                    Staff_Name = staff.Staff_Name,
                    Position = staff.Position,
                    Staff_Phone = staff.Staff_Phone,
                    Salary = staff.Salary,
                    HireDate = staff.HireDate,
                    
                    // User account association
                    HasUserAccount = associatedUser != null,
                    AssociatedUser = associatedUser
                };
                
                _staffList.Add(staffVM);
            }
            
            // Update total count
            TotalItemsLabel.Text = $"Total Staff Members: {_staffList.Count}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load staff: {ex.Message}", "OK");
        }
        finally
        {
            StaffRefreshView.IsRefreshing = false;
        }
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                await LoadStaffAsync();
                return;
            }
            
            var searchTerm = e.NewTextValue.ToLowerInvariant();
            
            // Get all staff and users
            var allStaff = await _staffRepository.GetAllAsync();
            var allUsers = await _userRepository.GetAllUsersAsync();
            
            // Filter staff by search criteria
            var filteredStaff = allStaff.Where(s => 
                s.Staff_Name.ToLowerInvariant().Contains(searchTerm) ||
                (s.Position != null && s.Position.ToLowerInvariant().Contains(searchTerm)) ||
                (s.Staff_Phone != null && s.Staff_Phone.ToLowerInvariant().Contains(searchTerm)));
            
            // Create view models for filtered staff
            _staffList.Clear();
            foreach (var staff in filteredStaff)
            {
                var associatedUser = allUsers.FirstOrDefault(u => u.StaffID == staff.StaffID);
                
                var staffVM = new StaffViewModel
                {
                    StaffID = staff.StaffID,
                    Staff_Name = staff.Staff_Name,
                    Position = staff.Position,
                    Staff_Phone = staff.Staff_Phone,
                    Salary = staff.Salary,
                    HireDate = staff.HireDate,
                    
                    HasUserAccount = associatedUser != null,
                    AssociatedUser = associatedUser
                };
                
                _staffList.Add(staffVM);
            }
            
            // Update search results count
            TotalItemsLabel.Text = $"Search Results: {_staffList.Count}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Search failed: {ex.Message}", "OK");
        }
    }

    private async void OnAddStaffClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("Staff/Add");
    }

    private async void OnStaffSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is StaffViewModel selectedStaff)
        {
            // Clear selection
            StaffCollectionView.SelectedItem = null;
            
            // Navigate to staff edit page
            await Shell.Current.GoToAsync($"Staff/Edit?id={selectedStaff.StaffID}");
        }
    }
    
    private async void OnEditStaffClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is StaffViewModel staff)
        {
            // Navigate to edit page with the selected staff ID
            await Shell.Current.GoToAsync($"Staff/Edit?id={staff.StaffID}");
        }
    }
    
    private async void OnManageUserClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is StaffViewModel staff)
        {
            if (staff.HasUserAccount)
            {
                // Edit existing user account
                await Shell.Current.GoToAsync($"Users/Edit?id={staff.AssociatedUser.UserID}");
            }
            else
            {
                // Create new user account for this staff
                await Shell.Current.GoToAsync($"Users/Edit?staffId={staff.StaffID}");
            }
        }
    }
    
    private async void OnDeleteStaffClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is StaffViewModel staff)
        {
            try
            {
                // Check if this staff has a user account that belongs to the current user
                if (staff.HasUserAccount && staff.AssociatedUser.UserID == _authService.CurrentUser?.UserID)
                {
                    await DisplayAlert("Error", "You cannot delete your own account.", "OK");
                    return;
                }
                
                string message = staff.HasUserAccount
                    ? $"This will delete {staff.Staff_Name} and remove the association with user account '{staff.AssociatedUser.Username}'."
                    : $"This will delete staff member {staff.Staff_Name}.";
                
                // Confirm deletion
                bool confirm = await DisplayAlert("Delete Staff Member", 
                    $"Are you sure you want to delete this staff member?\n\n{message}", 
                    "Yes", "No");
                    
                if (confirm)
                {
                    if (staff.HasUserAccount)
                    {
                        // Remove staff association from user
                        var user = staff.AssociatedUser;
                        user.StaffID = null;
                        await _userRepository.UpdateUserAsync(user);
                    }
                    
                    // Delete the staff member
                    var staffEntity = await _staffRepository.GetByIdAsync(staff.StaffID);
                    if (staffEntity != null)
                    {
                        await _staffRepository.DeleteAsync(staffEntity);
                    }
                    
                    // Refresh the list
                    await LoadStaffAsync();
                    
                    // Show success message
                    await DisplayAlert("Success", "Staff member deleted successfully.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to delete staff member: {ex.Message}", "OK");
            }
        }
    }
    
    private async void OnViewAllUsersClicked(object sender, EventArgs e)
    {
        // Navigate to the user management page
        await Shell.Current.GoToAsync("Users");
    }
}

// View model to combine Staff and User information for display
public class StaffViewModel : INotifyPropertyChanged
{
    // Staff properties
    public int StaffID { get; set; }
    public string Staff_Name { get; set; }
    public string Position { get; set; }
    public string Staff_Phone { get; set; }
    public double? Salary { get; set; }
    public DateTime? HireDate { get; set; }
    
    // User account association
    private bool _hasUserAccount;
    public bool HasUserAccount
    {
        get => _hasUserAccount;
        set
        {
            if (_hasUserAccount != value)
            {
                _hasUserAccount = value;
                OnPropertyChanged();
            }
        }
    }
    
    private User _associatedUser;
    public User AssociatedUser
    {
        get => _associatedUser;
        set
        {
            if (_associatedUser != value)
            {
                _associatedUser = value;
                HasUserAccount = _associatedUser != null;
                OnPropertyChanged();
            }
        }
    }
    
    // INotifyPropertyChanged implementation
    public event PropertyChangedEventHandler PropertyChanged;
    
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
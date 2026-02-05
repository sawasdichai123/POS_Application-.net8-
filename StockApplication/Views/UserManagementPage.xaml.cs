using StockApplication.Models;
using StockApplication.Repositories;
using StockApplication.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace StockApplication.Views
{
    public partial class UserManagementPage : ContentPage
    {
        private readonly UserRepository _userRepository;
        private readonly Repository<Staff> _staffRepository;
        private readonly AuthService _authService;
        private readonly UserManagementViewModel _viewModel;
        
        public UserManagementPage(UserRepository userRepository, AuthService authService, Repository<Staff> staffRepository)
        {
            InitializeComponent();
            
            _userRepository = userRepository;
            _authService = authService;
            _staffRepository = staffRepository;
            
            // Initialize ViewModel
            _viewModel = new UserManagementViewModel(userRepository, authService, staffRepository);
            BindingContext = _viewModel;
            
            // Load users when page appears
            this.Appearing += OnPageAppearing;
        }
        
        private async void OnPageAppearing(object sender, EventArgs e)
        {
            await _viewModel.LoadUsersAsync();
        }
        
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.FilterUsers(e.NewTextValue);
        }
        
        private async void OnUserSelected(object sender, SelectionChangedEventArgs e)
        {
            // Clear selection
            UsersCollectionView.SelectedItem = null;
        }
        
        private async void OnAddUserClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new UserEditPage(_userRepository, _staffRepository, _authService));
        }
        
        private async void OnEditUserClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var user = button?.CommandParameter as User;
            
            if (user != null)
            {
                try
                {
                    // Use QueryString parameters that match the UserEditPage's QueryProperty name (UserId, not UserID)
                    var navigationParameter = new Dictionary<string, object>
                    {
                        { "id", user.UserID.ToString() }
                    };
                    
                    // Make sure to use the correct route name registered in AppShell.xaml.cs
                    await Shell.Current.GoToAsync("useredit", navigationParameter);
                }
                catch (Exception ex)
                {
                    // Add error handling to help diagnose issues
                    await DisplayAlert("Navigation Error", $"Could not navigate to edit page: {ex.Message}", "OK");
                }
            }
        }
        
        private async void OnToggleUserStatusClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var user = button?.CommandParameter as User;
            
            if (user != null)
            {
                // Don't allow deactivating your own account
                if (user.UserID == _authService.CurrentUser.UserID)
                {
                    await DisplayAlert("Error", "You cannot deactivate your own account.", "OK");
                    return;
                }
                
                bool result;
                string message;
                
                if (user.IsActive)
                {
                    result = await DisplayAlert("Deactivate User", 
                        $"Are you sure you want to deactivate user '{user.Username}'?", 
                        "Yes", "No");
                    message = $"User '{user.Username}' has been deactivated.";
                }
                else
                {
                    result = await DisplayAlert("Activate User", 
                        $"Are you sure you want to activate user '{user.Username}'?", 
                        "Yes", "No");
                    message = $"User '{user.Username}' has been activated.";
                }
                
                if (result)
                {
                    user.IsActive = !user.IsActive;
                    await _userRepository.UpdateUserAsync(user);
                    await _viewModel.LoadUsersAsync();
                    _viewModel.StatusMessage = message;
                }
            }
        }
        
        private async void OnDeleteUserClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var user = button?.CommandParameter as User;
            
            if (user != null)
            {
                // Don't allow deleting your own account
                if (user.UserID == _authService.CurrentUser.UserID)
                {
                    await DisplayAlert("Error", "You cannot delete your own account.", "OK");
                    return;
                }
                
                // Confirm deletion
                bool confirmDelete = await DisplayAlert("Delete User", 
                    $"Are you sure you want to delete user '{user.Username}'? This action cannot be undone.", 
                    "Yes", "No");
                    
                if (confirmDelete)
                {
                    try
                    {
                        // Delete the user using the ID-based method
                        await _userRepository.DeleteUserAsync(user.UserID);
                        await _viewModel.LoadUsersAsync();
                        _viewModel.StatusMessage = $"User '{user.Username}' has been deleted.";
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to delete user: {ex.Message}", "OK");
                    }
                }
            }
        }
        
        private async void OnChangePasswordClicked(object sender, EventArgs e)
        {
            // Navigate to user edit page for the current user
            var currentUser = _authService.CurrentUser;
            if (currentUser != null)
            {
                try
                {
                    // Use PushAsync for navigation - this matches your existing UI flow better
                    await Navigation.PushAsync(new UserEditPage(_userRepository, _staffRepository, _authService) { 
                        UserId = currentUser.UserID.ToString() 
                    });
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Navigation Error", $"Could not navigate to edit page: {ex.Message}", "OK");
                }
            }
        }
    }
    
    // ViewModel for User Management
    public class UserManagementViewModel : INotifyPropertyChanged
    {
        private readonly UserRepository _userRepository;
        private readonly AuthService _authService;
        private readonly Repository<Staff> _staffRepository;
        
        private ObservableCollection<User> _users = new();
        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }
        
        private ObservableCollection<User> _allUsers = new();
        
        private User _currentUser;
        public User CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }
        
        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }
        
        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }
        
        // Commands
        public ICommand RefreshCommand { get; }
        
        public UserManagementViewModel(UserRepository userRepository, AuthService authService, Repository<Staff> staffRepository)
        {
            _userRepository = userRepository;
            _authService = authService;
            _staffRepository = staffRepository;
            
            // Set current user
            CurrentUser = _authService.CurrentUser;
            
            // Initialize commands
            RefreshCommand = new Command(async () => await LoadUsersAsync());
        }
        
        public async Task LoadUsersAsync()
        {
            try
            {
                IsRefreshing = true;
                
                // Load all users
                var userList = await _userRepository.GetAllUsersAsync();
                
                // Set permissions based on current user and roles
                foreach (var user in userList)
                {
                    // DEFAULT: Allow deletion of all users
                    user.CanBeDeleted = true;
                    
                    // EXCEPTION 1: Cannot delete your own account
                    if (user.UserID == CurrentUser.UserID)
                    {
                        user.CanBeDeleted = false;
                    }
                    
                    // EXCEPTION 2: Only Admins can delete Admin accounts
                    if (user.Role == "Admin" && CurrentUser.Role != "Admin")
                    {
                        user.CanBeDeleted = false;
                    }
                    
                    // NO RESTRICTION for managers deleting other managers
                    // Removed the code that prevented managers from deleting other managers
                }
                
                // Update collections
                _allUsers.Clear();
                foreach (var user in userList)
                {
                    _allUsers.Add(user);
                }
                
                Users.Clear();
                foreach (var user in _allUsers)
                {
                    Users.Add(user);
                }
            }
            catch (Exception ex)
            {
                // Improved exception handling
                System.Diagnostics.Debug.WriteLine($"Error loading users: {ex.Message}");
                StatusMessage = "Error loading users. Please try again.";
            }
            finally
            {
                IsRefreshing = false;
            }
        }
        
        public void FilterUsers(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Reset to show all users
                Users.Clear();
                foreach (var user in _allUsers)
                {
                    Users.Add(user);
                }
                return;
            }
            
            // Filter users based on search text
            var filteredUsers = _allUsers.Where(u => 
                u.Username.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                u.Role.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(u.StaffName) && u.StaffName.Contains(searchText, StringComparison.OrdinalIgnoreCase)));
            
            Users.Clear();
            foreach (var user in filteredUsers)
            {
                Users.Add(user);
            }
        }
        
        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;
                
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
using StockApplication.Models;
using StockApplication.Repositories;
using StockApplication.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace StockApplication.Views
{
    [QueryProperty(nameof(UserId), "id")]
    [QueryProperty(nameof(StaffId), "staffId")]
    public partial class UserEditPage : ContentPage
    {
        private readonly UserRepository _userRepository;
        private readonly Repository<Staff> _staffRepository;
        private readonly AuthService _authService;
        private readonly UserEditViewModel _viewModel;
        
        private int _userId;
        private int _staffId;
        
        public string UserId
        {
            set
            {
                _userId = int.TryParse(value, out var result) ? result : 0;
                LoadUser();
            }
        }
        
        public string StaffId
        {
            set
            {
                _staffId = int.TryParse(value, out var result) ? result : 0;
                if (_staffId > 0)
                {
                    LoadStaffForNewUser();
                }
            }
        }
        
        public UserEditPage(UserRepository userRepository, Repository<Staff> staffRepository, AuthService authService)
        {
            InitializeComponent();
            
            _userRepository = userRepository;
            _staffRepository = staffRepository;
            _authService = authService;
            
            // Initialize ViewModel
            _viewModel = new UserEditViewModel(userRepository, staffRepository);
            BindingContext = _viewModel;
        }
        
        private async void LoadUser()
        {
            if (_userId <= 0)
            {
                // New user
                _viewModel.IsNewUser = true;
                _viewModel.IsEditMode = false;
                _viewModel.Title = "Add User";
                _viewModel.PasswordSectionTitle = "Password";
                _viewModel.PasswordEntryLabel = "Password *";
                _viewModel.CanChangeStaffLink = true;
                
                // Load staff list
                await _viewModel.LoadStaffListAsync();
                return;
            }
            
            try
            {
                // Existing user
                _viewModel.IsNewUser = false;
                _viewModel.IsEditMode = true;
                _viewModel.Title = "Edit User";
                _viewModel.PasswordSectionTitle = "Change Password (Optional)";
                _viewModel.PasswordEntryLabel = "New Password";
                
                // Load the user
                User user = await _userRepository.GetUserByIdAsync(_userId);
                if (user != null)
                {
                    _viewModel.UserID = user.UserID;
                    _viewModel.Username = user.Username;
                    _viewModel.Role = user.Role;
                    _viewModel.IsActive = user.IsActive;
                    _viewModel.StaffID = user.StaffID;
                    
                    // If user came from a Staff edit page, don't allow changing the staff link
                    _viewModel.CanChangeStaffLink = _staffId <= 0;
                    
                    // Load staff list and select the associated staff if any
                    await _viewModel.LoadStaffListAsync();
                    if (user.StaffID.HasValue)
                    {
                        _viewModel.SelectedStaff = _viewModel.StaffList.FirstOrDefault(s => s.StaffID == user.StaffID.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load user: {ex.Message}", "OK");
            }
        }
        
        private async void LoadStaffForNewUser()
        {
            try
            {
                _viewModel.IsNewUser = true;
                _viewModel.IsEditMode = false;
                _viewModel.Title = "Add User";
                _viewModel.PasswordSectionTitle = "Password";
                _viewModel.PasswordEntryLabel = "Password *";
                
                // We're creating a user for a specific staff, don't allow changing
                _viewModel.CanChangeStaffLink = false;
                
                // Load staff list
                await _viewModel.LoadStaffListAsync();
                
                // Select the staff member
                var staff = await _staffRepository.GetByIdAsync(_staffId);
                if (staff != null)
                {
                    _viewModel.SelectedStaff = staff;
                    _viewModel.StaffID = staff.StaffID;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load staff: {ex.Message}", "OK");
            }
        }
        
        private void OnStatusLabelTapped(object sender, EventArgs e)
        {
            IsActiveCheckBox.IsChecked = !IsActiveCheckBox.IsChecked;
        }
        
        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(_viewModel.Username))
                {
                    _viewModel.ErrorMessage = "Username is required.";
                    return;
                }
                
                if (string.IsNullOrEmpty(_viewModel.Role))
                {
                    _viewModel.ErrorMessage = "Role is required.";
                    return;
                }
                
                if (_viewModel.IsNewUser && string.IsNullOrEmpty(_viewModel.Password))
                {
                    _viewModel.ErrorMessage = "Password is required for new users.";
                    return;
                }
                
                if (!string.IsNullOrEmpty(_viewModel.Password))
                {
                    if (_viewModel.Password != _viewModel.ConfirmPassword)
                    {
                        _viewModel.ErrorMessage = "Passwords do not match.";
                        return;
                    }
                    
                    if (_viewModel.Password.Length < 8)
                    {
                        _viewModel.ErrorMessage = "Password must be at least 8 characters long.";
                        return;
                    }
                }
                
                if (_viewModel.IsEditMode && 
                    !string.IsNullOrEmpty(_viewModel.Password) && 
                    string.IsNullOrEmpty(_viewModel.CurrentPassword))
                {
                    _viewModel.ErrorMessage = "Current password is required to change password.";
                    return;
                }
                
                // Create or update user
                User user;
                
                if (_viewModel.IsNewUser)
                {
                    // Create new user
                    user = new User
                    {
                        Username = _viewModel.Username,
                        Password = _userRepository.HashPassword(_viewModel.Password),
                        Role = _viewModel.Role,
                        IsActive = _viewModel.IsActive,
                        StaffID = _viewModel.SelectedStaff?.StaffID
                    };
                    
                    await _userRepository.InsertUserAsync(user);
                    await DisplayAlert("Success", "User created successfully.", "OK");
                }
                else
                {
                    // Update existing user
                    user = await _userRepository.GetUserByIdAsync(_viewModel.UserID);
                    
                    if (user != null)
                    {
                        // Only check current password if changing password
                        if (!string.IsNullOrEmpty(_viewModel.Password))
                        {
                            // Verify current password
                            if (!_userRepository.VerifyPassword(_viewModel.CurrentPassword, user.Password))
                            {
                                _viewModel.ErrorMessage = "Current password is incorrect.";
                                return;
                            }
                            
                            user.Password = _userRepository.HashPassword(_viewModel.Password);
                        }
                        
                        user.Role = _viewModel.Role;
                        user.IsActive = _viewModel.IsActive;
                        user.StaffID = _viewModel.SelectedStaff?.StaffID;
                        
                        await _userRepository.UpdateUserAsync(user);
                        await DisplayAlert("Success", "User updated successfully.", "OK");
                    }
                }
                
                // Navigate back
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                _viewModel.ErrorMessage = $"Error: {ex.Message}";
            }
        }
        
        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
    
    // Enhanced ViewModel for UserEditPage
    public class UserEditViewModel : INotifyPropertyChanged
    {
        private readonly UserRepository _userRepository;
        private readonly Repository<Staff> _staffRepository;
        
        // User properties
        private int _userID;
        public int UserID
        {
            get => _userID;
            set => SetProperty(ref _userID, value);
        }
        
        private string _username;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }
        
        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }
        
        private string _confirmPassword;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }
        
        private string _currentPassword;
        public string CurrentPassword
        {
            get => _currentPassword;
            set => SetProperty(ref _currentPassword, value);
        }
        
        private string _role;
        public string Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
        }
        
        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }
        
        private int? _staffID;
        public int? StaffID
        {
            get => _staffID;
            set => SetProperty(ref _staffID, value);
        }
        
        // Staff association
        private ObservableCollection<Staff> _staffList = new();
        public ObservableCollection<Staff> StaffList
        {
            get => _staffList;
            set => SetProperty(ref _staffList, value);
        }
        
        private Staff _selectedStaff;
        public Staff SelectedStaff
        {
            get => _selectedStaff;
            set
            {
                if (SetProperty(ref _selectedStaff, value))
                {
                    StaffID = _selectedStaff?.StaffID;
                }
            }
        }
        
        // UI properties
        private string _title = "User";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        
        private bool _isNewUser = true;
        public bool IsNewUser
        {
            get => _isNewUser;
            set => SetProperty(ref _isNewUser, value);
        }
        
        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }
        
        private bool _canChangeStaffLink = true;
        public bool CanChangeStaffLink
        {
            get => _canChangeStaffLink;
            set => SetProperty(ref _canChangeStaffLink, value);
        }
        
        private string _passwordSectionTitle = "Password";
        public string PasswordSectionTitle
        {
            get => _passwordSectionTitle;
            set => SetProperty(ref _passwordSectionTitle, value);
        }
        
        private string _passwordEntryLabel = "Password *";
        public string PasswordEntryLabel
        {
            get => _passwordEntryLabel;
            set => SetProperty(ref _passwordEntryLabel, value);
        }
        
        private bool _hasError;
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }
        
        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                HasError = !string.IsNullOrEmpty(value);
            }
        }
        
        // Constructor
        public UserEditViewModel(UserRepository userRepository, Repository<Staff> staffRepository)
        {
            _userRepository = userRepository;
            _staffRepository = staffRepository;
            
            // Set default values
            Role = "Staff"; // Default role
            IsActive = true;
        }
        
        // Load staff list for dropdown
        public async Task LoadStaffListAsync()
        {
            try
            {
                var staffMembers = await _staffRepository.GetAllAsync();
                
                StaffList.Clear();
                foreach (var staff in staffMembers)
                {
                    StaffList.Add(staff);
                }
                
                // If StaffID is set but SelectedStaff is not, try to find and set it
                if (StaffID.HasValue && SelectedStaff == null)
                {
                    SelectedStaff = StaffList.FirstOrDefault(s => s.StaffID == StaffID.Value);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load staff list: {ex.Message}";
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
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;
                
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
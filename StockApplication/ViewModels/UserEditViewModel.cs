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

namespace StockApplication.ViewModels
{
    public class UserEditViewModel : INotifyPropertyChanged
    {
        private readonly UserRepository _userRepository;
        private readonly Repository<Staff> _staffRepository;
        private readonly User _user;
        
        private string _username;
        private string _role;
        private Staff _selectedStaff;
        private string _currentPassword;
        private string _password;
        private string _confirmPassword;
        private bool _isActive = true;
        private string _errorMessage;
        private ObservableCollection<Staff> _staffList;
        
        public string Title => _user?.UserID > 0 ? "Edit User" : "Add User";
        public string PasswordSectionTitle => _user?.UserID > 0 ? "Change Password" : "Set Password";
        public string PasswordEntryLabel => _user?.UserID > 0 ? "New Password" : "Password *";
        
        public bool IsNewUser => _user?.UserID == 0;
        public bool IsEditMode => !IsNewUser;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        
        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }
        
        public string Role
        {
            get => _role;
            set
            {
                _role = value;
                OnPropertyChanged();
            }
        }
        
        public ObservableCollection<Staff> StaffList
        {
            get => _staffList;
            set
            {
                _staffList = value;
                OnPropertyChanged();
            }
        }
        
        public Staff SelectedStaff
        {
            get => _selectedStaff;
            set
            {
                _selectedStaff = value;
                OnPropertyChanged();
            }
        }
        
        public string CurrentPassword
        {
            get => _currentPassword;
            set
            {
                _currentPassword = value;
                OnPropertyChanged();
            }
        }
        
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }
        
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                _confirmPassword = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }
        
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }
        
        public UserEditViewModel(UserRepository userRepository, Repository<Staff> staffRepository, User user = null)
        {
            _userRepository = userRepository;
            _staffRepository = staffRepository;
            _user = user ?? new User();
            
            // Initialize properties from user
            Username = _user.Username;
            Role = _user.Role ?? "Staff"; // Default to Staff
            IsActive = user?.IsActive ?? true; // Default to active
            
            // Load staff list
            LoadStaffListAsync();
        }
        
        private async void LoadStaffListAsync()
        {
            try
            {
                var staffMembers = await _staffRepository.GetAllAsync();
                StaffList = new ObservableCollection<Staff>(staffMembers);
                
                // Set selected staff if user has StaffID
                if (_user.StaffID.HasValue)
                {
                    SelectedStaff = StaffList.FirstOrDefault(s => s.StaffID == _user.StaffID.Value);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading staff list: {ex.Message}";
            }
        }
        
        public async Task<bool> SaveAsync()
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(Username))
                {
                    ErrorMessage = "Username is required.";
                    return false;
                }
                
                if (string.IsNullOrWhiteSpace(Role))
                {
                    ErrorMessage = "Role is required.";
                    return false;
                }
                
                // Check if username is taken (for new users)
                if (IsNewUser)
                {
                    bool isUsernameTaken = await _userRepository.IsUsernameTakenAsync(Username);
                    if (isUsernameTaken)
                    {
                        ErrorMessage = "Username is already taken.";
                        return false;
                    }
                    
                    // Password is required for new users
                    if (string.IsNullOrWhiteSpace(Password))
                    {
                        ErrorMessage = "Password is required.";
                        return false;
                    }
                }
                
                // Check password strength
                if (!string.IsNullOrEmpty(Password) && Password.Length < 8)
                {
                    ErrorMessage = "Password must be at least 8 characters long.";
                    return false;
                }
                
                // Check password confirmation
                if (!string.IsNullOrEmpty(Password) && Password != ConfirmPassword)
                {
                    ErrorMessage = "Passwords do not match.";
                    return false;
                }
                
                // For existing users, validate current password if changing password
                if (!IsNewUser && !string.IsNullOrEmpty(Password))
                {
                    if (string.IsNullOrEmpty(CurrentPassword))
                    {
                        ErrorMessage = "Current password is required.";
                        return false;
                    }
                    
                    // Verify current password
                    bool isCorrectPassword = await _userRepository.ChangePasswordAsync(
                        _user.UserID, CurrentPassword, Password);
                    
                    if (!isCorrectPassword)
                    {
                        ErrorMessage = "Current password is incorrect.";
                        return false;
                    }
                }
                
                // Set user properties
                _user.Username = Username;
                _user.Role = Role;
                _user.StaffID = SelectedStaff?.StaffID;
                _user.IsActive = IsActive;
                
                if (IsNewUser)
                {
                    // Create new user
                    await _userRepository.CreateUserAsync(_user, Password);
                }
                else
                {
                    // Update existing user without changing password (already done above if needed)
                    await _userRepository.UpdateUserAsync(_user);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error saving user: {ex.Message}";
                return false;
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
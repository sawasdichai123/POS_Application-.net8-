using StockApplication.Models;
using StockApplication.Repositories;
using StockApplication.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace StockApplication.ViewModels
{
    public class ChangePasswordViewModel : INotifyPropertyChanged
    {
        private readonly UserRepository _userRepository;
        private readonly AuthService _authService;
        
        private string _currentPassword;
        private string _newPassword;
        private string _confirmPassword;
        private string _errorMessage;
        private bool _isBusy;
        
        public string CurrentPassword
        {
            get => _currentPassword;
            set
            {
                _currentPassword = value;
                OnPropertyChanged();
            }
        }
        
        public string NewPassword
        {
            get => _newPassword;
            set
            {
                _newPassword = value;
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
        
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }
        
        public ICommand ChangePasswordCommand { get; }
        
        public event EventHandler PasswordChanged;
        
        public ChangePasswordViewModel(UserRepository userRepository, AuthService authService)
        {
            _userRepository = userRepository;
            _authService = authService;
            
            ChangePasswordCommand = new Command(async () => await ChangePasswordAsync());
        }
        
        private async Task ChangePasswordAsync()
        {
            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;
                
                // Validate input
                if (string.IsNullOrWhiteSpace(CurrentPassword))
                {
                    ErrorMessage = "Current password is required.";
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(NewPassword))
                {
                    ErrorMessage = "New password is required.";
                    return;
                }
                
                if (NewPassword.Length < 8)
                {
                    ErrorMessage = "New password must be at least 8 characters long.";
                    return;
                }
                
                if (NewPassword != ConfirmPassword)
                {
                    ErrorMessage = "New passwords do not match.";
                    return;
                }
                
                if (CurrentPassword == NewPassword)
                {
                    ErrorMessage = "New password must be different from current password.";
                    return;
                }
                
                // Get current user
                User currentUser = _authService.CurrentUser;
                if (currentUser == null)
                {
                    ErrorMessage = "You must be logged in to change your password.";
                    return;
                }
                
                // Change password
                bool success = await _userRepository.ChangePasswordAsync(
                    currentUser.UserID, CurrentPassword, NewPassword);
                
                if (success)
                {
                    // Notify subscribers that password was changed
                    PasswordChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    ErrorMessage = "Current password is incorrect.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error changing password: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
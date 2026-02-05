using StockApplication.Services;
using System;
using System.Threading.Tasks;

namespace StockApplication.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly AuthService _authService;
        
        public LoginPage(AuthService authService)
        {
            InitializeComponent();
            
            _authService = authService;
            
            // Set up event handlers
            LoginButton.Clicked += OnLoginButtonClicked;
        }
        
        private async void OnLoginButtonClicked(object sender, EventArgs e)
        {
            // Show loading state
            LoginButton.IsEnabled = false;
            LoginButton.Text = "Logging in...";
            ErrorLabel.IsVisible = false;
            
            try
            {
                string username = UsernameEntry.Text?.Trim();
                string password = PasswordEntry.Text;
                
                
                // Validate input
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ShowError("Please enter both username and password.");
                    return;
                }
                
                // Attempt login
                bool success = await _authService.LoginAsync(username, password);
                
                if (success)
                {
                    // Navigate to main app
                    Application.Current.MainPage = new AppShell(_authService);
                }
                else
                {
                    ShowError("Invalid username or password.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Login error: {ex.Message}");
            }
            finally
            {
                // Reset button state
                LoginButton.IsEnabled = true;
                LoginButton.Text = "Login";
            }
            
        }
        
        private void ShowError(string message)
        {
            ErrorLabel.Text = message;
            ErrorLabel.IsVisible = true;
        }

        
    }
}
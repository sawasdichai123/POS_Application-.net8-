using StockApplication.Repositories;
using StockApplication.Services;
using StockApplication.ViewModels;
using System;

namespace StockApplication.Views
{
    public partial class ChangePasswordPage : ContentPage
    {
        private readonly ChangePasswordViewModel _viewModel;
        
        public ChangePasswordPage(UserRepository userRepository, AuthService authService)
        {
            InitializeComponent();
            
            _viewModel = new ChangePasswordViewModel(userRepository, authService);
            BindingContext = _viewModel;
            
            // Subscribe to password change success event
            _viewModel.PasswordChanged += OnPasswordChanged;
        }
        
        private async void OnPasswordChanged(object sender, EventArgs e)
        {
            await DisplayAlert("Success", "Your password has been updated successfully.", "OK");
            await Navigation.PopAsync();
        }
        
        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
        
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Unsubscribe from event to prevent memory leaks
            _viewModel.PasswordChanged -= OnPasswordChanged;
        }
    }
}
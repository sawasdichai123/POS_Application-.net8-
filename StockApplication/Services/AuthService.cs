using StockApplication.Models;
using StockApplication.Repositories;
using System;
using System.Threading.Tasks;

namespace StockApplication.Services
{
    public class AuthService
    {
        private readonly UserRepository _userRepository;
        private User _currentUser;
        
        public User CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null;
        public bool IsManager => _currentUser?.Role == "Manager";
        public bool IsStaff => _currentUser?.Role == "Staff";
        
        public event EventHandler<User> UserLoggedIn;
        public event EventHandler UserLoggedOut;
        
        public AuthService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        
        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                var user = await _userRepository.AuthenticateAsync(username, password);
                
                if (user != null)
                {
                    _currentUser = user;
                    UserLoggedIn?.Invoke(this, user);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                return false;
            }
        }
        
        public void Logout()
        {
            _currentUser = null;
            UserLoggedOut?.Invoke(this, EventArgs.Empty);
        }
        
        // Method to check if user has permission for specific action
        public bool HasPermission(string permission)
        {
            // For now, we have a simple role-based check
            switch (permission)
            {
                case "AccessFlyoutMenu":
                    return IsManager;
                case "CreateOrder":
                    return IsAuthenticated; // Both roles can create orders
                case "ManageStaff":
                    return IsManager;
                case "ManageSuppliers":
                    return IsManager;
                case "AccessReports":
                    return IsManager;
                default:
                    return false;
            }
        }
    }
}
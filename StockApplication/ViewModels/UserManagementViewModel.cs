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

namespace StockApplication.ViewModels
{
    public class UserManagementViewModel : INotifyPropertyChanged
    {
        private readonly UserRepository _userRepository;
        private readonly AuthService _authService;
        private readonly Repository<Staff> _staffRepository;
        
        private ObservableCollection<User> _users;
        private ObservableCollection<User> _allUsers;
        private bool _isRefreshing;
        private string _statusMessage;
        
        public ObservableCollection<User> Users
        {
            get => _users;
            set
            {
                _users = value;
                OnPropertyChanged();
            }
        }
        
        public User CurrentUser => _authService.CurrentUser;
        
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                _isRefreshing = value;
                OnPropertyChanged();
            }
        }
        
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
        
        public ICommand RefreshCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        
        public UserManagementViewModel(UserRepository userRepository, AuthService authService, Repository<Staff> staffRepository)
        {
            _userRepository = userRepository;
            _authService = authService;
            _staffRepository = staffRepository;
            
            Users = new ObservableCollection<User>();
            _allUsers = new ObservableCollection<User>();
            
            RefreshCommand = new Command(async () => await LoadUsersAsync());
            ChangePasswordCommand = new Command(async () => await GoToChangePasswordAsync());
        }
        
        public async Task LoadUsersAsync()
        {
            try
            {
                IsRefreshing = true;
                StatusMessage = "Loading users...";
                
                var users = await _userRepository.GetAllAsync();
                var staffMembers = await _staffRepository.GetAllAsync();
                
                // Combine user and staff information
                foreach (var user in users)
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
                    // We explicitly want to allow this
                    
                    // Add staff information
                    if (user.StaffID.HasValue)
                    {
                        var staffMember = staffMembers.FirstOrDefault(s => s.StaffID == user.StaffID.Value);
                        if (staffMember != null)
                        {
                            user.StaffName = staffMember.Staff_Name;
                        }
                    }
                }
                
                _allUsers.Clear();
                foreach (var user in users)
                {
                    _allUsers.Add(user);
                }
                
                Users = new ObservableCollection<User>(_allUsers);
                StatusMessage = $"Loaded {users.Count} users";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading users: {ex.Message}";
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
                Users = new ObservableCollection<User>(_allUsers);
            }
            else
            {
                searchText = searchText.ToLower();
                Users = new ObservableCollection<User>(
                    _allUsers.Where(u => 
                        u.Username.ToLower().Contains(searchText) || 
                        u.Role.ToLower().Contains(searchText) ||
                        (u.StaffName != null && u.StaffName.ToLower().Contains(searchText)))
                );
            }
            
            StatusMessage = $"Found {Users.Count} users";
        }
        
        private async Task GoToChangePasswordAsync()
        {
            await Shell.Current.GoToAsync("Account/ChangePassword");
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
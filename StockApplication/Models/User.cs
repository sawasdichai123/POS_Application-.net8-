using SQLite;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StockApplication.Models
{
    public class User : INotifyPropertyChanged
    {
        private int _userID;
        private string _username;
        private string _password;
        private string _role;
        private int? _staffID;
        private DateTime? _lastLogin;
        private bool _isActive = true;
        private string _staffName;
        private bool _canBeDeleted;

        [PrimaryKey, AutoIncrement]
        public int UserID
        {
            get => _userID;
            set => SetProperty(ref _userID, value);
        }
        
        [NotNull, Unique]
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }
        
        [NotNull]
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }
        
        [NotNull]
        public string Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
        }
        
        [Indexed]
        public int? StaffID
        {
            get => _staffID;
            set => SetProperty(ref _staffID, value);
        }
        
        public DateTime? LastLogin
        {
            get => _lastLogin;
            set => SetProperty(ref _lastLogin, value);
        }
        
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }
        
        [Ignore]
        public string StaffName
        {
            get => _staffName;
            set => SetProperty(ref _staffName, value);
        }
        
        [Ignore]
        public bool CanBeDeleted
        {
            get => _canBeDeleted;
            set => SetProperty(ref _canBeDeleted, value);
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
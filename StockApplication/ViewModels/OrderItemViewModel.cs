using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StockApplication.ViewModels
{
    public class OrderItemViewModel : INotifyPropertyChanged
    {
        private int _menuID;
        private string _menu_name;
        private double _price;
        private int _quantity;
        private double _subtotal;
        private string _notes;
        private bool _isModified;
        private double _originalPrice;

        public int MenuID 
        { 
            get => _menuID; 
            set
            {
                if (_menuID != value)
                {
                    _menuID = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string Menu_name
        { 
            get => _menu_name; 
            set
            {
                if (_menu_name != value)
                {
                    _menu_name = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public double Price
        { 
            get => _price; 
            set
            {
                if (_price != value)
                {
                    _price = value;
                    IsModified = Math.Abs(_price - _originalPrice) >= 0.001;
                    OnPropertyChanged();
                    UpdateSubtotal();  // Update subtotal when price changes
                }
            }
        }
        
        public double OriginalPrice
        {
            get => _originalPrice; 
            set
            {
                if (_originalPrice != value)
                {
                    _originalPrice = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value && value > 0) // Ensure quantity is always positive
                {
                    _quantity = value;
                    OnPropertyChanged();
                    UpdateSubtotal();  // Update subtotal when quantity changes
                }
            }
        }
        
        public double Subtotal
        {
            get => _subtotal;
            set
            {
                if (_subtotal != value)
                {
                    _subtotal = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string Notes
        {
            get => _notes;
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public bool IsModified
        {
            get => _isModified;
            set
            {
                if (_isModified != value)
                {
                    _isModified = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string FormattedPrice => $"${Price:F2}";
        
        public string FormattedSubtotal => $"${Subtotal:F2}";
        
        public void UpdateSubtotal()
        {
            Subtotal = Math.Round(Quantity * Price, 2);
        }
        
        // Calculate potential discount percentage if modified price is lower than original
        public double DiscountPercentage => 
            (OriginalPrice > 0 && Price < OriginalPrice) 
                ? Math.Round(100 - (Price / OriginalPrice * 100), 1) 
                : 0;
        
        public string DiscountDisplay => 
            DiscountPercentage > 0 
                ? $"-{DiscountPercentage}%" 
                : string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            // When certain properties change, we need to update dependent properties
            if (propertyName == nameof(Price) || propertyName == nameof(OriginalPrice))
            {
                OnPropertyChanged(nameof(FormattedPrice));
                OnPropertyChanged(nameof(DiscountPercentage));
                OnPropertyChanged(nameof(DiscountDisplay));
            }
            else if (propertyName == nameof(Subtotal))
            {
                OnPropertyChanged(nameof(FormattedSubtotal));
            }
        }
        
        // Constructor to ensure subtotal is calculated
        public OrderItemViewModel()
        {
            _quantity = 1;
            _notes = string.Empty;
            _isModified = false;
            // Subtotal will be calculated when Price is set
        }
        
        // Constructor with parameters for easier initialization
        public OrderItemViewModel(int menuId, string menuName, double price, int quantity = 1)
        {
            _menuID = menuId;
            _menu_name = menuName;
            _originalPrice = price;
            _price = price;
            _quantity = quantity;
            _notes = string.Empty;
            _isModified = false;
            UpdateSubtotal();
        }
        
        // Creates a clone of this view model
        public OrderItemViewModel Clone()
        {
            return new OrderItemViewModel
            {
                MenuID = this.MenuID,
                Menu_name = this.Menu_name,
                OriginalPrice = this.OriginalPrice,
                Price = this.Price,
                Quantity = this.Quantity,
                Subtotal = this.Subtotal,
                Notes = this.Notes,
                IsModified = this.IsModified
            };
        }
    }
}
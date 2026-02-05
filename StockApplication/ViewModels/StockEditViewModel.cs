using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockApplication.Models;
using StockApplication.Repositories;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace StockApplication.ViewModels
{
    // ViewModel for Stock Edit page
    public class StockEditViewModel : BaseViewModel
    {
        private readonly StockRepository _stockRepository;
        private readonly Repository<Supplier> _supplierRepository;

        private int _stockId;
        private string _stockName;
        private string _type;
        private double _quantity;
        private string _unit;
        private DateTime? _expiryDate;
        private int? _supplierId;
        private ObservableCollection<Supplier> _suppliers = new();

        public int StockId
        {
            get => _stockId;
            set => SetProperty(ref _stockId, value);
        }
        
        public string StockName
        {
            get => _stockName;
            set => SetProperty(ref _stockName, value);
        }
        
        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }
        
        public double Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }
        
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }
        
        public DateTime? ExpiryDate
        {
            get => _expiryDate;
            set => SetProperty(ref _expiryDate, value);
        }
        
        public int? SupplierId
        {
            get => _supplierId;
            set => SetProperty(ref _supplierId, value);
        }
        
        public ObservableCollection<Supplier> Suppliers
        {
            get => _suppliers;
            set => SetProperty(ref _suppliers, value);
        }

        public IRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IRelayCommand LoadSuppliersCommand { get; }

        public StockEditViewModel(StockRepository stockRepository, Repository<Supplier> supplierRepository)
        {
            Title = "Edit Stock";
            _stockRepository = stockRepository;
            _supplierRepository = supplierRepository;

            SaveCommand = new AsyncRelayCommand(SaveAsync);
            CancelCommand = new RelayCommand(Cancel);
            LoadSuppliersCommand = new AsyncRelayCommand(LoadSuppliersAsync);
        }

        public async Task InitializeAsync(int stockId)
        {
            if (stockId > 0)
            {
                // Load existing stock
                var stock = await _stockRepository.GetByIdAsync(stockId);
                if (stock != null)
                {
                    StockId = stock.StockID;
                    StockName = stock.Stock_Name;
                    Type = stock.Type;
                    Quantity = stock.Quantity;
                    Unit = stock.Unit;
                    ExpiryDate = stock.ExpiryDate;
                    SupplierId = stock.SupplierID;
                    Title = "Edit Stock";
                }
            }
            else
            {
                // Initialize for a new stock item
                StockId = 0;
                StockName = string.Empty;
                Type = string.Empty;
                Quantity = 0;
                Unit = string.Empty;
                ExpiryDate = DateTime.Now.AddMonths(6);
                SupplierId = null;
                Title = "Add Stock";
            }

            await LoadSuppliersAsync();
        }

        private async Task LoadSuppliersAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                Suppliers.Clear();
                var suppliers = await _supplierRepository.GetAllAsync();
                foreach (var supplier in suppliers)
                {
                    Suppliers.Add(supplier);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveAsync()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrWhiteSpace(StockName))
            {
                // Show validation error
                return;
            }

            IsBusy = true;
            try
            {
                var stock = new Stock
                {
                    StockID = StockId,
                    Stock_Name = StockName,
                    Type = Type,
                    Quantity = Quantity,
                    Unit = Unit,
                    ExpiryDate = ExpiryDate,
                    SupplierID = SupplierId,
                    UpdatedAt = DateTime.Now
                };

                if (StockId > 0)
                {
                    await _stockRepository.UpdateAsync(stock);
                }
                else
                {
                    await _stockRepository.InsertAsync(stock);
                }

                // Navigate back after save
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Cancel()
        {
            // Navigate back without saving
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockApplication.Models;
using StockApplication.Repositories;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace StockApplication.ViewModels
{
    // ViewModel for Stock List page
    public class StockListViewModel : BaseViewModel
    {
        private readonly StockRepository _stockRepository;
        private readonly Repository<Supplier> _supplierRepository;
        
        public ObservableCollection<Stock> Stocks { get; } = new();
        public ObservableCollection<Stock> LowStockItems { get; } = new();
        
        private Stock _selectedStock;
        public Stock SelectedStock
        {
            get => _selectedStock;
            set => SetProperty(ref _selectedStock, value);
        }

        public IRelayCommand LoadStocksCommand { get; }
        public IRelayCommand AddStockCommand { get; }
        public IRelayCommand<Stock> EditStockCommand { get; }
        public IRelayCommand<Stock> DeleteStockCommand { get; }
        public IRelayCommand LoadLowStockCommand { get; }

        public StockListViewModel(StockRepository stockRepository, Repository<Supplier> supplierRepository)
        {
            Title = "Stock Inventory";
            _stockRepository = stockRepository;
            _supplierRepository = supplierRepository;

            LoadStocksCommand = new AsyncRelayCommand(LoadStocksAsync);
            AddStockCommand = new AsyncRelayCommand(AddStockAsync);
            EditStockCommand = new AsyncRelayCommand<Stock>(EditStockAsync);
            DeleteStockCommand = new AsyncRelayCommand<Stock>(DeleteStockAsync);
            LoadLowStockCommand = new AsyncRelayCommand(LoadLowStockAsync);
        }

        private async Task LoadStocksAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                Stocks.Clear();
                // Use the new conversion method instead of directly using GetStockWithSupplierAsync
                var stocks = await _stockRepository.GetStockWithSupplierAsStockAsync();
                foreach (var stock in stocks)
                {
                    Stocks.Add(stock);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadLowStockAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                LowStockItems.Clear();
                var stocks = await _stockRepository.GetLowStockAsync(10); // Items with quantity < 10
                foreach (var stock in stocks)
                {
                    LowStockItems.Add(stock);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Task AddStockAsync()
        {
            // Navigation to Add Stock page would happen here
            return Task.CompletedTask;
        }

        private Task EditStockAsync(Stock stock)
        {
            // Navigation to Edit Stock page with selected stock would happen here
            return Task.CompletedTask;
        }

        private async Task DeleteStockAsync(Stock stock)
        {
            if (stock == null)
                return;

            await _stockRepository.DeleteAsync(stock);
            await LoadStocksAsync();
        }
    }
}
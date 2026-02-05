using SQLite;
using StockApplication.Models;
using StockApplication.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace StockApplication.Repositories
{
    // Composite model for joined Stock and Supplier data
    public class StockWithSupplierInfo
    {
        // Stock properties
        public int StockID { get; set; }
        public string Stock_Name { get; set; } // Changed from ProductName to match Stock model
        public string Type { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; }
        public decimal Price { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? SupplierID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Supplier properties
        public string SupplierName { get; set; }
        public string ContactInfo { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }

    // Specific repository for Stock
    public class StockRepository : Repository<Stock>
    {
        private readonly DatabaseService _databaseService;
        private SQLiteAsyncConnection _connection;

        public StockRepository(DatabaseService databaseService) : base(databaseService)
        {
            _databaseService = databaseService;
        }

        private async Task EnsureConnectionAsync()
        {
            try
            {
                if (_connection == null)
                {
                    _connection = await _databaseService.GetConnection();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in EnsureConnectionAsync: {ex.Message}");
                throw;
            }
        }

        // Get all stocks from the database - reliable method we'll use as foundation
        public async Task<List<Stock>> GetAllStocksAsync()
        {
            try
            {
                await EnsureConnectionAsync();
                return await _connection.Table<Stock>().ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAllStocksAsync: {ex.Message}");
                throw;
            }
        }

        // Safe method to get stock by ID - uses GetAllStocksAsync internally
        public async Task<Stock> GetStockByIdSafelyAsync(int id)
        {
            try
            {
                var allStocks = await GetAllStocksAsync();
                return allStocks.FirstOrDefault(s => s.StockID == id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetStockByIdSafelyAsync: {ex.Message}");
                throw;
            }
        }

        // Safe update method
        public async Task<bool> UpdateStockSafelyAsync(Stock stock)
        {
            if (stock == null)
                return false;

            try
            {
                await EnsureConnectionAsync();
                int result = await _connection.UpdateAsync(stock);
                return result > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateStockSafelyAsync: {ex.Message}");
                throw;
            }
        }

        // Get stock with supplier information using JOIN
        public async Task<List<StockWithSupplierInfo>> GetStockWithSupplierAsync()
        {
            try
            {
                await EnsureConnectionAsync();
                
                // Use raw SQL query to perform JOIN
                var query = @"SELECT s.StockID, 
                             s.Stock_Name, 
                             s.Type,
                             s.Quantity,
                             s.Unit,
                             s.Price,
                             s.ExpiryDate,
                             s.SupplierID,
                             s.UpdatedAt,
                             sup.SupplierName, 
                             sup.ContactInfo, 
                             sup.Address, 
                             sup.Email, 
                             sup.Phone 
                             FROM Stock s 
                             LEFT JOIN Supplier sup ON s.SupplierID = sup.SupplierID";
                
                var result = await _connection.QueryAsync<StockWithSupplierInfo>(query);
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetStockWithSupplierAsync: {ex.Message}");
                throw;
            }
        }

        // Convert StockWithSupplierInfo to Stock - used to keep compatibility with existing code
        public async Task<List<Stock>> GetStockWithSupplierAsStockAsync()
        {
            try
            {
                // Get the joined data
                var stockWithSupplierInfos = await GetStockWithSupplierAsync();
                
                // Convert to Stock objects
                List<Stock> stocks = new List<Stock>();
                foreach (var info in stockWithSupplierInfos)
                {
                    // Create a Stock object and copy properties
                    var stock = new Stock
                    {
                        StockID = info.StockID,
                        Stock_Name = info.Stock_Name,
                        Type = info.Type,
                        Quantity = info.Quantity,
                        Unit = info.Unit,
                        ExpiryDate = info.ExpiryDate,
                        SupplierID = info.SupplierID,
                        UpdatedAt = info.UpdatedAt
                    };
                    
                    stocks.Add(stock);
                }
                
                return stocks;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetStockWithSupplierAsStockAsync: {ex.Message}");
                throw;
            }
        }

        // Get stock items that are below a threshold
        public async Task<List<Stock>> GetLowStockAsync(double threshold)
        {
            try
            {
                await EnsureConnectionAsync();
                return await _connection.Table<Stock>()
                    .Where(s => s.Quantity < threshold)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetLowStockAsync: {ex.Message}");
                throw;
            }
        }

        // Get stock items that are about to expire - safe implementation
        public async Task<List<Stock>> GetNearExpiryAsync(int daysThreshold)
        {
            try
            {
                await EnsureConnectionAsync();
                
                // Get all stocks first
                var allStocks = await GetAllStocksAsync();
                
                // Filter in memory to avoid SQLite date calculation issues
                var thresholdDate = DateTime.Now.AddDays(daysThreshold);
                return allStocks
                    .Where(s => s.ExpiryDate.HasValue && 
                                s.ExpiryDate.Value < thresholdDate && 
                                s.ExpiryDate.Value > DateTime.Now)
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetNearExpiryAsync: {ex.Message}");
                throw;
            }
        }

        // Update stock quantity - safe implementation
        public async Task<int> UpdateStockQuantityAsync(int stockId, double newQuantity)
        {
            try
            {
                // Get the stock safely first
                var stock = await GetStockByIdSafelyAsync(stockId);
                
                if (stock != null)
                {
                    await EnsureConnectionAsync();
                    stock.Quantity = newQuantity;
                    stock.UpdatedAt = DateTime.Now;
                    return await _connection.UpdateAsync(stock);
                }
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateStockQuantityAsync: {ex.Message}");
                throw;
            }
        }

        // Fixed method for finding stocks by criteria - now with better error handling
        public new async Task<List<Stock>> FindAsync(Expression<Func<Stock, bool>> predicate)
        {
            try
            {
                await EnsureConnectionAsync();
                
                // Get all stocks first
                var allStocks = await GetAllStocksAsync();
                
                // Compile the expression to a delegate and filter in memory
                var compiledPredicate = predicate.Compile();
                return allStocks.Where(compiledPredicate).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in FindAsync: {ex.Message}");
                throw;
            }
        }
        
        // Get low stock items with supplier information using JOIN
        public async Task<List<StockWithSupplierInfo>> GetLowStockWithSupplierAsync(double threshold)
        {
            try
            {
                await EnsureConnectionAsync();
            
                var query = @"SELECT s.StockID, 
                             s.Stock_Name, 
                             s.Type,
                             s.Quantity,
                             s.Unit,
                             s.Price,
                             s.ExpiryDate,
                             s.SupplierID,
                             s.UpdatedAt,
                             sup.SupplierName, 
                             sup.ContactInfo, 
                             sup.Address, 
                             sup.Email, 
                             sup.Phone 
                             FROM Stock s 
                             LEFT JOIN Supplier sup ON s.SupplierID = sup.SupplierID
                             WHERE s.Quantity < ?";
                
                var result = await _connection.QueryAsync<StockWithSupplierInfo>(query, threshold);
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetLowStockWithSupplierAsync: {ex.Message}");
                throw;
            }
        }
        
        // Convert low stock with supplier info to Stock objects
        public async Task<List<Stock>> GetLowStockWithSupplierAsStockAsync(double threshold)
        {
            try
            {
                // Get the joined data
                var lowStockWithSupplierInfos = await GetLowStockWithSupplierAsync(threshold);
                
                // Convert to Stock objects
                List<Stock> stocks = new List<Stock>();
                foreach (var info in lowStockWithSupplierInfos)
                {
                    // Create a Stock object and copy properties
                    var stock = new Stock
                    {
                        StockID = info.StockID,
                        Stock_Name = info.Stock_Name,
                        Type = info.Type,
                        Quantity = info.Quantity,
                        Unit = info.Unit,
                        ExpiryDate = info.ExpiryDate,
                        SupplierID = info.SupplierID,
                        UpdatedAt = info.UpdatedAt
                    };
                    
                    stocks.Add(stock);
                }
                
                return stocks;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetLowStockWithSupplierAsStockAsync: {ex.Message}");
                throw;
            }
        }
    }
}
using System;
using SQLite;
using System.Collections.Generic;

namespace StockApplication.Models;

public class Stock
    {
        [PrimaryKey, AutoIncrement]
        public int StockID { get; set; }
        
        [NotNull]
        public string Stock_Name { get; set; }
        
        public string Type { get; set; }
        
        [NotNull]
        public double Quantity { get; set; }
        
        public string Unit { get; set; }
        
        public DateTime? ExpiryDate { get; set; }
        
        [Indexed]
        public int? SupplierID { get; set; }
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
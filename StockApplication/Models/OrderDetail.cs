using System;
using SQLite;
using System.Collections.Generic;
namespace StockApplication.Models;

public class OrderDetail
    {
        [PrimaryKey, AutoIncrement]
        public int OrderDetailID { get; set; }
        
        [Indexed]
        public int OrderID { get; set; }
        
        [Indexed]
        public int MenuID { get; set; }
        
        public int Quantity { get; set; } = 1;
        
        public double Subtotal { get; set; }
        
        [Ignore]
        public MenuItem MenuItem { get; set; }
    }

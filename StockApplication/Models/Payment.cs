using System;
using SQLite;
using System.Collections.Generic;

namespace StockApplication.Models;

public class Payment
    {
        [PrimaryKey, AutoIncrement]
        public int PaymentID { get; set; }
        
        [Indexed]
        public int OrderID { get; set; }
        
        [Indexed]
        public int? MemberID { get; set; }
        
        public double Total { get; set; }
        
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        
        public string PaymentMethod { get; set; }
        
        public int PointsEarned { get; set; }

        [Indexed]
        public int? TableID { get; set; }
    }

using System;
using SQLite;
using System.Collections.Generic;

namespace StockApplication.Models;

public class Order
{
    [PrimaryKey, AutoIncrement]
    public int OrderID { get; set; }
    
    [Indexed]
    public int? StaffID { get; set; }
    
    [Indexed]
    public int? MemberID { get; set; }

    // Add this new property for table association
    [Indexed]
    public int? TableID { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;
    
    public double TotalAmount { get; set; }
    
    public string PaymentStatus { get; set; } = "Pending";
    
    public string Notes { get; set; } // Added Notes property
    
    [Ignore]
    public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
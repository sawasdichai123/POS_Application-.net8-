using System;
using SQLite;
using System.Collections.Generic;

namespace StockApplication.Models;

public class Customer
    {
        [PrimaryKey, AutoIncrement]
        public int MemberID { get; set; }
        
        [NotNull]
        public string Cus_Name { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        
        public string Cus_Phone { get; set; }
        
        public string Email { get; set; }
        
        public int Points { get; set; }
    }
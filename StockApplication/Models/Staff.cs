using System;
using SQLite;
using System.Collections.Generic;

namespace StockApplication.Models;

public class Staff
    {
        [PrimaryKey, AutoIncrement]
        public int StaffID { get; set; }
        
        [NotNull]
        public string Staff_Name { get; set; }
        
        public string Position { get; set; }
        
        public string Staff_Phone { get; set; }
        
        public double? Salary { get; set; }
        
        public DateTime? HireDate { get; set; }
    }

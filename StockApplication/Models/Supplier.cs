using System;
using SQLite;
using System.Collections.Generic;

namespace StockApplication.Models;

public class Supplier
    {
        [PrimaryKey, AutoIncrement]
        public int SupplierID { get; set; }
        
        [NotNull]
        public string Sup_name { get; set; }
        
        public string Sup_Phone { get; set; }
        
        public string Sup_Email { get; set; }
        
        public string Sup_Address { get; set; }
    }

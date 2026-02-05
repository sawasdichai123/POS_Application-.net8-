using System;
using SQLite;
using System.Collections.Generic;


public class MenuItem
    {
        [PrimaryKey, AutoIncrement]
        public int MenuID { get; set; }
        
        [NotNull]
        public string Menu_Name { get; set; }
        
        public string Category { get; set; }
        
        public string Description { get; set; }
        
        [NotNull]
        public double Price { get; set; }
    }
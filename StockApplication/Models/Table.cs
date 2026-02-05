using SQLite;

namespace StockApplication.Models
{
    [Table("Tables")]
    public class Table
    {
        [PrimaryKey, AutoIncrement]
        public int TableID { get; set; }
        
        public string TableName { get; set; }
        
        public string Status { get; set; } // Available, Occupied, Reserved
        
        public int Capacity { get; set; }
        
        public string Location { get; set; } // e.g., "Main Floor", "Patio", etc.
    }
}
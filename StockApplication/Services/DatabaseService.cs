using SQLite;
using StockApplication.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace StockApplication.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;
        private bool _isInitialized = false;

        public DatabaseService()
        {
            // Constructor is empty
        }

        public async Task Init()
        {
            try
            {
                if (_database is not null && _isInitialized)
                {
                    Debug.WriteLine("Database is already initialized, skipping initialization");
                    return;
                }

                Debug.WriteLine("Initializing database...");
                
                // Get the database path
                string databasePath = Path.Combine(FileSystem.AppDataDirectory, "stock.db");
                Debug.WriteLine($"Database path: {databasePath}");
                Console.WriteLine($"Database path: {databasePath}");

                // Create the database directory if it doesn't exist
                string databaseDirectory = Path.GetDirectoryName(databasePath);
                if (!Directory.Exists(databaseDirectory))
                {
                    Debug.WriteLine($"Creating database directory: {databaseDirectory}");
                    Directory.CreateDirectory(databaseDirectory);
                }

                // Create the connection with explicit flags
                _database = new SQLiteAsyncConnection(databasePath, SQLiteOpenFlags.Create | 
                                                     SQLiteOpenFlags.ReadWrite | 
                                                     SQLiteOpenFlags.SharedCache);

                // Create all tables
                Debug.WriteLine("Creating Supplier table...");
                await _database.CreateTableAsync<Supplier>();
                
                Debug.WriteLine("Creating Stock table...");
                await _database.CreateTableAsync<Stock>();
                
                Debug.WriteLine("Creating MenuItem table...");
                await _database.CreateTableAsync<MenuItem>();
                
                Debug.WriteLine("Creating Customer table...");
                await _database.CreateTableAsync<Customer>();
                
                Debug.WriteLine("Creating Staff table...");
                await _database.CreateTableAsync<Staff>();
                
                Debug.WriteLine("Creating Order table...");
                await _database.CreateTableAsync<Order>();
                
                Debug.WriteLine("Creating OrderDetail table...");
                await _database.CreateTableAsync<OrderDetail>();
                
                Debug.WriteLine("Creating Payment table...");
                await _database.CreateTableAsync<Payment>();
                
                Debug.WriteLine("Creating Table table...");
                await _database.CreateTableAsync<Table>();
                
                Debug.WriteLine("Creating User table...");
                await _database.CreateTableAsync<User>();
                
                _isInitialized = true;
                Debug.WriteLine("Database initialization complete");
                
                // Run migrations after initialization
                await MigrateDatabase();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database initialization failed: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Re-throw the exception to be handled by the caller
                throw new Exception($"Failed to initialize database: {ex.Message}", ex);
            }
        }

        public async Task MigrateDatabase()
        {
            try
            {
                Debug.WriteLine("Running database migrations...");
                
                // Ensure database is initialized
                if (_database == null)
                {
                    await Init();
                }
                
                // Add Notes column to Order table if it doesn't exist
                Debug.WriteLine("Migrating Order table to add Notes column...");
                
                // Check if column exists first to avoid errors
                var tableInfo = await _database.QueryAsync<TableColumnInfo>(
                    "PRAGMA table_info(\"Order\")");
                
                bool hasNotesColumn = tableInfo.Any(c => c.name == "Notes");
                
                if (!hasNotesColumn)
                {
                    Debug.WriteLine("Notes column doesn't exist, adding it now...");
                    await _database.ExecuteAsync("ALTER TABLE \"Order\" ADD COLUMN Notes TEXT;");
                    Debug.WriteLine("Notes column added successfully");
                }
                else
                {
                    Debug.WriteLine("Notes column already exists, skipping migration");
                }
                
                // Check if TableID column exists in Order table
                bool hasOrderTableIdColumn = tableInfo.Any(c => c.name == "TableID");
                
                if (!hasOrderTableIdColumn)
                {
                    Debug.WriteLine("TableID column doesn't exist in Order table, adding it now...");
                    await _database.ExecuteAsync("ALTER TABLE \"Order\" ADD COLUMN TableID INTEGER;");
                    Debug.WriteLine("TableID column added to Order table successfully");
                }
                else
                {
                    Debug.WriteLine("TableID column already exists in Order table, skipping migration");
                }
                
                // Check if TableID column exists in Payment table
                var paymentTableInfo = await _database.QueryAsync<TableColumnInfo>(
                    "PRAGMA table_info(Payment)");
                
                bool hasPaymentTableIdColumn = paymentTableInfo.Any(c => c.name == "TableID");
                
                if (!hasPaymentTableIdColumn)
                {
                    Debug.WriteLine("TableID column doesn't exist in Payment table, adding it now...");
                    await _database.ExecuteAsync("ALTER TABLE Payment ADD COLUMN TableID INTEGER;");
                    Debug.WriteLine("TableID column added to Payment table successfully");
                }
                else
                {
                    Debug.WriteLine("TableID column already exists in Payment table, skipping migration");
                }
                
                // Check if User table exists, if not create it
                var tables = await _database.QueryAsync<TableInfo>("SELECT name FROM sqlite_master WHERE type='table'");
                bool userTableExists = tables.Any(t => t.name == "User");
                
                if (!userTableExists)
                {
                    Debug.WriteLine("User table doesn't exist, creating it now...");
                    await _database.CreateTableAsync<User>();
                    Debug.WriteLine("User table created successfully");
                }
                
                Debug.WriteLine("Database migrations completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database migration failed: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Log error but don't throw, allow app to continue
                Console.WriteLine($"Error during database migration: {ex.Message}");
            }
        }

        public async Task<SQLiteAsyncConnection> GetConnection()
        {
            await Init();
            return _database;
        }

        // Seed initial data for testing
        public async Task SeedDataAsync()
        {
            try
            {
                await Init();

                // Check if we already have data
                var suppliersCount = await _database.Table<Supplier>().CountAsync();
                Debug.WriteLine($"Found {suppliersCount} suppliers in database");
                
                if (suppliersCount > 0)
                {
                    Debug.WriteLine("Database already has supplier data");
                }
                else
                {
                    Debug.WriteLine("Seeding database with initial supplier data...");

                    // Add some suppliers
                    var supplier1 = new Supplier
                    {
                        Sup_name = "บริษัท ABC ซัพพลายเออร์",
                        Sup_Phone = "02-123-4567",
                        Sup_Email = "contact@abcsuppliers.com",
                        Sup_Address = "123 ถนนสาทร กรุงเทพฯ"
                    };

                    var supplier2 = new Supplier
                    {
                        Sup_name = "บริษัท XYZ ดีสทริบิวเตอร์",
                        Sup_Phone = "02-987-6543",
                        Sup_Email = "info@xyzdist.com",
                        Sup_Address = "456 ถนนสุขุมวิท กรุงเทพฯ"
                    };

                    var supplier3 = new Supplier
                    {
                        Sup_name = "บริษัท เฟรชฟู้ดส์ จำกัด",
                        Sup_Phone = "02-321-6540",
                        Sup_Email = "sales@freshfoodsco.com",
                        Sup_Address = "789 ถนนพระราม 4 กรุงเทพฯ"
                    };

                    var supplier4 = new Supplier
                    {
                        Sup_name = "กรีนมาร์เก็ต",
                        Sup_Phone = "02-654-3210",
                        Sup_Email = "contact@greenmarket.com",
                        Sup_Address = "101 ถนนวิภาวดีรังสิต กรุงเทพฯ"
                    };

                    var supplier5 = new Supplier
                    {
                        Sup_name = "โอเชียนิค ซีฟู้ดส์",
                        Sup_Phone = "02-987-1234",
                        Sup_Email = "support@oceanicseafoods.com",
                        Sup_Address = "234 ถนนบางนา กรุงเทพฯ"
                    };

                    var supplier6 = new Supplier
                    {
                        Sup_name = "กูร์เมต์ ดีสทริบิวเตอร์",
                        Sup_Phone = "02-543-9876",
                        Sup_Email = "info@gourmetdistributors.com",
                        Sup_Address = "567 ถนนราชดำเนิน กรุงเทพฯ"
                    };

                    var supplier7 = new Supplier
                    {
                        Sup_name = "ซันไชน์ฟาร์ม",
                        Sup_Phone = "02-432-5678",
                        Sup_Email = "sunshine@farms.com",
                        Sup_Address = "890 ถนนเกษตร กรุงเทพฯ"
                    };

                    var supplier8 = new Supplier
                    {
                        Sup_name = "เฮลท์ตี้ฟู้ดส์ อินคอร์ปอเรชั่น",
                        Sup_Phone = "02-876-5432",
                        Sup_Email = "orders@healthyfoods.com",
                        Sup_Address = "345 ถนนสุขุมวิทย์ กรุงเทพฯ"
                    };

                    var supplier9 = new Supplier
                    {
                        Sup_name = "เนเชอรัล ดีสทริบิวเตอร์",
                        Sup_Phone = "02-567-8901",
                        Sup_Email = "contact@naturaldist.com",
                        Sup_Address = "678 ถนนพหลโยธิน กรุงเทพฯ"
                    };

                    var supplier10 = new Supplier
                    {
                        Sup_name = "ออร์บัน โกรเซอร์ส",
                        Sup_Phone = "02-234-5678",
                        Sup_Email = "info@urbangrocers.com",
                        Sup_Address = "123 ถนนราชประสงค์ กรุงเทพฯ"
                    };

                    var supplier11 = new Supplier
                    {
                        Sup_name = "เมาท์เทน ซัพพลาย จำกัด",
                        Sup_Phone = "02-345-6789",
                        Sup_Email = "mountain@supplies.com",
                        Sup_Address = "456 ถนนแจ้งวัฒนะ กรุงเทพฯ"
                    };

                    var supplier12 = new Supplier
                    {
                        Sup_name = "คันทรี กู๊ดส์",
                        Sup_Phone = "02-765-4321",
                        Sup_Email = "contact@countrygoods.com",
                        Sup_Address = "789 ถนนพระราม 9 กรุงเทพฯ"
                    };

                    var supplier13 = new Supplier
                    {
                        Sup_name = "เอลีท โฮลเซล",
                        Sup_Phone = "02-543-6789",
                        Sup_Email = "sales@elitewholesale.com",
                        Sup_Address = "101 ถนนบางกอก กรุงเทพฯ"
                    };

                    var supplier14 = new Supplier
                    {
                        Sup_name = "โลคอล มาร์เก็ตส์",
                        Sup_Phone = "02-876-3217",
                        Sup_Email = "support@localmarkets.com",
                        Sup_Address = "234 ถนนสุขุมวิท กรุงเทพฯ"
                    };


                    await _database.InsertAsync(supplier1);
                    await _database.InsertAsync(supplier2);
                    await _database.InsertAsync(supplier3);
                    await _database.InsertAsync(supplier4);
                    await _database.InsertAsync(supplier5);
                    await _database.InsertAsync(supplier6);
                    await _database.InsertAsync(supplier7);
                    await _database.InsertAsync(supplier8);
                    await _database.InsertAsync(supplier9);
                    await _database.InsertAsync(supplier10);
                    await _database.InsertAsync(supplier11);
                    await _database.InsertAsync(supplier12);
                    await _database.InsertAsync(supplier13);
                    await _database.InsertAsync(supplier14);
                    Debug.WriteLine("Added suppliers");

                    // Add some stock items
                    
                    await _database.InsertAsync(new Stock
                    {
                        Stock_Name = "ปูม้า",  
                        Quantity = 20,  
                        Unit = "kg",  // ใช้หน่วย 'kg' สำหรับปูม้า
                        ExpiryDate = DateTime.Now.AddMonths(6),  
                        SupplierID = 1  
                    });

                    await _database.InsertAsync(new Stock
                    {
                        Stock_Name = "กระเทียม",
                        Quantity = 10,
                        Unit = "kg",  // ใช้หน่วย 'kg' สำหรับกระเทียม
                        ExpiryDate = DateTime.Now.AddMonths(3),
                        SupplierID = 2
                    });

                    await _database.InsertAsync(new Stock
                    {
                        Stock_Name = "พริก",
                        Quantity = 5,
                        Unit = "kg",  // ใช้หน่วย 'kg' สำหรับพริก
                        ExpiryDate = DateTime.Now.AddMonths(2),
                        SupplierID = 3
                    });

                    await _database.InsertAsync(new Stock
                    {
                        Stock_Name = "มะนาว",
                        Quantity = 30,
                        Unit = "kg",  // ใช้หน่วย 'kg' สำหรับมะนาว
                        ExpiryDate = DateTime.Now.AddMonths(1),
                        SupplierID = 4
                    });

                    await _database.InsertAsync(new Stock
                    {
                        Stock_Name = "น้ำตาล",
                        Quantity = 50,
                        Unit = "kg",  // ใช้หน่วย 'kg' สำหรับน้ำตาล
                        ExpiryDate = DateTime.Now.AddMonths(12),
                        SupplierID = 5
                    });

                    await _database.InsertAsync(new Stock
                    {
                        Stock_Name = "ซอสถั่วเหลือง",
                        Quantity = 15,
                        Unit = "L",  // ใช้หน่วย 'L' สำหรับซอสถั่วเหลือง
                        ExpiryDate = DateTime.Now.AddMonths(18),
                        SupplierID = 6
                    });

                    await _database.InsertAsync(new Stock
                    {
                        Stock_Name = "ไข่",
                        Quantity = 100,
                        Unit = "pieces",  // ใช้หน่วย 'pieces' สำหรับไข่
                        ExpiryDate = DateTime.Now.AddMonths(1),
                        SupplierID = 7
                    });

                    await _database.InsertAsync(new Stock
                    {
                        Stock_Name = "แครอท",
                        Quantity = 20,
                        Unit = "kg",  // ใช้หน่วย 'kg' สำหรับแครอท
                        ExpiryDate = DateTime.Now.AddMonths(2),
                        SupplierID = 8
                    });

                    await _database.InsertAsync(new Stock
                    {
                        Stock_Name = "บร็อคโคลี",
                        Quantity = 10,
                        Unit = "kg",  // ใช้หน่วย 'kg' สำหรับบร็อคโคลี
                        ExpiryDate = DateTime.Now.AddMonths(3),
                        SupplierID = 9
                    });

                    await _database.InsertAsync(new Stock
                    {
                        Stock_Name = "มะพร้าว",
                        Quantity = 15,
                        Unit = "pieces",  // ใช้หน่วย 'pieces' สำหรับมะพร้าว
                        ExpiryDate = DateTime.Now.AddMonths(6),
                        SupplierID = 10
                    });

                    await _database.InsertAsync(new Stock
                    {
                        Stock_Name = "ข้าวเหนียว",
                        Quantity = 25,
                        Unit = "kg",  // ใช้หน่วย 'kg' สำหรับข้าวเหนียว
                        ExpiryDate = DateTime.Now.AddMonths(9),
                        SupplierID = 11
                    });

                    await _database.InsertAsync(new Stock
                    {
                        Stock_Name = "มะม่วง",
                        Quantity = 40,
                        Unit = "kg",  // ใช้หน่วย 'kg' สำหรับมะม่วง
                        ExpiryDate = DateTime.Now.AddMonths(1),
                        SupplierID = 12
                    });

                    await _database.InsertAsync(new Stock
                    {
                        Stock_Name = "น้ำแข็ง",
                        Quantity = 50,
                        Unit = "kg",  // ใช้หน่วย 'kg' สำหรับน้ำแข็ง
                        ExpiryDate = DateTime.Now.AddMonths(12),
                        SupplierID = 13
                    });

                    await _database.InsertAsync(new Stock
                    {
                        Stock_Name = "ชา",
                        Quantity = 30,
                        Unit = "kg",  // ใช้หน่วย 'kg' สำหรับชา
                        ExpiryDate = DateTime.Now.AddMonths(18),
                        SupplierID = 14
                    });
                    Debug.WriteLine("Added stock items");

                    // Add menu items
                    await _database.InsertAsync(new MenuItem
                    {
                        Menu_Name = "ปูม้านึ่ง",
                        Category = "Main",
                        Description = "ปูม้านึ่งสด ๆ พร้อมน้ำจิ้มซีฟู้ดรสจัดจ้าน",
                        Price = 499.00
                    });

                    await _database.InsertAsync(new MenuItem
                    {
                        Menu_Name = "กุ้งย่าง",
                        Category = "Main",
                        Description = "กุ้งย่างทาด้วยเนยกระเทียมและสมุนไพร",
                        Price = 399.00
                    });

                    await _database.InsertAsync(new MenuItem
                    {
                        Menu_Name = "ข้าวผัด",
                        Category = "Side",
                        Description = "ข้าวผัดแสนอร่อยผสมผักและไข่",
                        Price = 199.00
                    });

                    await _database.InsertAsync(new MenuItem
                    {
                        Menu_Name = "ผักลวก",
                        Category = "Side",
                        Description = "ผักสดลวกสุกพอดีสำหรับเสิร์ฟคู่กับเมนูหลัก",
                        Price = 129.00
                    });

                    await _database.InsertAsync(new MenuItem
                    {
                        Menu_Name = "ไอศกรีมมะพร้าว",
                        Category = "Dessert",
                        Description = "ไอศกรีมมะพร้าวเนื้อนุ่มหอมมะพร้าว",
                        Price = 150.00
                    });

                    await _database.InsertAsync(new MenuItem
                    {
                        Menu_Name = "ข้าวเหนียวมะม่วง",
                        Category = "Dessert",
                        Description = "ข้าวเหนียวหวานคู่กับมะม่วงสุกหวานหอมและน้ำกะทิ",
                        Price = 180.00
                    });

                    await _database.InsertAsync(new MenuItem
                    {
                        Menu_Name = "น้ำมะนาว",
                        Category = "Beverage",
                        Description = "น้ำมะนาวสดชื่น ๆ รสเปรี้ยวหวานลงตัว",
                        Price = 59.00
                    });

                    await _database.InsertAsync(new MenuItem
                    {
                        Menu_Name = "ชานมเย็น",
                        Category = "Beverage",
                        Description = "ชานมเย็นเข้มข้นทานง่าย",
                        Price = 49.00
                    });

                    await _database.InsertAsync(new MenuItem
                    {
                        Menu_Name = "ชุดทะเลรวมมิตร",
                        Category = "Other",
                        Description = "รวมปูม้า กุ้ง และหอยนางรมสด พร้อมน้ำจิ้มซีฟู้ด",
                        Price = 799.00
                    });

                    await _database.InsertAsync(new MenuItem
                    {
                        Menu_Name = "น้ำจิ้มซีฟู้ด",
                        Category = "Other",
                        Description = "น้ำจิ้มซีฟู้ดรสจัดจ้านแถมให้กับทุกเมนูทะเล",
                        Price = 30.00
                    });
                    Debug.WriteLine("Added menu items");

                    // Add staff members with returning IDs for user linking
                    int managerStaffId = await _database.InsertAsync(new Staff
                    {
                        Staff_Name = "John Doe",
                        Position = "Manager",
                        Staff_Phone = "0812345678",
                        Salary = 50000,
                        HireDate = DateTime.Now.AddYears(-2)
                    });
                    
                    int chefStaffId = await _database.InsertAsync(new Staff
                    {
                        Staff_Name = "Jane Smith",
                        Position = "Chef",
                        Staff_Phone = "0823456789",
                        Salary = 40000,
                        HireDate = DateTime.Now.AddYears(-1)
                    });
                    
                    int waiterStaffId = await _database.InsertAsync(new Staff
                    {
                        Staff_Name = "Tom Wilson",
                        Position = "Waiter",
                        Staff_Phone = "0834567890",
                        Salary = 25000,
                        HireDate = DateTime.Now.AddMonths(-6)
                    });
                    Debug.WriteLine("Added staff members");
                    
                    // Add users linked to staff members
                    await _database.InsertAsync(new User
                    {
                        Username = "john.manager",
                        Password = PasswordHasher.HashPassword("manager123"),
                        Role = "Manager",
                        StaffID = 1,
                        StaffName = "John Doe",
                        LastLogin = DateTime.Now,
                        IsActive = true
                    });
                    
                    await _database.InsertAsync(new User
                    {
                        Username = "jane.chef",
                        Password = PasswordHasher.HashPassword("chef123"),
                        Role = "Staff",
                        StaffID = 2,
                        StaffName = "Jane Smith",
                        LastLogin = DateTime.Now.AddDays(-1),
                        IsActive = true
                    });
                    
                    await _database.InsertAsync(new User
                    {
                        Username = "tom.waiter",
                        Password = PasswordHasher.HashPassword("waiter123"),
                        Role = "Staff",
                        StaffID = 3,
                        StaffName = "Tom Wilson",
                        LastLogin = DateTime.Now.AddDays(-2),
                        IsActive = true
                    });
                    
                    
                    Debug.WriteLine("Added users");

                    // Add a customer
                    await _database.InsertAsync(new Customer
                    {
                        Cus_Name = "Jane Smith",
                        DateOfBirth = DateTime.Parse("1990-01-15"),
                        Cus_Phone = "0923456789",
                        Email = "jane@example.com",
                        Points = 100
                    });

                    await _database.InsertAsync(new Customer
                    {
                        Cus_Name = "Emily Brown",
                        DateOfBirth = DateTime.Parse("1995-03-10"),
                        Cus_Phone = "0933456789",
                        Email = "emily.brown@example.com",
                        Points = 350
                    });

                    await _database.InsertAsync(new Customer
                    {
                        Cus_Name = "Michael Green",
                        DateOfBirth = DateTime.Parse("1988-11-02"),
                        Cus_Phone = "0944567890",
                        Email = "michael.green@example.com",
                        Points = 500
                    });

                    await _database.InsertAsync(new Customer
                    {
                        Cus_Name = "Sophia Taylor",
                        DateOfBirth = DateTime.Parse("2000-06-15"),
                        Cus_Phone = "0955678901",
                        Email = "sophia.taylor@example.com",
                        Points = 150
                    });

                    await _database.InsertAsync(new Customer
                    {
                        Cus_Name = "Liam Johnson",
                        DateOfBirth = DateTime.Parse("1993-12-05"),
                        Cus_Phone = "0966789012",
                        Email = "liam.johnson@example.com",
                        Points = 75
                    });



                    Debug.WriteLine("Added customer");
                    
                   
                }
                
                // Seed the tables using the dedicated method
                await SeedTablesAsync();
                
                Debug.WriteLine("Database seeding complete");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database seeding failed: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new Exception($"Failed to seed database: {ex.Message}", ex);
            }
        }



        // Seed default users
        public async Task SeedUsersAsync()
        {
            try
            {
                await Init();
                
                // Check if we already have users
                var userTable = await _database.Table<User>().CountAsync();
                
                if (userTable == 0)
                {
                    Debug.WriteLine("Seeding default users...");
                    
                    // Add a default manager account with hashed password
                    var manager = new User
                    {
                        Username = "manager",
                        Password = PasswordHasher.HashPassword("manager123"),
                        Role = "Manager",
                        IsActive = true
                    };
                    
                    // Add a default staff account with hashed password
                    var staff = new User
                    {
                        Username = "staff",
                        Password = PasswordHasher.HashPassword("staff123"),
                        Role = "Staff",
                        IsActive = true
                    };
                    
                    await _database.InsertAsync(manager);
                    await _database.InsertAsync(staff);
                    
                    Debug.WriteLine("Default users created successfully");
                }
                else
                {
                    Debug.WriteLine($"Found {userTable} existing users, skipping seeding");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to seed users: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        // Dedicated method for table seeding
        public async Task SeedTablesAsync()
        {
            try
            {
                await Init();
                
                // Check if we have tables already
                var tablesCount = await _database.Table<Table>().CountAsync();
                Debug.WriteLine($"Found {tablesCount} tables in database");
                
                if (tablesCount == 0)
                {
                    Debug.WriteLine("Seeding tables data...");
                    
                    // Add sample tables - tables 1-10 plus Take Away option
                    var tables = new List<Table>
                    {
                        new Table { TableName = "Table 1", Status = "Available", Capacity = 4, Location = "Main Floor" },
                        new Table { TableName = "Table 2", Status = "Available", Capacity = 4, Location = "Main Floor" },
                        new Table { TableName = "Table 3", Status = "Available", Capacity = 4, Location = "Main Floor" },
                        new Table { TableName = "Table 4", Status = "Available", Capacity = 4, Location = "Main Floor" },
                        new Table { TableName = "Table 5", Status = "Available", Capacity = 4, Location = "Main Floor" },
                        new Table { TableName = "Table 6", Status = "Available", Capacity = 4, Location = "Main Floor" },
                        new Table { TableName = "Table 7", Status = "Available", Capacity = 4, Location = "Main Floor" },
                        new Table { TableName = "Table 8", Status = "Available", Capacity = 4, Location = "Main Floor" },
                        new Table { TableName = "Table 9", Status = "Available", Capacity = 4, Location = "Main Floor" },
                        new Table { TableName = "Table 10", Status = "Available", Capacity = 4, Location = "Main Floor" },
                        new Table { TableName = "Take Away", Status = "Available", Capacity = 0, Location = "Take Away" }
                    };

                    foreach (var table in tables)
                    {
                        await _database.InsertAsync(table);
                    }
                    
                    Debug.WriteLine("Added tables");
                }
                else
                {
                    // Check if we need to add the Take Away option if it doesn't exist
                    var takeAwayExists = await _database.Table<Table>()
                        .Where(t => t.TableName == "Take Away")
                        .CountAsync() > 0;
                        
                    if (!takeAwayExists)
                    {
                        Debug.WriteLine("Adding Take Away option...");
                        await _database.InsertAsync(new Table 
                        { 
                            TableName = "Take Away", 
                            Status = "Available", 
                            Capacity = 0, 
                            Location = "Take Away" 
                        });
                    }
                    
                    // Check and add any missing table numbers (1-10)
                    for (int i = 1; i <= 10; i++)
                    {
                        string tableName = $"Table {i}";
                        var tableExists = await _database.Table<Table>()
                            .Where(t => t.TableName == tableName)
                            .CountAsync() > 0;
                            
                        if (!tableExists)
                        {
                            Debug.WriteLine($"Adding missing {tableName}...");
                            await _database.InsertAsync(new Table 
                            { 
                                TableName = tableName, 
                                Status = "Available", 
                                Capacity = 4, 
                                Location = "Main Floor" 
                            });
                        }
                    }
                }
                
                Debug.WriteLine("Table seeding complete");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Table seeding failed: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new Exception($"Failed to seed tables: {ex.Message}", ex);
            }
        }

        // Method to reset just the tables
        public async Task ResetTablesAsync()
        {
            try
            {
                await Init();
                
                // Get all existing tables
                var existingTables = await _database.Table<Table>().ToListAsync();
                
                // Delete all existing tables
                foreach (var table in existingTables)
                {
                    await _database.DeleteAsync(table);
                }
                
                Debug.WriteLine("All tables deleted");
                
                // Reseed tables
                await SeedTablesAsync();
                
                Debug.WriteLine("Tables reset successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to reset tables: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new Exception($"Failed to reset tables: {ex.Message}", ex);
            }
        }

        // Method to check if tables exist
        public async Task<bool> CheckTablesExistAsync()
        {
            try
            {
                await Init();

                // Query SQLite master table to check if our tables exist
                var tables = await _database.QueryAsync<TableInfo>("SELECT name FROM sqlite_master WHERE type='table'");
                
                foreach (var table in tables)
                {
                    Debug.WriteLine($"Found table: {table.name}");
                }
                
                bool stockExists = tables.Any(t => t.name == "Stock");
                Debug.WriteLine($"Stock table exists: {stockExists}");
                
                return stockExists;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to check tables: {ex.Message}");
                return false;
            }
        }

        // Helper method to reset database (for debugging)
        public async Task ResetDatabaseAsync()
        {
            try
            {
                if (_database != null)
                {
                    await _database.CloseAsync();
                    _database = null;
                }

                string databasePath = Path.Combine(FileSystem.AppDataDirectory, "stock.db");
                
                if (File.Exists(databasePath))
                {
                    Debug.WriteLine($"Deleting database file: {databasePath}");
                    File.Delete(databasePath);
                }
                
                _isInitialized = false;
                await Init();
                Debug.WriteLine("Database has been reset");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to reset database: {ex.Message}");
                throw;
            }
        }
        
        // Reset just user data (for testing)
        public async Task ResetUsersAsync()
        {
            try
            {
                await Init();
                
                // Delete all users
                await _database.ExecuteAsync("DELETE FROM User");
                Debug.WriteLine("All users deleted");
                
                // Reseed users
                await SeedUsersAsync();
                
                Debug.WriteLine("Users reset successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to reset users: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        // Method to fix user passwords
        public async Task FixUserPasswordsAsync()
        {
            try
            {
                await Init();
                
                var connection = await GetConnection();
                var users = await connection.Table<User>().ToListAsync();
                
                Debug.WriteLine($"Starting password fix for {users.Count} users");
                
                // Dictionary of usernames and their expected plaintext passwords
                var passwordMap = new Dictionary<string, string>
                {
                    { "john.manager", "manager123" },
                    { "jane.chef", "chef123" },
                    { "tom.waiter", "waiter123" },
                    { "manager", "manager123" },
                    { "staff", "staff123" },
                    { "system", "system123!" }
                };
                
                int fixedCount = 0;
                
                foreach (var user in users)
                {
                    // Skip users not in our map
                    if (!passwordMap.ContainsKey(user.Username))
                    {
                        Debug.WriteLine($"Skipping user {user.Username} - not in password map");
                        continue;
                    }
                    
                    // Get the expected password
                    string plainPassword = passwordMap[user.Username];
                    
                    // Check if current password is verifiable first
                    bool isCurrentVerifiable = false;
                    try
                    {
                        isCurrentVerifiable = PasswordHasher.VerifyPassword(plainPassword, user.Password);
                    }
                    catch (Exception vex)
                    {
                        Debug.WriteLine($"Password verification failed for {user.Username}: {vex.Message}");
                        isCurrentVerifiable = false;
                    }
                    
                    // Only update if current password doesn't verify
                    if (!isCurrentVerifiable)
                    {
                        // Hash it with the current implementation
                        string newHash = PasswordHasher.HashPassword(plainPassword);
                        
                        // Update the user record
                        user.Password = newHash;
                        await connection.UpdateAsync(user);
                        
                        fixedCount++;
                        Debug.WriteLine($"Updated password for {user.Username}");
                    }
                    else
                    {
                        Debug.WriteLine($"Password for {user.Username} already verifies correctly, skipping");
                    }
                }
                
                Debug.WriteLine($"Password fix completed - updated {fixedCount} users");
                
                // Verify all passwords after update
                await VerifyAllPasswordsAsync(passwordMap);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Password fix error: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        // Method to debug user accounts and verify if passwords work
        public async Task DebugUsersAsync()
        {
            try
            {
                await Init();
                
                var connection = await GetConnection();
                var users = await connection.Table<User>().ToListAsync();
                
                Debug.WriteLine($"DEBUG: Found {users.Count} users in database");
                
                foreach (var user in users)
                {
                    Debug.WriteLine($"DEBUG: User ID: {user.UserID}, Username: {user.Username}, Role: {user.Role}, Active: {user.IsActive}");
                    int hashLength = user.Password?.Length ?? 0;
                    Debug.WriteLine($"DEBUG: Password hash length: {hashLength}");
                    
                    if (hashLength > 0 && user.Password != null)
                    {
                        // Show the first few characters of the hash
                        Debug.WriteLine($"DEBUG: Password hash starts with: {user.Password.Substring(0, Math.Min(20, hashLength))}...");
                    }
                    else
                    {
                        Debug.WriteLine("DEBUG: WARNING - User has no password hash!");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DEBUG: Debug users error: {ex.Message}");
            }
        }
        
        // Method to verify if all passwords work after fixing
        private async Task VerifyAllPasswordsAsync(Dictionary<string, string> passwordMap)
        {
            try
            {
                var connection = await GetConnection();
                var users = await connection.Table<User>().ToListAsync();
                
                Debug.WriteLine("Verifying all user passwords after fix:");
                
                foreach (var user in users)
                {
                    // Skip users not in our map
                    if (!passwordMap.ContainsKey(user.Username))
                    {
                        Debug.WriteLine($"Skipping verification for {user.Username} - not in password map");
                        continue;
                    }
                    
                    // Get the expected password
                    string plainPassword = passwordMap[user.Username];
                    
                    // Try to verify
                    bool isVerified = false;
                    try
                    {
                        isVerified = PasswordHasher.VerifyPassword(plainPassword, user.Password);
                    }
                    catch (Exception vex)
                    {
                        Debug.WriteLine($"Verification error for {user.Username}: {vex.Message}");
                    }
                    
                    Debug.WriteLine($"User {user.Username} password verification: {(isVerified ? "SUCCESS" : "FAILED")}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Password verification error: {ex.Message}");
            }
        }
        
        // Improved SeedUsersAsync method with consistent password hashing
        public async Task SeedUsersAsyncImproved()
        {
            try
            {
                await Init();
                
                // Check if we already have users
                var userTable = await _database.Table<User>().CountAsync();
                
                if (userTable == 0)
                {
                    Debug.WriteLine("Seeding default users...");
                    
                    // Add a default manager account with hashed password
                    var manager = new User
                    {
                        Username = "manager",
                        Password = PasswordHasher.HashPassword("manager123"),
                        Role = "Manager",
                        IsActive = true
                    };
                    
                    // Add a default staff account with hashed password
                    var staff = new User
                    {
                        Username = "staff",
                        Password = PasswordHasher.HashPassword("staff123"),
                        Role = "Staff",
                        IsActive = true
                    };
                    
                    await _database.InsertAsync(manager);
                    await _database.InsertAsync(staff);
                    
                    Debug.WriteLine("Default users created successfully");
                    
                    // Debug the hash values to verify consistency
                    Debug.WriteLine($"Manager password hash first 20 chars: {manager.Password.Substring(0, Math.Min(20, manager.Password.Length))}...");
                    Debug.WriteLine($"Staff password hash first 20 chars: {staff.Password.Substring(0, Math.Min(20, staff.Password.Length))}...");
                    
                    // Verify the passwords work immediately after creation
                    bool managerVerifies = PasswordHasher.VerifyPassword("manager123", manager.Password);
                    bool staffVerifies = PasswordHasher.VerifyPassword("staff123", staff.Password);
                    
                    Debug.WriteLine($"Manager password verifies: {managerVerifies}");
                    Debug.WriteLine($"Staff password verifies: {staffVerifies}");
                }
                else
                {
                    Debug.WriteLine($"Found {userTable} existing users, running password fix");
                    await FixUserPasswordsAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to seed users: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        
        
        // Method to completely reset and rebuild the User table
        public async Task RebuildUserTableAsync()
        {
            try
            {
                await Init();
                
                Debug.WriteLine("Rebuilding User table completely...");
                
                // First, drop the existing table
                await _database.ExecuteAsync("DROP TABLE IF EXISTS User");
                Debug.WriteLine("Dropped existing User table");
                
                // Recreate the table
                await _database.CreateTableAsync<User>();
                Debug.WriteLine("Created new User table");
                
                // Check if staff members exist first
                var staffMembers = await _database.Table<Staff>().ToListAsync();
                var staffCount = staffMembers.Count;
                
                if (staffCount == 0)
                {
                    // We need to create staff members first since none exist
                    Debug.WriteLine("No staff members found, creating defaults...");
                    
                    // Create a manager staff member
                    int managerStaffId = await _database.InsertAsync(new Staff
                    {
                        Staff_Name = "John Doe",
                        Position = "Manager",
                        Staff_Phone = "0812345678",
                        Salary = 50000,
                        HireDate = DateTime.Now.AddYears(-2)
                    });
                    
                    // Create a chef staff member (with Staff role)
                    int chefStaffId = await _database.InsertAsync(new Staff
                    {
                        Staff_Name = "Jane Smith",
                        Position = "Chef",
                        Staff_Phone = "0823456789",
                        Salary = 40000,
                        HireDate = DateTime.Now.AddYears(-1)
                    });
                    
                    // Create a waiter staff member (with Staff role)
                    int waiterStaffId = await _database.InsertAsync(new Staff
                    {
                        Staff_Name = "Tom Wilson",
                        Position = "Waiter",
                        Staff_Phone = "0834567890",
                        Salary = 25000,
                        HireDate = DateTime.Now.AddMonths(-6)
                    });
                    
                    // Get the newly created staff
                    staffMembers = await _database.Table<Staff>().ToListAsync();
                    Debug.WriteLine($"Created {staffMembers.Count} default staff members");
                }
                
                // Now create users for all staff members
                foreach (var staffMember in staffMembers)
                {
                    string username = staffMember.Staff_Name.ToLower().Replace(" ", ".");
                    string password;
                    string role;
                    
                    // If the Position is exactly "Manager", give Manager role
                    // For all other positions including "Chef", "Waiter", etc, give Staff role
                    if (staffMember.Position == "Manager")
                    {
                        role = "Manager";
                        password = "manager123";
                    }
                    else
                    {
                        role = "Staff";
                        password = "staff123";
                    }
                    
                    // Create a user linked to this staff member
                    await _database.InsertAsync(new User
                    {
                        Username = username,
                        Password = PasswordHasher.HashPassword(password),
                        Role = role,
                        StaffID = staffMember.StaffID,
                        StaffName = staffMember.Staff_Name,
                        LastLogin = DateTime.Now.AddDays(-new Random().Next(0, 7)), // Random recent login
                        IsActive = true
                    });
                    
                    Debug.WriteLine($"Created user '{username}' for staff member '{staffMember.Staff_Name}' with role '{role}'");
                }
                
                // Also add the default accounts if needed
                var managerExists = await _database.Table<User>()
                    .Where(u => u.Username == "manager")
                    .CountAsync() > 0;
                    
                var staffExists = await _database.Table<User>()
                    .Where(u => u.Username == "staff")
                    .CountAsync() > 0;
                
                if (!managerExists)
                {
                    await _database.InsertAsync(new User
                    {
                        Username = "manager",
                        Password = PasswordHasher.HashPassword("manager123"),
                        Role = "Manager",
                        StaffID = null,
                        StaffName = null,
                        IsActive = true
                    });
                    Debug.WriteLine("Created generic 'manager' user");
                }
                
                if (!staffExists)
                {
                    await _database.InsertAsync(new User
                    {
                        Username = "staff",
                        Password = PasswordHasher.HashPassword("staff123"),
                        Role = "Staff",
                        StaffID = null,
                        StaffName = null,
                        IsActive = true
                    });
                    Debug.WriteLine("Created generic 'staff' user");
                }
                
                // Debug all users
                await DebugUsersAsync();
                
                Debug.WriteLine("User table rebuild complete");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"User table rebuild failed: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }

    // Helper class for SQLite table info query
    public class TableInfo
    {
        public string name { get; set; }
    }
    
    // Helper class for column information
    public class TableColumnInfo
    {
        public int cid { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public int notnull { get; set; }
        public string dflt_value { get; set; }
        public int pk { get; set; }
    }
}
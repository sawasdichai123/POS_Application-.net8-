using StockApplication.Services;
using StockApplication.Views;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace StockApplication
{
    public partial class App : Application
    {
        private readonly DatabaseService _databaseService;
        private static AuthService _authService;
        
        // Make AuthService available for binding in XAML
        public static AuthService AuthService => _authService;
        
        public App(DatabaseService databaseService, AuthService authService)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _authService = authService; // Set the static reference
            
            // Start with login page
            MainPage = new LoginPage(authService);
            
            // Initialize database in background
            InitializeDatabaseAsync();
        }
        
        private async void InitializeDatabaseAsync()
        {
            try
            {
                Debug.WriteLine("Starting database initialization...");
                
                // First, explicitly initialize the database
                await _databaseService.Init();
                
                // Check if tables exist
                bool tablesExist = await _databaseService.CheckTablesExistAsync();
                
                if (!tablesExist)
                {
                    Debug.WriteLine("Tables don't exist, resetting database...");
                    // Tables not created properly, try resetting
                    await _databaseService.ResetDatabaseAsync();
                }
                
                // Run migrations to ensure User table exists
                await _databaseService.MigrateDatabase();
                
                // Seed data if needed
                await _databaseService.SeedDataAsync();
                
                // Seed user accounts - using improved method
                await _databaseService.SeedUsersAsyncImproved();
                
                // Fix user passwords to ensure consistency
                await _databaseService.FixUserPasswordsAsync();
                
                // Debug all users
                await _databaseService.DebugUsersAsync();
                
                Debug.WriteLine("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                // Log the exception
                Debug.WriteLine($"Database initialization error: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Try to recover by resetting the database
                try
                {
                    Debug.WriteLine("Attempting database recovery...");
                    await _databaseService.ResetDatabaseAsync();
                    await _databaseService.SeedDataAsync();
                    await _databaseService.SeedUsersAsyncImproved();
                    await _databaseService.FixUserPasswordsAsync();
                    Debug.WriteLine("Database recovery successful");
                }
                catch (Exception recoveryEx)
                {
                    Debug.WriteLine($"Database recovery failed: {recoveryEx.Message}");
                    // At this point, we might want to show a user-facing error
                    // MainThread.BeginInvokeOnMainThread(async () => 
                    //     await Current.MainPage.DisplayAlert("Database Error", 
                    //         "There was a problem initializing the app's database. Please restart the app.", "OK"));
                }
            }
        }
        
        protected override void OnStart()
        {
            base.OnStart();
        }

        protected override void OnSleep()
        {
            base.OnSleep();
        }

        protected override void OnResume()
        {
            base.OnResume();
        }
    }
}
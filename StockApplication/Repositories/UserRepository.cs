using SQLite;
using StockApplication.Models;
using StockApplication.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StockApplication.Repositories
{
    public class UserRepository : Repository<User>
    {
        private readonly DatabaseService _databaseService;
        
        public UserRepository(DatabaseService databaseService) : base(databaseService)
        {
            _databaseService = databaseService;
        }
        
        // Authenticate user and return user if credentials are valid
        public async Task<User> AuthenticateAsync(string username, string password)
        {
            try
            {
                var connection = await _databaseService.GetConnection();
                
                // Get user by username using raw SQL
                var user = await connection.FindWithQueryAsync<User>(
                    "SELECT * FROM User WHERE Username = ? AND IsActive = 1", username);
                
                if (user != null && PasswordHasher.VerifyPassword(password, user.Password))
                {
                    // Update last login time
                    user.LastLogin = DateTime.Now;
                    await connection.UpdateAsync(user);
                    return user;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
                throw;
            }
        }
        
        // Create a new user with hashed password
        public async Task<User> CreateUserAsync(User user, string rawPassword)
        {
            try
            {
                // Hash the password before storing
                user.Password = PasswordHasher.HashPassword(rawPassword);
                
                var connection = await _databaseService.GetConnection();
                await connection.InsertAsync(user);
                
                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Create user error: {ex.Message}");
                throw;
            }
        }
        
        // Update an existing user
        public async Task<bool> UpdateUserAsync(User user, string newPassword = null)
        {
            try
            {
                var connection = await _databaseService.GetConnection();
                
                // If a new password was provided, hash it
                if (!string.IsNullOrEmpty(newPassword))
                {
                    user.Password = PasswordHasher.HashPassword(newPassword);
                }
                
                await connection.UpdateAsync(user);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update user error: {ex.Message}");
                return false;
            }
        }
        
        // Change user password
        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var connection = await _databaseService.GetConnection();
                var user = await connection.Table<User>()
                    .Where(u => u.UserID == userId)
                    .FirstOrDefaultAsync();
                
                if (user != null && PasswordHasher.VerifyPassword(currentPassword, user.Password))
                {
                    user.Password = PasswordHasher.HashPassword(newPassword);
                    await connection.UpdateAsync(user);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Change password error: {ex.Message}");
                return false;
            }
        }
        
        // Check if username is already taken
        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            try
            {
                var connection = await _databaseService.GetConnection();
                var count = await connection.Table<User>()
                    .Where(u => u.Username == username)
                    .CountAsync();
                
                return count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Username check error: {ex.Message}");
                throw;
            }
        }
        
        // Get all users (including their staff names if linked)
        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                var connection = await _databaseService.GetConnection();
                
                // Get all users using raw SQL
                var users = await connection.QueryAsync<User>("SELECT * FROM User");
                
                // Get all staff to populate StaffName property
                var staffList = await connection.Table<Staff>().ToListAsync();
                
                // Join users with staff to get staff names
                foreach (var user in users)
                {
                    if (user.StaffID.HasValue)
                    {
                        var staff = staffList.FirstOrDefault(s => s.StaffID == user.StaffID);
                        if (staff != null)
                        {
                            user.StaffName = staff.Staff_Name;
                        }
                    }
                }
                
                return users;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get all users error: {ex.Message}");
                throw;
            }
        }
        
        // Get all users using raw SQL query
        public async Task<List<User>> GetAllUsersRawSqlAsync()
        {
            try
            {
                var connection = await _databaseService.GetConnection();
                
                // Use raw SQL to get all users
                var users = await connection.QueryAsync<User>("SELECT * FROM User");
                
                // Get all staff to populate StaffName property
                var staffList = await connection.Table<Staff>().ToListAsync();
                
                // Join users with staff to get staff names
                foreach (var user in users)
                {
                    if (user.StaffID.HasValue)
                    {
                        var staff = staffList.FirstOrDefault(s => s.StaffID == user.StaffID);
                        if (staff != null)
                        {
                            user.StaffName = staff.Staff_Name;
                        }
                    }
                }
                
                return users;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get all users raw SQL error: {ex.Message}");
                throw;
            }
        }

        // Get user by ID
        public async Task<User> GetUserByIdAsync(int id)
        {
            try
            {
                var connection = await _databaseService.GetConnection();
                
                var user = await connection.Table<User>()
                    .Where(u => u.UserID == id)
                    .FirstOrDefaultAsync();
                    
                if (user != null && user.StaffID.HasValue)
                {
                    // Get associated staff name
                    var staff = await connection.Table<Staff>()
                        .Where(s => s.StaffID == user.StaffID)
                        .FirstOrDefaultAsync();
                        
                    if (staff != null)
                    {
                        user.StaffName = staff.Staff_Name;
                    }
                }
                
                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get user by ID error: {ex.Message}");
                throw;
            }
        }

        // Insert a new user (this wraps the CreateUserAsync method you already have)
        public async Task<int> InsertUserAsync(User user)
        {
            // This assumes the password is already hashed or will be hashed elsewhere
            try
            {
                var connection = await _databaseService.GetConnection();
                await connection.InsertAsync(user);
                return user.UserID;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Insert user error: {ex.Message}");
                throw;
            }
        }

        // Delete a user
        public async Task<int> DeleteUserAsync(User user)
        {
            try
            {
                var connection = await _databaseService.GetConnection();
                return await connection.DeleteAsync(user);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Delete user error: {ex.Message}");
                throw;
            }
        }
        
        // Delete a user by ID
        public async Task<int> DeleteUserAsync(int userId)
        {
            try
            {
                var connection = await _databaseService.GetConnection();
                
                // First get the user to delete
                var user = await connection.Table<User>()
                    .Where(u => u.UserID == userId)
                    .FirstOrDefaultAsync();
                    
                if (user == null)
                {
                    throw new Exception("User not found");
                }
                
                // Delete the user
                return await connection.DeleteAsync(user);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Delete user by ID error: {ex.Message}");
                throw;
            }
        }

        // Wrapper for password verification to match the method name expected in our new code
        public bool VerifyPassword(string providedPassword, string storedHash)
        {
            return PasswordHasher.VerifyPassword(providedPassword, storedHash);
        }

        // Wrapper for password hashing to match the method name expected in our new code
        public string HashPassword(string password)
        {
            return PasswordHasher.HashPassword(password);
        }
    }
}
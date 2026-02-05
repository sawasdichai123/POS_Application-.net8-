using SQLite;
using StockApplication.Services;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace StockApplication.Repositories
{
    // Generic repository interface
    public interface IRepository<T> where T : new()
    {
        Task<List<T>> GetAllAsync();
        Task<T> GetByIdAsync(int id);
        Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<int> InsertAsync(T entity);
        Task<int> UpdateAsync(T entity);
        Task<int> DeleteAsync(T entity);
        Task<int> DeleteByIdAsync(int id);
        Task<int> CountAsync();
    }

    // Generic repository implementation
    public class Repository<T> : IRepository<T> where T : new()
    {
        private readonly DatabaseService _databaseService;
        private SQLiteAsyncConnection _connection;

        public Repository(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        private async Task EnsureConnectionAsync()
        {
            _connection ??= await _databaseService.GetConnection();
        }

        public async Task<List<T>> GetAllAsync()
        {
            await EnsureConnectionAsync();
            return await _connection.Table<T>().ToListAsync();
        }

        public async Task<T> GetByIdAsync(int id)
        {
            await EnsureConnectionAsync();
            return await _connection.FindAsync<T>(id);
        }

        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            await EnsureConnectionAsync();
            return await _connection.Table<T>().Where(predicate).ToListAsync();
        }

        public async Task<int> InsertAsync(T entity)
        {
            await EnsureConnectionAsync();
            return await _connection.InsertAsync(entity);
        }

        public async Task<int> UpdateAsync(T entity)
        {
            await EnsureConnectionAsync();
            return await _connection.UpdateAsync(entity);
        }

        public async Task<int> DeleteAsync(T entity)
        {
            await EnsureConnectionAsync();
            return await _connection.DeleteAsync(entity);
        }

        public async Task<int> DeleteByIdAsync(int id)
        {
            await EnsureConnectionAsync();
            var entity = await GetByIdAsync(id);
            if (entity != null)
                return await _connection.DeleteAsync(entity);
            return 0;
        }

        public async Task<int> CountAsync()
        {
            await EnsureConnectionAsync();
            return await _connection.Table<T>().CountAsync();
        }
    }
}
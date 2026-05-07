using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using Microsoft.EntityFrameworkCore.Storage;

namespace AgendamentoPro.Infrastructure.Database.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AgendamentoProDbContext _context;
        private IDbContextTransaction _transaction;

        public UnitOfWork(AgendamentoProDbContext context)
        {
            _context = context;
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction == null)
                _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            await _context.SaveChangesAsync();
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

        public void Dispose()
        {
            _transaction?.Dispose();
        }
    }
}

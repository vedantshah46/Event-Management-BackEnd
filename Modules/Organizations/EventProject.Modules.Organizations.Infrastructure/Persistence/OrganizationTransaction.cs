using EventProject.Modules.Organizations.Core.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Exceptions;
using Shared.Infrastructure.Persistence;

namespace EventProject.Modules.Organizations.Infrastructure.Persistence;

public sealed class OrganizationTransaction(EventProjectDbContext context) : IOrganizationTransaction
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new BusinessException(BusinessError.Conflict, "The organization changed during this request. Reload and retry.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new BusinessException(BusinessError.Conflict, "An account or membership with these details already exists.");
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}

using CommerceHub.Api.Domain.Orders;

namespace CommerceHub.Api.Repositories;

public interface IOrdersRepository
{
    Task<Order?> GetByIdAsync(string id, CancellationToken ct);
    Task InsertAsync(Order order, CancellationToken ct);
    Task<bool> ReplaceIfNotShippedAsync(string id, Order replacement, CancellationToken ct);
}
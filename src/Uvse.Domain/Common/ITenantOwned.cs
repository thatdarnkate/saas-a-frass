namespace Uvse.Domain.Common;

public interface ITenantOwned
{
    Guid TenantId { get; }
}

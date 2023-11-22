using Sharecode.Backend.Domain.Base;
using Sharecode.Backend.Domain.Base.Interfaces;
using Sharecode.Backend.Domain.Entity.Profile;

namespace Sharecode.Backend.Domain.Repositories;

public interface IUserRepository : IBaseRepository<User>
{
    void Register(User user);
    Task<bool> IsEmailAddressUnique(string emailAddress, CancellationToken token = default);

    Task<User?> GetUserByIdIncludingAccountSettings(Guid userId, bool includeAccountSettings = false ,CancellationToken token = default);
    
    Task<User?> GetUserByEmailIncludingAccountSettings(string emailAddress, bool includeAccountSettings = false, CancellationToken token = default);
    
}
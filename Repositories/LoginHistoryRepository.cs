using ServicePlatform.Data;
using ServicePlatform.Models;
using ServicePlatform.Repositories.Interfaces;

namespace ServicePlatform.Repositories;

public class LoginHistoryRepository : GenericRepository<LoginHistory>, ILoginHistoryRepository
{
    public LoginHistoryRepository(ApplicationDbContext context) : base(context)
    {
    }
}

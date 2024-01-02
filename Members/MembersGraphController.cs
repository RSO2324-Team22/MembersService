using GraphQL.AspNet.Controllers;
using Microsoft.EntityFrameworkCore;
using MembersService.Database;

namespace MembersService.Members;

public class MembersGraphController : GraphController
{
    private readonly ILogger<MembersGraphController> _logger;
    private readonly MembersDbContext _dbContext;

    public MembersGraphController(
            ILogger<MembersGraphController> logger,
            MembersDbContext dbContext) {
        this._logger = logger;
        this._dbContext = dbContext;
    }

    public async Task<IEnumerable<Member>> All() {
        return await this._dbContext.Members.ToListAsync();
    }

    public async Task<Member> Member(int id) {
        return await this._dbContext.Members
            .Where(m => m.Id == id)
            .SingleAsync();
    }
}

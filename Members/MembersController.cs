using MembersService.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MembersService.Members;

[ApiController]
[Route("[controller]")]
public class MembersController : ControllerBase
{
    private readonly ILogger<MembersController> _logger;
    private readonly MembersDbContext _dbContext;

    public MembersController(
            ILogger<MembersController> logger,
            MembersDbContext dbContext) {
        this._logger = logger;
        this._dbContext = dbContext;
    }

    [HttpGet(Name = "GetMembers")]
    [Route("all")]
    [Route("")]
    public async Task<IEnumerable<Member>> GetMembers()
    {
        return await this._dbContext.Members.ToListAsync();
    }

    [HttpGet(Name = "GetSingers")]
    [Route("singers")]
    public async Task<IEnumerable<Member>> GetSingers()
    {
        return await this._dbContext.Members
            .Where(member => member.Roles.Contains(Role.Singer))
            .ToListAsync();
    }
}

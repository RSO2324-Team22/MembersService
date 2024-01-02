using MembersService.Database;
using MembersService.Metrics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MembersService.Members;

[ApiController]
[Route("[controller]")]
public class MembersController : ControllerBase
{
    private readonly ILogger<MembersController> _logger;
    private readonly MembersDbContext _dbContext;
    private readonly MembersMetrics _metrics;
    public MembersController(
            ILogger<MembersController> logger,
            MembersDbContext dbContext,
            MembersMetrics metrics) {
        this._logger = logger;
        this._dbContext = dbContext;
        this._metrics = metrics;
    }

    [HttpGet(Name = "GetMembers")]
    public async Task<IEnumerable<Member>> Index()
    {
        return await this._dbContext.Members.ToListAsync();
    }

    // [HttpGet(Name = "GetRole")]
    // [Route("[action]")]
    // public async Task<IEnumerable<Member>> Singers()
    // {
    //     return await this._dbContext.Members
    //         .Where(member => member.Roles.Contains(Role.Singer))
    //         .ToListAsync();
    // }

    [HttpPost(Name = "AddMember")]
    public async Task<string> Add([FromBody] Member newMember)
    {
        try
        {
            this._dbContext.Members.Add(newMember);
            await this._dbContext.SaveChangesAsync();
            this._metrics.MemberAdded(newMember.Name);
            return "Član uspešno dodan.";
        }
        catch
        {
            return "Napaka pri dodajanju člana.";
        }
    }
}

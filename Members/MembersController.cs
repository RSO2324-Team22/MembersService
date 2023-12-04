using Microsoft.AspNetCore.Mvc;

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

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<Member> Get()
    {
        return new List<Member>();
    }
}

using Confluent.Kafka;
using MembersService.Database;
using MembersService.Metrics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace MembersService.Members;

[ApiController]
[Route("[controller]")]
public class MemberController : ControllerBase
{
    private readonly ILogger<MemberController> _logger;
    private readonly MembersDbContext _dbContext;
    private readonly MembersMetrics _metrics;
    private readonly IProducer<string, int> _kafkaProducer;

    public MemberController(
            ILogger<MemberController> logger,
            MembersDbContext dbContext,
            MembersMetrics metrics,
            IProducer<string, int> kafkaProducer) {
        this._logger = logger;
        this._dbContext = dbContext;
        this._metrics = metrics;
        this._kafkaProducer = kafkaProducer;
    }

    [HttpGet]
    [SwaggerOperation("GetMembers")]
    public async Task<IEnumerable<Member>> Index()
    {
        return await this._dbContext.Members.ToListAsync();
    }

    [HttpGet]
    [Route("singers")]
    [SwaggerOperation("GetSingerMembers")]
    public async Task<ActionResult<IEnumerable<Member>>> Singers()
    {
        try
        {
            var members = await Index();
            return Ok(members.Where(member => member.Roles.Contains(Role.Singer)));
        }
        catch (Exception e)
        {
            var errMsg = "There was a problem fetching singers";
            this._logger.LogError(e, errMsg);
            return BadRequest(errMsg);
        }
    }

    [HttpGet]
    [Route("council")]
    [SwaggerOperation("GetCouncilMembers")]
    public async Task<ActionResult<IEnumerable<Member>>> GetCouncil()
    {
        try
        {
            var members = await Index();
            return Ok(members.Where(member => member.Roles.Contains(Role.Council)));
        }
        catch (Exception e)
        {
            const string errMsg = "There was a problem fetching council members";
            this._logger.LogError(e, errMsg);
            return BadRequest(errMsg);
        }
    }

    [HttpPost]
    [SwaggerOperation("AddMember")]
    public async Task<IResult> Add([FromBody] CreateMemberModel model)
    {
        Member member = new Member() {
            Name = model.Name,
            Section = model.Section,
            PhoneNumber = model.PhoneNumber,
            Email = model.Email,
            Roles = model.Roles
        };

        try
        {
            this._dbContext.Members.Add(member);
            await this._dbContext.SaveChangesAsync();
            Message<string, int> addMemberMessage = new Message<string, int>() {
                Key = "add_member",
                Value = member.Id
            };
            await this._kafkaProducer.ProduceAsync("members", addMemberMessage);
            return Results.Created(nameof(Index), member);
        }
        catch (Exception e)
        {
            const string errMsg = "There was a problem adding new user";
            this._logger.LogError(e, errMsg);
            return Results.BadRequest(errMsg);
        }
    }

    [HttpPut]
    [Route("{id}")]
    [SwaggerOperation("EditMember")]
    public async Task<IResult> Edit(int id, [FromBody] CreateMemberModel model)
    {
        Member? member = await this._dbContext.Members
            .Where(m => m.Id == id)
            .SingleOrDefaultAsync();

        if (member == null)
        {
            this._logger.LogInformation("Member with id: {id} does not exist");
            return Results.BadRequest();
        }

        member.Name = model.Name;
        member.Section = model.Section;
        member.PhoneNumber = model.PhoneNumber;
        member.Email = model.Email;
        member.Roles = model.Roles;

        try {
            await this._dbContext.SaveChangesAsync();
            
            this._logger.LogInformation(221, "Edited user {id}", id);
            
            Message<string, int> editMemberMessage = new Message<string, int>() {
                Key = "edit_member",
                Value = member.Id
            };
            await this._kafkaProducer.ProduceAsync("members", editMemberMessage);
            return Results.Created(nameof(Index), member);
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "There was an error editing user with id: {id}", id);
            return Results.BadRequest($"There was an error editing user with id: {id}");
        }
    }

    [HttpDelete]
    [Route("{id}")]
    [SwaggerOperation("DeleteMember")]
    public async Task<IResult> Delete(int id) {
        Member member = await this._dbContext.Members
            .Where(m => m.Id == id)
            .SingleAsync();

        if (member == null)
        {
            this._logger.LogInformation("Member with given id: {id} does not exist");
            return Results.BadRequest();
        }

        try
        {
            this._dbContext.Remove(member);
            await this._dbContext.SaveChangesAsync();
            Message<string, int> deleteMemberMessage = new Message<string, int>()
            {
                Key = "delete_member",
                Value = member.Id
            };
            await this._kafkaProducer.ProduceAsync("members", deleteMemberMessage);
            return Results.Ok(member);
        }
        catch(Exception e)
        {
            this._logger.LogError(e, "There was an error deleting user {id}", id);
            return Results.BadRequest($"There was an error deleting user {id}");
        }
    }
}

using Confluent.Kafka;
using MembersService.Database;
using MembersService.Kafka;
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
    private readonly IProducer<string, KafkaMessage> _kafkaProducer;
    private readonly HttpContext _httpContext;

    public MemberController(
            ILogger<MemberController> logger,
            IHttpContextAccessor httpContextAccessor,
            MembersDbContext dbContext,
            MembersMetrics metrics,
            IProducer<string, KafkaMessage> kafkaProducer) {
        this._logger = logger;
        this._dbContext = dbContext;
        this._metrics = metrics;
        this._kafkaProducer = kafkaProducer;
        this._httpContext = httpContextAccessor.HttpContext!;
    }

    [HttpGet]
    [SwaggerOperation("GetMembers")]
    public async Task<IEnumerable<Member>> Index()
    {
        this._logger.LogInformation("Getting all members");
        return await this._dbContext.Members.ToListAsync();
    }

    [HttpGet]
    [Route("singers")]
    [SwaggerOperation("GetSingerMembers")]
    public async Task<ActionResult<IEnumerable<Member>>> Singers()
    {
        this._logger.LogInformation("Getting members with Singer role");
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
        this._logger.LogInformation("Getting members with Council role");
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

    [HttpGet]
    [Route("{id}")]
    [SwaggerOperation("GetMemberById")]
    public async Task<ActionResult<Member>> GetMemberById(int id)
    {
        this._logger.LogInformation($"Getting member {id}");
        try
        {   
            Member? member = await this._dbContext.Members
                .Where(m => m.Id == id)
                .SingleOrDefaultAsync();
            
            if (member is null) {
                return NotFound();
            }

            this._logger.LogInformation("Returned member with id: {id}", id);
            return Ok(member);
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "There was a problem fetching council members");
            throw;
        }
    }

    [HttpPost]
    [SwaggerOperation("AddMember")]
    public async Task<ActionResult<Member>> Add([FromBody] CreateMemberModel model)
    {
        this._logger.LogInformation("Adding member");
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
            Message<string, KafkaMessage> addMemberMessage = new Message<string, KafkaMessage>()
            {
                Key = "add_member",
                Value = new KafkaMessage {
                    EntityId = member.Id,
                    CorrelationId = this._httpContext.Request.Headers["X-Correlation-Id"]!
                }
            };
            this._kafkaProducer.Produce("members", addMemberMessage);
            this._logger.LogInformation("Added member {id}", member.Id);
            return CreatedAtAction(nameof(GetMemberById), 
                                   new { id = member.Id }, member);
        }
        catch (Exception e)
        {
            const string errMsg = "Error while adding member";
            this._logger.LogError(e, errMsg);
            return BadRequest(errMsg);
        }
    }

    [HttpPut]
    [Route("{id}")]
    [SwaggerOperation("EditMember")]
    public async Task<ActionResult<Member>> Edit(int id, [FromBody] CreateMemberModel model)
    {
        this._logger.LogInformation("Editing member {id}", id);
        Member? member = await this._dbContext.Members
            .Where(m => m.Id == id)
            .SingleOrDefaultAsync();

        if (member is null)
        {
            this._logger.LogInformation("Member {id} does not exist", id);
            return NotFound();
        }

        member.Name = model.Name;
        member.Section = model.Section;
        member.PhoneNumber = model.PhoneNumber;
        member.Email = model.Email;
        member.Roles = model.Roles;

        try {
            await this._dbContext.SaveChangesAsync();
            
            this._logger.LogInformation(221, "Edited user {id}", id);
            
            Message<string, KafkaMessage> editMemberMessage = new Message<string, KafkaMessage>()
            {
                Key = "edit_member",
                Value = new KafkaMessage {
                    EntityId = member.Id,
                    CorrelationId = this._httpContext.Request.Headers["X-Correlation-Id"]!
                }
            };
            this._kafkaProducer.Produce("members", editMemberMessage);
            this._logger.LogInformation("Updated member {id}", id);
            return Ok(member);
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "There was an error editing user with id: {id}", id);
            throw;
        }
    }

    [HttpDelete]
    [Route("{id}")]
    [SwaggerOperation("DeleteMember")]
    public async Task<ActionResult<Member>> Delete(int id) {
        this._logger.LogInformation("Deleting member {id}", id);
        Member member = await this._dbContext.Members
            .Where(m => m.Id == id)
            .SingleAsync();

        if (member is null)
        {
            this._logger.LogInformation("Member {id} does not exist", id);
            return NotFound();
        }

        try
        {
            this._dbContext.Remove(member);
            await this._dbContext.SaveChangesAsync();
            Message<string, KafkaMessage> deleteMemberMessage = new Message<string, KafkaMessage>()
            {
                Key = "delete_member",
                Value = new KafkaMessage {
                    EntityId = member.Id,
                    CorrelationId = this._httpContext.Request.Headers["X-Correlation-Id"]!
                }
            };
            this._kafkaProducer.Produce("members", deleteMemberMessage);
            this._logger.LogInformation("Deleted member {id}", id);
            return Ok(member);
        }
        catch(Exception e)
        {
            this._logger.LogError(e, "There was an error deleting member {id}", id);
            throw;
        }
    }
}

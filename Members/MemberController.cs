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
    public async Task<IEnumerable<Member>> Singers()
    {
        return await this._dbContext.Members
            .Where(member => member.Roles.Contains(Role.Singer))
            .ToListAsync();
    }

    [HttpGet]
    [Route("council")]
    [SwaggerOperation("GetCouncilMembers")]
    public async Task<IEnumerable<Member>> GetCouncil()
    {
        return await this._dbContext.Members
            .Where(member => member.Roles.Contains(Role.Council))
            .ToListAsync();
    }

    [HttpPost(Name = "AddMember")]
    [SwaggerOperation("AddMember")]
    public async Task<Member> Add([FromBody] CreateMemberModel model)
    {
        Member member = new Member() {
            Name = model.Name,
            Section = Enum.Parse<Section>(model.Section),
            PhoneNumber = model.PhoneNumber,
            Email = model.Email,
            Roles = model.Roles.Select(r => Enum.Parse<Role>(r)).ToList()
        };

        this._dbContext.Members.Add(member);
        await this._dbContext.SaveChangesAsync();
        Message<string, int> addMemberMessage = new Message<string, int>() {
            Key = "add_member",
            Value = member.Id
        };
        await this._kafkaProducer.ProduceAsync("members", addMemberMessage);
        return member;
    }

    [HttpPut]
    [Route("{id}")]
    [SwaggerOperation("EditMember")]
    public async Task<Member> Edit(int id, [FromBody] CreateMemberModel model) {
        Member member = await this._dbContext.Members
            .Where(m => m.Id == id)
            .SingleAsync();
        
        member.Name = model.Name;
        member.Section = Enum.Parse<Section>(model.Section);
        member.PhoneNumber = model.PhoneNumber;
        member.Email = model.Email;
        member.Roles = model.Roles.Select(r => Enum.Parse<Role>(r)).ToList();

        await this._dbContext.SaveChangesAsync();
        Message<string, int> editMemberMessage = new Message<string, int>() {
            Key = "edit_member",
            Value = member.Id
        };
        await this._kafkaProducer.ProduceAsync("members", editMemberMessage);
        return member;
    }

    [HttpDelete]
    [Route("{id}")]
    [SwaggerOperation("DeleteMember")]
    public async Task<Member> Delete(int id) {
        Member member = await this._dbContext.Members
            .Where(m => m.Id == id)
            .SingleAsync();
        
        this._dbContext.Remove(member);
        await this._dbContext.SaveChangesAsync();
        Message<string, int> deleteMemberMessage = new Message<string, int>() {
            Key = "delete_member",
            Value = member.Id
        };
        await this._kafkaProducer.ProduceAsync("members", deleteMemberMessage);
        return member;
    }
}

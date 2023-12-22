using Sharecode.Backend.Domain.Base.Primitive;
using Sharecode.Backend.Domain.Dto.Snippet;

namespace Sharecode.Backend.Application.Features.Users.GetMySnippets;

public class GetMySnippetsResponse : ListResponse<GetMySnippetsQuery, MySnippetsDto>
{
    
}

public class MySnippetsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool Public { get; set; }
    public long View { get; set; }
    public long Copy { get; set; }
    public long Comment { get; set; }
    public List<ReactionCommonDto> Reactions { get; set; } = [];
    public Guid? OwnerId { get; set; }
}
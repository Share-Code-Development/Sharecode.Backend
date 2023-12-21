using Sharecode.Backend.Domain.Dto.Snippet;
using Sharecode.Backend.Domain.Entity.Snippet;

namespace Sharecode.Backend.Application.Features.Snippet.Create;

public class SnippetCreatedResponse(
    Guid snippetId,
    string title,
    string? description,
    string language,
    string previewCode,
    List<string> tags,
    bool @public,
    long views,
    long copy,
    Guid? ownerId,
    List<SnippetReactions> reactions,
    List<SnippetComment> comments)
    : SnippetDto(snippetId, title, description, language, previewCode, tags, @public, views, copy, ownerId, reactions,
        comments)
{

    public new static SnippetCreatedResponse From(Sharecode.Backend.Domain.Entity.Snippet.Snippet snippet)
    {
        return new(
            snippet.Id,
            snippet.Title,
            snippet.Description,
            snippet.Language,
            snippet.PreviewCode,
            snippet.Tags ?? [],
            snippet.Public,
            snippet.Views,
            snippet.Copy,
            snippet.OwnerId,
            snippet.Reactions ?? [],
            snippet.Comments ?? []
        );
    }
    
}
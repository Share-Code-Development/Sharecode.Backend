using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Sharecode.Backend.Application.Client;
using Sharecode.Backend.Application.Features.Snippet;
using Sharecode.Backend.Application.Features.Snippet.Comments.Create;
using Sharecode.Backend.Application.Features.Snippet.Create;
using Sharecode.Backend.Application.Features.Snippet.Get;
using Sharecode.Backend.Utilities.RedisCache;

namespace Sharecode.Backend.Api.Controller;

public class SnippetController(IAppCacheClient cache, IHttpClientContext requestContext, ILogger<AbstractBaseEndpoint> logger, IMediator mediator) : AbstractBaseEndpoint(cache, requestContext, logger, mediator)
{
    /// <summary>
    /// Retrieves a snippet by its ID and query parameters.
    /// </summary>
    /// <param name="id">The ID of the snippet to retrieve.</param>
    /// <param name="snippetQuery">The query parameters for the snippet.</param>
    /// <returns>The HTTP response with the snippet data.</returns>
    [HttpGet("{id}", Name = "Get a snippet")]
    public async Task<ActionResult<GetSnippetResponse>> GetSnippet([FromRoute] Guid id, [FromQuery] GetSnippetQuery snippetQuery)
    {
        snippetQuery.SnippetId = id;
        FrameCacheKey("snippet", id.ToString());
        var cacheValue = await ScanAsync<GetSnippetResponse>();
        if (cacheValue != null)
        {
            return Ok(cacheValue);
        }
        
        var snippetResponse = await mediator.Send(snippetQuery);
        if (snippetResponse == null)
            return NotFound();

        await StoreCacheAsync(snippetResponse);
        //Only need to clear the recent snippet of the user. Not the current request's
        //This key would be added by handler
        await ClearCacheAsync(removeSelf: false);
        return Ok(snippetResponse);
    }

    /// <summary>
    /// Creates a new snippet publicly.
    /// </summary>
    /// <returns>Returns the result of creating a new snippet as an ActionResult</returns>
    [HttpPost("public", Name = "Create a new snippet publicly")]
    public async Task<ActionResult<CreateSnippetCommentResponse>> CreateSnippet()
    {
        return await CreateInternal();
    }

    /// <summary>
    /// Creates a new snippet securely.
    /// </summary>
    /// <returns>Returns the ActionResult containing the CreateSnippetCommentResponse object.</returns>
    /// <remarks>
    /// This method is used to create a new snippet securely. The method is decorated with the [HttpPost] attribute to specify that it handles HTTP POST requests. It is also decorated with
    /// the [Authorize] attribute to ensure that the user must be authenticated before executing this method.
    /// The method internally calls the CreateInternal() method and awaits its result. The result is then returned as an ActionResult containing the CreateSnippetCommentResponse object.
    /// </remarks>
    [HttpPost(Name = "Create a new snippet securely")]
    [Authorize]
    public async Task<ActionResult<CreateSnippetCommentResponse>> CreateSnippetSecure()
    {
        return await CreateInternal();
    }

    /// <summary>
    /// Get the comments of a specific snippet.
    /// </summary>
    /// <param name="snippetId">The unique identifier of the snippet.</param>
    /// <returns>An IActionResult representing the result of the operation.</returns>
    [HttpGet("{snippetId}/comments", Name = "Get the comments of snippets")]
    public async Task<IActionResult> GetSnippetComments(Guid snippetId)
    {
        FrameCacheKey("snippet-comment", snippetId.ToString());
        return Ok();
    }

    /// <summary>
    /// Create a comment for a snippet.
    /// </summary>
    /// <param name="snippetId">The ID of the snippet to create the comment for.</param>
    /// <param name="command">The command object containing the details of the comment to be created.</param>
    /// <returns>The created snippet comment as an ActionResult of type CreateSnippetCommentResponse.</returns>
    [HttpPost("{snippetId}/comments", Name = "Create a comment for snippet")]
    [Authorize]
    public async Task<ActionResult<CreateSnippetCommentResponse>> CreateSnippetComments([FromRoute]Guid snippetId, [FromBody] CreateSnippetCommentCommand command)
    {
        command.SnippetId = snippetId;
        var response = await mediator.Send(command);
        return CreatedAtAction("GetSnippetComments", new { snippetId = response.Id }, response);
    }

    /// <summary>
    /// Creates a new snippet comment internally.
    /// </summary>
    /// <returns>An <see cref="ActionResult{T}"/> object containing the result of the operation.</returns>
    private async Task<ActionResult<CreateSnippetCommentResponse>> CreateInternal()
    {
        var formCollection = await Request.ReadFormAsync();
        var file = formCollection.Files.FirstOrDefault();
        if (file == null)
        {
            return BadRequest("Missing file object");
        }
        var bodyRaw = formCollection["body"];

        if (string.IsNullOrEmpty(bodyRaw))
            return BadRequest("Invalid body object");

        var command = JsonConvert.DeserializeObject<CreateSnippetCommand>(bodyRaw.ToString());
        if (command == null)
            return BadRequest("Failed to parse the request");
        
        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        command.Content = ms.ToArray();

        if (string.IsNullOrEmpty(command.PreviewCode))
        {
            string preview;
            preview = command.Content.Length > 1200 ? Encoding.Default.GetString(command.Content, 0, 1200) : Encoding.Default.GetString(command.Content);
            command.PreviewCode = preview;
        }
        
        var response = await mediator.Send(command);
        return CreatedAtAction("GetSnippet", new {id = response.SnippetId} , response);
    }
}
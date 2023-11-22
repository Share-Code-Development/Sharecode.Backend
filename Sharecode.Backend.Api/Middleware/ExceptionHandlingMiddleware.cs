using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Sharecode.Backend.Application.Exceptions;
using Sharecode.Backend.Domain.Exceptions;
using Sharecode.Backend.Infrastructure.Exceptions.Jwt;

namespace Sharecode.Backend.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    
    private static readonly Regex CamelCaseRegex = new Regex("(\\B[A-Z])", RegexOptions.Compiled);
    private static readonly ConcurrentDictionary<string, string> ExceptionNameCache = new ConcurrentDictionary<string, string>();

    
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AggregateException aggEx)
        {
            var innerException = aggEx.Flatten().InnerException;
            _logger.LogError(innerException, "An aggregate exception has been caught on the request pipeline.");
            await HandleExceptionAsync(context, innerException);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception has been caught on the request pipeline.");
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception? exception)
    {
        ExceptionDetail exceptionDetail = BuildExceptionMessage(exception, _env.IsDevelopment());
        context.Response.StatusCode = exceptionDetail.StatusCode;
        context.Response.ContentType = "application/json";
        if (exceptionDetail.StatusCode < 500 || exceptionDetail.StatusCode >= 400)
        {
            context.Response.Headers["SCE-Code"] = exceptionDetail.ErrorCode.ToString();
            context.Response.Headers["SCE-Message"] = exceptionDetail.Message;
        }
        await context.Response.WriteAsJsonAsync(exceptionDetail);
    }
    
    
    private static ExceptionDetail BuildExceptionMessage(Exception? exception, bool showExtended = false)
    {
        return exception switch
        {
            ValidationException validationException => new ExceptionDetail(
                StatusCodes.Status400BadRequest,
                "Validation Error",
                "Failed to validate properties provided",
                validationException.Errors,
                ErrorCode: 400
            ),
            JwtGenerationException jwtGenerationException => new ExceptionDetail(
                StatusCodes.Status500InternalServerError,
                "Access Generation Failure",
                $"Failed to generate access tokens.",
                null,
                ExtendedMessage: showExtended ? jwtGenerationException.Message : ""
                ),
            JwtFetchKeySecretException jwtFetchKeySecretException => new ExceptionDetail(
                StatusCodes.Status500InternalServerError,
                "Access Generation Failure",
                $"Failed to fetch keys. {jwtFetchKeySecretException.Message}",
                null,
                ExtendedMessage: showExtended ? jwtFetchKeySecretException.Message : ""
                ),
            AppException appException => CreateExceptionDetail(appException),
            _ => new ExceptionDetail(
                StatusCodes.Status500InternalServerError,
                "Unknown Error",
                "An unknown error occured. Please retry again",
                null
            )
        };
    }
    
    private static ExceptionDetail CreateExceptionDetail(AppException appException)
    {
        string typeName = appException.GetType().Name;
        string readableName = ExceptionNameCache.GetOrAdd(typeName, key =>
            CamelCaseRegex.Replace(key, " $1").Replace("Exception", string.Empty)
        );
        

        return new ExceptionDetail(
            (int)appException.StatusCode,
            readableName.TrimEnd(),
            appException.PublicMessage == String.Empty ? appException.Message : appException.PublicMessage,
            appException.Errors, 
            appException.InnerException?.Message,
            ErrorCode: appException.ErrorCode
        );
    }
}


internal sealed record ExceptionDetail(
    [property: Newtonsoft.Json.JsonIgnore] [field: Newtonsoft.Json.JsonIgnore] [property: JsonIgnore] [field: JsonIgnore] int StatusCode,
    string Type, string Message, 
    IEnumerable<object>? Errors, 
    string? ExtendedMessage = null,
    long? ErrorCode = null
    );
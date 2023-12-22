using Sharecode.Backend.Application.Models;
using Sharecode.Backend.Domain.Base.Primitive;
using Sharecode.Backend.Domain.Dto;
using Sharecode.Backend.Domain.Entity.Profile;
using Sharecode.Backend.Domain.Enums;

namespace Sharecode.Backend.Application.Features.Users.Login;

public class LoginUserResponse : UserDto
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    public LoginUserResponse() { }

    public LoginUserResponse(Guid userId, string firstName, string? middleName, string lastName, string emailAddress, bool emailVerified, Dictionary<string, string> metadata, AccountVisibility visibility, AccountSettingDto? settings, DateTime created, string? profilePicture ,string? accessToken, string? refreshToken)
        : base(userId, firstName, middleName, lastName, emailAddress, emailVerified, metadata, visibility, settings, created, profilePicture)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }

    public static LoginUserResponse From(User user, AccessCredentials? credentials)
    {
        return new LoginUserResponse(
            user.Id,
            user.FirstName,
            user.MiddleName,
            user.LastName,
            user.EmailAddress,
            user.EmailVerified,
            user.Metadata,
            user.Visibility,
            AccountSettingDto.From(user),
            user.CreatedAt,
            user.ProfilePicture,
            credentials?.AccessToken,
            credentials?.RefreshToken
        );
    }
}
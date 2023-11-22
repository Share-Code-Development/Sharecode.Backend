namespace Sharecode.Backend.Domain.Entity.Profile;

public record Permission(string Key, string Description, bool IsAdminOnly = false);

public class Permissions
{
    public static Permission ViewDocument => new("view-document", "View all the documents");

    public static Permission AccessProfileOthers =>
        new Permission("read-profile-others", "Fetch/View others profile information");
}
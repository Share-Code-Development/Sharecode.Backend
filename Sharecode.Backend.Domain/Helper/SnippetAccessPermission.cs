﻿using Sharecode.Backend.Domain.Dto.Snippet;
using Sharecode.Backend.Domain.Entity.Snippet;
using Sharecode.Backend.Domain.Enums;

namespace Sharecode.Backend.Domain.Helper;

public class SnippetAccessPermission(Guid snippetId, Guid accessorId, bool read, bool write, bool manage, bool snippetPublic)
{

    public static SnippetAccessPermission NoPermission(Guid snippetId, Guid accessorId) =>
        new SnippetAccessPermission(snippetId, accessorId, false, false, false, true);
    
    public static SnippetAccessPermission Error =>
        new SnippetAccessPermission(Guid.Empty, Guid.Empty, false, false, false, true);
    
    public Guid SnippetId { get; } = snippetId;
    public Guid AccessorId { get; } = accessorId;
    private bool Read { get;  } = read || write || manage;
    private bool Write { get; } = write || manage;
    private bool Manage { get; } = manage;

    public bool IsPublicSnippet => snippetPublic;
    public bool IsPrivateSnippet => !snippetPublic;

    public bool Any()
    {
        return Any(SnippetAccess.Read, SnippetAccess.Write, SnippetAccess.Manage);
    }
    
    public bool Any(params SnippetAccess[] accesses)
    {
        foreach (var access in accesses)
        {
            switch (access)
            {
                case SnippetAccess.Read when Read:
                case SnippetAccess.Write when Write:
                case SnippetAccess.Manage when Manage:
                    return true;
            }
        }

        return false;
    }

    public bool All()
    {
        return All(SnippetAccess.Read, SnippetAccess.Write, SnippetAccess.Manage);
    }

    public bool All(params SnippetAccess[] accesses)
    {
        foreach (var access in accesses)
        {
            switch (access)
            {
                case SnippetAccess.Read when !Read:
                case SnippetAccess.Write when !Write:
                case SnippetAccess.Manage when !Manage:
                    return false;
            }
        }

        return true;
    }

    public SnippetAccessControlDto ToControlModel()
    {
        return new SnippetAccessControlDto()
        {
            UserId = AccessorId,
            Manage = manage,
            Read = read,
            Write = write
        };
    }
}

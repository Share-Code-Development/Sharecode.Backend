﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sharecode.Backend.Domain.Base;
using Sharecode.Backend.Domain.Base.Primitive;
using Sharecode.Backend.Domain.Enums;
using Sharecode.Backend.Domain.Events.Users;

namespace Sharecode.Backend.Domain.Entity.Profile;

public class User : AggregateRootWithMetadata
{
    public User()
    {
    }
    
    [EmailAddress]
    [Length(minimumLength:5, maximumLength: 100)]
    [Required]
    public required string EmailAddress { get; init; }
    [Required]
    [Length(minimumLength: 3, maximumLength: 100)]
    public required string FirstName { get; set; }
    [Length(minimumLength: 3, maximumLength: 100)]
    public string? MiddleName { get; set; }
    [Required]
    [Length(minimumLength: 3, maximumLength: 100)]
    public required string LastName { get; set; }
    [NotMapped]
    public string FullName
    {
        get
        {
            if (string.IsNullOrEmpty(MiddleName))
                return $"{FirstName} {LastName}";
            
            return $"{FirstName} {MiddleName} {LastName}";
        }
    }

    [Length(minimumLength: 9, maximumLength: 300)]
    public string NormalizedFullName
    {
        get => FullName.ToUpper();
        private set
        {
            
        }
    }

    public DateTime LastLogin { get; private set; } = DateTime.UtcNow;
    public byte[]? Salt { get; set; }
    public byte[]? PasswordHash { get; set; }
    [Required]
    public bool EmailVerified { get; private set; }
    [Url]
    public string? ProfilePicture { get; set; }
    public AccountSetting AccountSetting { get; set; }
    [Required]
    public required AccountVisibility Visibility { get; set; }
    public bool Active { get; private set; }
    public InactiveReason? InActiveReason { get; private set; }
    
    public override void RaiseCreatedEvent()
    {
        if(EmailVerified)
            return;
        
        UserCreatedDomainEvent @event = UserCreatedDomainEvent.Create(this);
        RaiseDomainEvent(@event);
    }

    public bool VerifyUser()
    {
        if(EmailVerified)
            return false;

        EmailVerified = true;
        RaiseDomainEvent(UserVerifiedDomainEvent.Create(this));
        return true;
    }

    public bool SetInActive(string reason)
    {
        if(!Active)
            return false;

        InActiveReason = reason;
        Active = false;
        RaiseDomainEvent(AccountSetInActiveDomainEvent.Create(this));
        return true;
    }

    public bool SetActive()
    {
        if (Active)
            return false;

        InActiveReason = null;
        Active = true;
        return true;
    }

    public void SetLastLogin()
    {
        LastLogin = DateTime.UtcNow;
    }

    public void ResendEmailVerification()
    {
        if(EmailVerified)
            return;
        
        RaiseDomainEvent(UserCreatedDomainEvent.Create(this));
    }

    public bool RequestPasswordReset(bool verifyAccountStatus = true)
    {
        if (verifyAccountStatus && !Active && InActiveReason! == InactiveReasons.InvalidPassword)
        {
            return false;
        }

        RaiseDomainEvent(RequestPasswordResetDomainEvent.Create(this));
        return true;
    }
}

#region Inactive Reasons

public class InactiveReason
{
    public string Value { get; private set; }

    private InactiveReason() { } // EF Core uses this

    public InactiveReason(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static implicit operator string(InactiveReason reason) => reason.Value;
    public static implicit operator InactiveReason(string reason) => new InactiveReason(reason);
}

public static class InactiveReasons
{
    public static InactiveReason InvalidPassword => "Wrong password limit reached";

    public static InactiveReason ContactSupport => "Your account has been temporarily suspended, Please contact support!";
}


#endregion
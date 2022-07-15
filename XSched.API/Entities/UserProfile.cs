using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace XSched.API.Entities;

#nullable disable

public class UserProfile
{
    public Guid Id { get; set; }

    [Required]
    public string Title { get; set; }

    [IgnoreDataMember]
    public string UserId { get; set; }

    public ApplicationUser User { get; set; }

    public bool IsDefault { get; set; }
}
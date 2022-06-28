using System.ComponentModel.DataAnnotations;

namespace XSched.API.Entities;

public class UserProfile
{
    public Guid Id { get; set; }

    [Required]
    public string Title { get; set; }

    public string UserId { get; set; }

    public ApplicationUser User { get; set; }
}
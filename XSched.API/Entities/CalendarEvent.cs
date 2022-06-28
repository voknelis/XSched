using System.ComponentModel.DataAnnotations;

namespace XSched.API.Entities;

public class CalendarEvent
{
    public Guid Id { get; set; }

    [Required]
    public string Title { get; set; }

    public string? Description { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public bool? AllDay { get; set; }

    public string? RecurrenceRule { get; set; }

    public string? RecurrenceException { get; set; }

    public Guid ProfileId { get; set; }

    public UserProfile Profile { get; set; }
}
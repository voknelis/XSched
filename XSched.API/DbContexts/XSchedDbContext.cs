using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using XSched.API.Entities;

namespace XSched.API.DbContexts;

public class XSchedDbContext : IdentityDbContext<IdentityUser>
{
    public virtual DbSet<RefreshSession> RefreshSessions { get; set; }

    public virtual DbSet<UserProfile> Profiles { get; set; }

    public virtual DbSet<CalendarEvent> CalendarEvents { get; set; }

    public XSchedDbContext(DbContextOptions<XSchedDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}
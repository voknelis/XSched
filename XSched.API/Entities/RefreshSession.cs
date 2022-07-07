namespace XSched.API.Entities;

public class RefreshSession
{
    public int Id { get; set; }

    public string UserId { get; set; }

    public ApplicationUser User { get; set; }

    public string RefreshToken { get; set; }

    public DateTime ExpiresIn { get; set; }

    public DateTime Created { get; set; }

    public string Fingerprint { get; set; }

    public string UserAgent { get; set; }

    public string Ip { get; set; }
}
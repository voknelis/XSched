namespace XSched.API.Dtos;

public class RefreshTokenResult
{
    public string Token { get; set; }
    public DateTime ExpiresIn { get; set; }
}
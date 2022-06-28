namespace XSched.API.Dtos;

public class TokenResponse
{
    public string AccessToken { get; set; }

    public string RefrestToken { get; set; }

    public DateTime Expiration { get; set; }
}
namespace XSched.API.Dtos;

public class ClientConnectionMetadata
{
    public string Fingerprint { get; set; }
    public string UserAgent { get; set; }
    public string Ip { get; set; }
}
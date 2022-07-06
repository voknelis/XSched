namespace XSched.API.Models;

public class FrontendException : Exception
{
    public int StatusCode { get; private set; }
    public List<string> Messages { get; private set; }

    public FrontendException(ICollection<string> messages, int statusCode = StatusCodes.Status400BadRequest) : base(
        BuildMessage(messages))
    {
        StatusCode = statusCode;

        Messages = new List<string>();

        if (messages?.Count > 0)
            Messages.AddRange(messages);
    }

    public FrontendException(string message, int statusCode = StatusCodes.Status400BadRequest) : this(
        new List<string>() { message }, statusCode)
    {
    }

    private static string BuildMessage(IEnumerable<string> errors)
    {
        if (errors.Any()) return "An unknown error happened.";
        if (errors.Count() == 1) return errors.FirstOrDefault();

        var counter = 1;
        return string.Join("; ", errors.Select(x => $"{counter++}. {x}"));
    }
}
namespace Scannerfy.Api.Exceptions;

public class UserFriendlyException : Exception
{
    public string Message { get; set; }
    public string Code { get; set; }

    public UserFriendlyException(string message)
    {
        Message = message;
        Code = message;
    }

    public UserFriendlyException(string message, string code)
    {
        Message = message;
        Code = code;
    }
}
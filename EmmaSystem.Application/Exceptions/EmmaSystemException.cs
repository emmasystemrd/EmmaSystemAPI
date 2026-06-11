namespace EmmaSystem.Application.Exceptions;

public sealed class EmmaSystemException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }

    public EmmaSystemException(string message, int statusCode = 500, string errorCode = "EMMA_ERROR") : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}
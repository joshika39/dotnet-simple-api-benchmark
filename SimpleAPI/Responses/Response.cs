namespace SimpleAPI.Responses;

public class Response(string message, int code) : IResponse
{
    public string Message { get; } = message;
    public int Code { get; } = code;
}
namespace SimpleAPI.Responses;

public interface IResponse
{
    string Message { get; }
    int Code { get; }
}
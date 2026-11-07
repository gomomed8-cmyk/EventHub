

namespace EventHub.Application.Common.Errors
{
    public sealed record Error(
        string Code, string Description,
        ErrorType ErrorType=ErrorType.Failure);
    
}



namespace EventHub.Application.Common.Errors
{
    public sealed class UserErrors
    {
        public static readonly Error Unauthorized =
      new(
          "User.Unauthorized",
          "User is not authenticated.",
          ErrorType.Unauthorized);

        public static readonly Error NotFound =
            new(
                "User.NotFound",
                "User Id not found.",
                ErrorType.NotFound);
    }
}

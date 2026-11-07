using EventHub.Application.Common.Errors;


namespace EventHub.Application.Common.Results
{
    public class Result
    {
        protected Result(bool isSuccess ,IReadOnlyList<Error> errors)
        {
            IsSuccess = isSuccess ;
            Errors = errors;
        }
        public bool IsSuccess { get; }
        public bool IsFailure=> !IsSuccess;
        public IReadOnlyList<Error> Errors { get; }

        public static Result Ok()
      => new(true, Array.Empty<Error>());

        public static Result Fail(Error error)
            => new(false, new[] { error });

        public static Result Fail(IReadOnlyList<Error> errors)
            => new(false, errors);

    }
}

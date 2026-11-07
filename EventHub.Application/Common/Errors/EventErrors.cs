

namespace EventHub.Application.Common.Errors
{
    public sealed class EventErrors
    {
        public static readonly Error InvalidStartDate =
          new(
              "Event.InvalidStartDate",
              "Event start date cannot be in the past.",
              ErrorType.Validation);

        public static readonly Error InvalidDateRange =
           new(
               "Event.InvalidDateRange",
               "Event end date cannot be before start date.",
               ErrorType.Validation);
        public static readonly Error NoTicketCategories =
           new(
               "Event.NoTicketCategories",
               "Event must have at least one ticket category.",
             ErrorType.Validation);
        public static readonly Error DuplicateEvent = new(
                 "Event.Duplicate",
                 "An identical event already exists.",
                  ErrorType.Conflict);
    }
}

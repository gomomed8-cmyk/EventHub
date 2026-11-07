using EventHub.Application.Common.Errors;
using EventHub.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;
namespace EventHub.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiBaseController : ControllerBase
    {
        public static ActionResult<T> ToActionResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                return new OkObjectResult(result.Value);
            }
            else
            {
                return ToProblem(result.Errors);
            }
        }
            public static ActionResult ToActionResult(Result result)
        {

            if (result.IsSuccess)
            {
                return new OkResult();
            }
            else
            {
                return ToProblem(result.Errors);
            }

        }
        protected static ObjectResult ToProblem(IReadOnlyList<Error> errors)
        {
            if (errors is null || errors.Count == 0)
            {
                return new ObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "InternalServerError",
                    Detail = "An unexpected error occurred."
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }

            var firstError = errors[0];

            var statusCode = firstError.ErrorType switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError
            };

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = firstError.Code,
                Detail = firstError.Description
            };

            problem.Extensions["errors"] = errors;

            return new ObjectResult(problem)
            {
                StatusCode = statusCode
            };
        }
    }
      
    
}

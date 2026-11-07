using EventHub.Application.Common.Errors;
using EventHub.Application.Common.Results;
using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Auth;
using EventHub.Infrastructure.Identity.ContextIdentity;
using EventHub.Infrastructure.Identity.EntityIdentity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;


namespace EventHub.Infrastructure.Identity
{
    internal class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EventHubIdentityDbContext _eventHubIdentity;
        private readonly ILogger<IdentityService> _logger;

        public IdentityService(UserManager<ApplicationUser> userManager,EventHubIdentityDbContext eventHubIdentity,ILogger<IdentityService> logger)
        {
            _userManager = userManager;
            _eventHubIdentity = eventHubIdentity;
            _logger = logger;
        }
        public async Task<Result<Guid>> CreateUserAsync(RegisterRequest request, CancellationToken ct = default)
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                DisplayName = request.DisplayName,
                PhoneNumber = request.Phone,


            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (result.Succeeded)
            {
                var roleExist=await _userManager.AddToRoleAsync(user,"User");

                if (!roleExist.Succeeded)
                {
                  await _userManager.DeleteAsync(user);

                    var roleError=roleExist.Errors.Select(e=> new Error(e.Code,e.Description)).ToList();

                    return Result<Guid>.Fail(roleError);
                }
                return Result<Guid>.Ok(user.Id);
            }
           
            else
            {
                var errors = result.Errors.Select(e => new Error(e.Code, e.Description)).ToList();

                return Result<Guid>.Fail(errors);
            }

        }

        public async Task<Result<LoginUserInfo>> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                return Result<LoginUserInfo>.Fail(new Error("User Not Found", "User Can Not Found"));
            }
            var roleExist = await _userManager.GetRolesAsync(user);
            var role = roleExist.FirstOrDefault();
            if (role is null)
            {
                return Result<LoginUserInfo>.Fail(
                    new Error("Role.NotFound", "User does not have a role."));
            }
            return Result<LoginUserInfo>.Ok(new LoginUserInfo
            {
                UserId = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email,
                Role = role
            });
        }

        public async Task<Result<UserEmailInfo>> GetUserEmailInfoAsync(Guid userId,CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null)
            {
                return Result<UserEmailInfo>.Fail(
                    new Error("User.NotFound", "User was not found.",ErrorType.NotFound));
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return Result<UserEmailInfo>.Fail(
                    new Error("User.EmailNotFound", "User does not have an email address.",ErrorType.Conflict));
            }

            return Result<UserEmailInfo>.Ok(
                new UserEmailInfo(
                    user.Id,
                    user.Email,
                    user.DisplayName));
        }

        public async Task<Result<LoginUserInfo>> LoginAsync(string email, string password, CancellationToken ct = default)
        {
         var user =await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return Result<LoginUserInfo>.Fail(new Error("Invalid email or password", "InvalidCredentials"));

            }
            
            var checkPassword = await _userManager.CheckPasswordAsync(user, password);
            if (!checkPassword)
            {
                return Result<LoginUserInfo>.Fail(new Error("Invalid email or password", "InvalidCredentials"));
            }
            var getRole = await _userManager.GetRolesAsync(user);
            var role = getRole.FirstOrDefault();
            if (role is null)
            {
                return Result<LoginUserInfo>.Fail(
                    new Error("Role.NotFound", "User does not have a role."));
            }
            return Result<LoginUserInfo>.Ok(new LoginUserInfo
                {
                    UserId = user.Id,
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    Role = role
                });
  
        }

        public async Task<Result> PromoteToOrganizerAsync(Guid userId, CancellationToken ct = default)
        {

               var user =  await  _userManager.FindByIdAsync(userId.ToString());

                if (user == null)
                {

                    return Result.Fail(new Error("User.NotFound", "User was not found."));

                }
                var roles = await _userManager.GetRolesAsync(user);

                var currentRole = roles.FirstOrDefault();
                if (currentRole == "Organizer")
                {

                    return Result.Fail(
                       new Error(
                    "Role.AlreadyOrganizer",
                    "User is already an organizer."));
                }
                if (currentRole != "User")
                {

                    return Result.Fail(
                     new Error(
                    "Role.Invalid",
                    "Only users can be promoted to organizers."));
                }


            await using var transaction = await _eventHubIdentity.Database.BeginTransactionAsync(ct);

            try { 

                var remove = await _userManager.RemoveFromRoleAsync(user, "User");

                if (!remove.Succeeded)
                {
                    var errors = remove.Errors.Select(e => new Error(e.Code, e.Description)).ToList();

                   await transaction.RollbackAsync();

                    return Result.Fail(errors);
                }

                var addRole = await _userManager.AddToRoleAsync(user, "Organizer");
                if (!addRole.Succeeded)
                {
                    var roleError = addRole.Errors.Select(e => new Error(e.Code, e.Description)).ToList();

                    await transaction.RollbackAsync();

                    return Result.Fail(roleError);
                }
                await transaction.CommitAsync(ct);


                return Result.Ok();
            }

            catch(Exception ex)
            {
                    await transaction.RollbackAsync();

                    _logger.LogError(ex,
                    "Failed to promote user with ID {UserId} to organizer."
                    , userId);

                  return Result.Fail(
                      new Error(
                     "Promotion.Failed",
                     "Failed to promote user to organizer."));
            }
        }
    }
}


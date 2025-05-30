using DevStore.Core.Http;
using DevStore.Core.Messages.Integration;
using DevStore.Identity.API.Models;
using DevStore.MessageBus;
using DevStore.WebAPI.Core.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NetDevPack.Identity.Interfaces;
using NetDevPack.Identity.Jwt.Model;
using NetDevPack.Security.Jwt.Core.Interfaces;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DevStore.Identity.API.Controllers
{
    [Route("api/identity")]
    public class AuthController : MainController
    {
        private readonly IJwtBuilder _jwtBuilder;
        private readonly IMessageBus _bus;
        public readonly SignInManager<IdentityUser> SignInManager;
        public readonly UserManager<IdentityUser> UserManager;
        private readonly IRestClient _restClient;

        public AuthController(
            IJwtBuilder jwtBuilder,
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IMessageBus bus,
            IRestClient restClient)
        {
            _jwtBuilder = jwtBuilder;
            SignInManager = signInManager;
            UserManager = userManager;
            _bus = bus;
            _restClient = restClient;
        }

        [HttpPost("new-account")]
        public async Task<ActionResult> Register(NewUser newUser)
        {

            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var user = new IdentityUser
            {
                UserName = newUser.Email,
                Email = newUser.Email,
                EmailConfirmed = true
            };

            var result = await UserManager.CreateAsync(user, newUser.Password);

            if (result.Succeeded)
            {
                var (jwt, customerResult) = await RegisterUser(newUser);

                if (!customerResult.ValidationResult.IsValid)
                {
                    await UserManager.DeleteAsync(user);
                    return CustomResponse(customerResult.ValidationResult);
                }

                return CustomResponse(jwt);
            }

            foreach (var error in result.Errors)
            {
                AddErrorToStack(error.Description);
            }

            return CustomResponse();
        }

        [HttpPost("auth")]
        public async Task<ActionResult> Login(UserLogin userLogin)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var result = await SignInManager.PasswordSignInAsync(userLogin.Email, userLogin.Password,
                false, true);

            if (result.Succeeded)
            {

                var jwt = await _jwtBuilder
                                            .WithEmail(userLogin.Email)
                                            .WithJwtClaims()
                                            .WithUserClaims()
                                            .WithUserRoles()
                                            .WithRefreshToken()
                                            .BuildUserResponse();
                return CustomResponse(jwt);
            }

            if (result.IsLockedOut)
            {
                AddErrorToStack("User temporary blocked. Too many tries.");
                return CustomResponse();
            }

            AddErrorToStack("User or Password incorrect");
            return CustomResponse();
        }

        private async Task<(UserResponse, ResponseMessage)> RegisterUser(NewUser newUser)
        {
            var user = await UserManager.FindByEmailAsync(newUser.Email);

            var userRegistered = new UserRegisteredIntegrationEvent(Guid.Parse(user.Id), newUser.Name, newUser.Email, newUser.SocialNumber);

            try
            {
                var jwt = await _jwtBuilder
                                            .WithEmail(newUser.Email)
                                            .WithJwtClaims()
                                            .WithUserClaims()
                                            .WithUserRoles()
                                            .WithRefreshToken()
                                            .BuildUserResponse();
                var response = await _restClient
                    .PostAsync<UserRegisteredIntegrationEvent, ResponseMessage>(userRegistered, jwt.AccessToken);
                return (jwt, response);
                //return await _bus.RequestAsync<UserRegisteredIntegrationEvent, ResponseMessage>(userRegistered);
            }
            catch (Exception)
            {
                await UserManager.DeleteAsync(user);
                throw;
            }
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult> RefreshToken([FromBody] string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                AddErrorToStack("Invalid Refresh Token");
                return CustomResponse();
            }

            var token = await _jwtBuilder.ValidateRefreshToken(refreshToken);

            if (!token.IsValid)
            {
                AddErrorToStack("Expired Refresh Token");
                return CustomResponse();
            }


            var jwt = await _jwtBuilder
                                        .WithUserId(token.UserId)
                                        .WithJwtClaims()
                                        .WithUserClaims()
                                        .WithUserRoles()
                                        .WithRefreshToken()
                                        .BuildUserResponse();

            return CustomResponse(jwt);
        }

#if DEBUG
        [HttpPost("validate-jwt")]
        public async Task<ActionResult> ValidateJwt([FromServices] IJwtService jwtService, [FromForm] string jwt)
        {
            var handler = new JsonWebTokenHandler();

            var result = await handler.ValidateTokenAsync(jwt, new TokenValidationParameters()
            {
                ValidIssuer = "https://devstore.academy",
                ValidAudience = "DevStore",
                ValidateAudience = true,
                ValidateIssuer = true,
                RequireSignedTokens = false,
                IssuerSigningKey = await jwtService.GetCurrentSecurityKey(),
            });

            if (!result.IsValid)
                return BadRequest();

            return Ok(result.Claims.Select(s => new { s.Key, s.Value }));
        }
        
#endif
    }
}
using Ecommerce.API.DTOs.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Ecommerce.API.Areas.Identity
{
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area(SD.IDENTITY_AREA)]
    public class AuthsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IAccountService _accountService;
        private readonly IRepository<ApplicationUserOTP> _applicationUserOTPRepository;
        private readonly IConfiguration _configuration;

        public AuthsController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailSender emailSender, IAccountService accountService, IRepository<ApplicationUserOTP> applicationUserOTPRepository, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _accountService = accountService;
            _applicationUserOTPRepository = applicationUserOTPRepository;
            _configuration = configuration;
        }

        // Register
        [HttpPost("Register")]
        //[Route("Register")]
        public async Task<IActionResult> Register(RegisterRequest registerRequest)
        {
            ApplicationUser user = new()
            {
                FirstName = registerRequest.FName,
                LastName = registerRequest.LName,
                Email = registerRequest.Email,
                UserName = registerRequest.UserName,
                Address = registerRequest.Address,
            };

            var result = await _userManager.CreateAsync(user, registerRequest.Password);
            // Password must contain (lower case, upper case, digits, special char, more than 6 char)

            if (!result.Succeeded)
            {
                //foreach (var item in result.Errors)
                //    ModelState.AddModelError(string.Empty, item.Description);

                //return View(registerRequest);

                return BadRequest(result.Errors);
            }

            // Send Email Confirmation
            await _accountService.SendMailAsync(user, Url, Request);

            await _userManager.AddToRoleAsync(user, RoleNames.CUSTOMER);

            //TempData["success_notification"] = "Add Account Successfully, check you email";
            //return RedirectToAction("Login");

            var url = Url.Action("Login", SD.AUTH, new { area = SD.IDENTITY_AREA });
            return Created(url, new Response()
            {
                Message = "Add Account Successfully, check you email",
                Status = 201
            });
        }

        // Login (Tokens - JWT & Refresh Token)
        [HttpPost("Login")]
        //[Route("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginReuqest)
        {
            ModelStateDictionary keyValuePairs = new();

            var user = await _userManager.FindByEmailAsync(loginReuqest.EmailORUserName) ?? await _userManager.FindByNameAsync(loginReuqest.EmailORUserName);

            if (user is null)
            {
                //ModelState.AddModelError(nameof(LoginVM.EmailORUserName), "Invalid User Name Or Email");
                //ModelState.AddModelError(nameof(LoginVM.Password), "Invalid Password");
                //return View(loginReuqest);

                //return NotFound("Invalid User Name Or Email, Invalid Password");

                keyValuePairs.AddModelError(loginReuqest.EmailORUserName, "Invalid User Name Or Email");
                keyValuePairs.AddModelError(loginReuqest.Password, "Invalid Password");
                return BadRequest(keyValuePairs);
            }

            #region Old way
            //var result = await _userManager.CheckPasswordAsync(user, loginReuqest.Password);

            //if(!result)
            //{
            //    ModelState.AddModelError(nameof(LoginVM.EmailORUserName), "Invalid User Name Or Email");
            //    ModelState.AddModelError(nameof(LoginVM.Password), "Invalid Password");

            //    return View(loginReuqest);
            //}

            //if(!user.EmailConfirmed)
            //{
            //    ModelState.AddModelError(nameof(LoginVM.EmailORUserName), "Confirm Your Email First");

            //    return View(loginReuqest);
            //} 
            #endregion

            var result = await _signInManager.PasswordSignInAsync(user, loginReuqest.Password, loginReuqest.RememberMe, true);

            if (!result.Succeeded)
            {
                keyValuePairs.AddModelError(loginReuqest.EmailORUserName, "Invalid User Name Or Email");
                keyValuePairs.AddModelError(loginReuqest.Password, "Invalid Password");
                return BadRequest(keyValuePairs);
            }

            if (result.IsNotAllowed)
            {
                keyValuePairs.AddModelError(loginReuqest.EmailORUserName, "Confirm Your Email First");
                return BadRequest(keyValuePairs);
            }

            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var item in roles)
                claims.Add(new(ClaimTypes.Role, item));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]!));
            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                    issuer: _configuration["JWT:issuer"],
                    audience: _configuration["JWT:audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("JWT:AccessTokenExpiryMinutes", 60)),
                    signingCredentials: signingCredentials
                );

            //TempData["success_notification"] = $"Welcome Back {user.FirstName} {user.LastName}";
            //return RedirectToAction(nameof(HomeController.Index), SD.HOME_CONTROLER, new { area = SD.CUSTOMER_AREA });

            //return Ok(new
            //{
            //    msg = $"Welcome Back {user.FirstName} {user.LastName}"
            //});

            var url = Url.Action("Index", SD.HOME_CONTROLER, new { area = SD.CUSTOMER_AREA });
            return Created(url, new Response
            {
                Message = $"Welcome Back {user.FirstName} {user.LastName}",
                Body = [new JwtSecurityTokenHandler().WriteToken(token)],
                Status = 201
            });
        }
    }
}

namespace ExampleAPI.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<AppRole> _roleManager;        
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, RoleManager<AppRole> roleManager, ITokenService tokenService, IMapper mapper, IEmailService emailService, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;            
            _tokenService = tokenService;
            _mapper = mapper;
            _emailService = emailService;
            _env = env;
        }
        /// <summary>
        /// Get current logged in user.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("currentuser")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var user = await _userManager.FindByEmailFromClaimsPrincipal(User);

            return new UserDto
            {
                Email = user.Email,
                Token = await _tokenService.CreateToken(user),
                DisplayName = user.DisplayName
            };
        }
        /// <summary>
        /// User login.
        /// </summary>
        /// <param name="loginDto"></param>
        /// <returns></returns>
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null) return Unauthorized(new ApiResponse(401));
            //use this if lockout is not needed and if lockoutonfailure is false.
            //if (await _userManager.CheckPasswordAsync(user, loginDto.Password) == false) return Unauthorized(new ApiResponse { StatusCode = 401, Message = "Invalid Password." });
            if (!user.EmailConfirmed) return BadRequest("Email not verified!");
            var result = await _signInManager.PasswordSignInAsync(user, loginDto.Password, loginDto.RememberMe, lockoutOnFailure: true);
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(SendTwoStepEmail), new { user.Email, loginDto.RememberMe });
            }
            else if (result.Succeeded)
            {
                return new UserDto
                {
                    Email = user.Email,
                    Token = await _tokenService.CreateToken(user),
                    DisplayName = user.DisplayName
                };
            }
            else if (result.IsLockedOut)
            {
                var forgotPassLink = Url.Action(nameof(ForgotPassword), "Account", new { }, Request.Scheme);
                var content = string.Format("Your account is locked out, to reset your password, please click this link: {0}", forgotPassLink);
                var subject = "Locked out account information";
                var message = new Message(new List<EmailAddress> { new EmailAddress { DisplayName = user.DisplayName, Address = user.Email } }, subject, content, null);
                await _emailService.SendEmail(message);
                return Unauthorized("The account is locked out");
            }
            else if (await _userManager.CheckPasswordAsync(user, loginDto.Password) == false)
            {
                return Unauthorized(new ApiResponse { StatusCode = 401, Message = "Invalid Password." }); //remove if lockoutonfailure is false and use above.
            }
            else return Unauthorized(new ApiResponse { StatusCode = 401, Message = "Invalid Email or Password." });
        }
        /// <summary>
        /// To send two step code to email for signin.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="rememberMe"></param>
        /// <returns></returns>
        [HttpGet("send-two-step-email")]
        public async Task<IActionResult> SendTwoStepEmail(string email, bool rememberMe)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return BadRequest("User not found");
            var providers = await _userManager.GetValidTwoFactorProvidersAsync(user);
            if (!providers.Contains("Email"))
            {
                //maybe change error response
                return BadRequest(new ApiResponse(400));
            }
            var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
            var message = new Message(new List<EmailAddress> { new EmailAddress { DisplayName = user.DisplayName, Address = email } }, "Authentication token", token, null);
            await _emailService.SendEmail(message);
            //change return later maybe
            //TwoStepDto twoStepDto = new()
            //{
            //    RememberMe = rememberMe
            //};
            //return RedirectToAction(nameof(LoginTwoStep), new { twoStepDto.RememberMe });
            return Ok(new ApiResponse { StatusCode = 200, Message = "OTP send to registered email." });
        }
        /// <summary>
        /// To two step signin with the received code.
        /// </summary>
        /// <param name="twoStepDto"></param>
        /// <returns></returns>
        [HttpPost("login-two-step")]
        public async Task<ActionResult<UserDto>> LoginTwoStep(TwoStepDto twoStepDto)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
                return BadRequest("User not found");
            var result = await _signInManager.TwoFactorSignInAsync("Email", twoStepDto.TwoFactorCode, twoStepDto.RememberMe, rememberClient: false);
            if (result.Succeeded)
            {
                return new UserDto
                {
                    Email = user.Email,
                    Token = await _tokenService.CreateToken(user),
                    DisplayName = user.DisplayName
                };
            }
            else if (result.IsLockedOut)
            {
                var forgotPassLink = Url.Action(nameof(ForgotPassword), "Account", new { }, Request.Scheme);
                var content = string.Format("Your account is locked out, to reset your password, please click this link: {0}", forgotPassLink);
                var subject = "Locked out account information";
                var message = new Message(new List<EmailAddress> { new EmailAddress { DisplayName = user.DisplayName, Address = user.Email } }, subject, content, null);
                await _emailService.SendEmail(message);
                return Unauthorized("The account is locked out");
            }
            else
            {
                return Unauthorized(new ApiResponse(401));
            }
        }
        /// <summary>
        /// Creates a user.
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// POST /api/Account/user-registration
        /// {
        ///  "displayName": "srikarpabba",
        ///  "userName": "srikarpabba",
        ///  "firstName": "srikar",
        ///  "lastName": "pabba",
        ///  "emailAddress": "pabbasrikar@gmail.com",
        ///  "password": "Pa$$w0rd",
        ///  "confirmPassword": "Pa$$w0rd",
        ///  "profilePicture": "string"
        /// }
        /// </remarks>
        /// <param name="registerDto"></param>
        /// <returns></returns>
        [HttpPost("user-registration")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (CheckUserNameExistsAsync(registerDto.UserName).Result.Value)
            {
                return new BadRequestObjectResult(new ApiValidationErrorResponse
                { Errors = new[] { "Username is in use" } });
            }

            if (CheckEmailExistsAsync(registerDto.EmailAddress).Result.Value)
            {
                return new BadRequestObjectResult(new ApiValidationErrorResponse
                { Errors = new[] { "Email address is in use" } });
            }
            var user = _mapper.Map<RegisterDto, AppUser>(registerDto);
            //user.TwoFactorEnabled= true; //enabling two step login when creating new user.

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded) return BadRequest(new ApiResponse(400));

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            //token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));  //maybe checkout later

            var confirmationLink = Url.Action(nameof(ConfirmEmail), "Account", new { code, email = user.Email }, Request.Scheme);

            //Get TemplateFile located at wwwroot/Templates/EmailTemplate/Confirm_Account_Registration.html
            var pathToFile = _env.WebRootPath
                    + Path.DirectorySeparatorChar.ToString()
                    + "Templates"
                    + Path.DirectorySeparatorChar.ToString()
                    + "EmailTemplate"
                    + Path.DirectorySeparatorChar.ToString()
                    + "Confirm_Account_Registration.html";

            var subject = "Confirm Account Registration";

            var builder = new BodyBuilder();
            using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
            {
                builder.HtmlBody = SourceReader.ReadToEnd();
            }

            //{0} : Subject
            //{1} : DisplayName
            //{2} : confirmationLink

            string messageBody = string.Format(builder.HtmlBody,
                subject,
                user.DisplayName,
                confirmationLink
                );

            var message = new Message(new List<EmailAddress> { new EmailAddress { DisplayName = user.DisplayName, Address = user.Email } }, subject, messageBody, null);

            await _emailService.SendEmail(message);

            if (await _roleManager.RoleExistsAsync("User"))
            {
                await _userManager.AddToRoleAsync(user, "User");
            }

            return new UserDto
            {
                DisplayName = user.DisplayName,
                Token = await _tokenService.CreateToken(user),
                Email = user.Email
            };
        }
        /// <summary>
        /// Creates staff and assigns them a role.
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// POST /api/Account/staff-registration
        /// roleName: "Manager"
        /// {
        ///  "displayName": "srikarpabba",
        ///  "userName": "srikarpabba",
        ///  "firstName": "srikar",
        ///  "lastName": "pabba",
        ///  "emailAddress": "pabbasrikar@gmail.com",
        ///  "password": "Pa$$w0rd",
        ///  "confirmPassword": "Pa$$w0rd",
        ///  "profilePicture": "string"
        /// }
        /// </remarks>
        /// <param name="registerDto"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost("staff-registration")]
        public async Task<ActionResult<UserDto>> RegisterStaff(RegisterDto registerDto, string roleName)
        {
            if (CheckUserNameExistsAsync(registerDto.UserName).Result.Value)
            {
                return new BadRequestObjectResult(new ApiValidationErrorResponse
                { Errors = new[] { "Username is in use" } });
            }
            if (CheckEmailExistsAsync(registerDto.EmailAddress).Result.Value)
            {
                return new BadRequestObjectResult(new ApiValidationErrorResponse
                { Errors = new[] { "Email address is in use" } });
            }
            var user = _mapper.Map<RegisterDto, AppUser>(registerDto);

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded) return BadRequest(new ApiResponse(400));

            if (roleName != null && roleName.ToLower() != "admin")
            {
                if (await _roleManager.RoleExistsAsync(roleName))
                    await _userManager.AddToRoleAsync(user, roleName);
            }
            return new UserDto
            {
                DisplayName = user.DisplayName,
                Token = await _tokenService.CreateToken(user),
                Email = user.Email
            };
        }
        /// <summary>
        /// Signs out a user.
        /// </summary>
        /// <returns></returns>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (_signInManager.IsSignedIn(User))
            {
                await _signInManager.SignOutAsync();
            }
            // Clear the existing external cookie to ensure a clean login process
            //await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            return Ok(new ApiResponse { StatusCode = 200, Message = $"{User.Identity.Name} signed out." });
        }
        /// <summary>
        /// To send registered user reset password link.
        /// </summary>
        /// <param name="forgotPassword"></param>
        /// <returns></returns>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto forgotPassword)
        {
            var user = await _userManager.FindByEmailAsync(forgotPassword.Email);
            if (user == null)
                return new BadRequestObjectResult(new ApiValidationErrorResponse
                { Errors = new[] { "Invalid Email address" } });
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            //adding % in token look into it later
            //var callback = Url.Action(nameof(ResetPassword), "Account", new { user.Id, token }, Request.Scheme);
            var callback = $"{Request.Scheme}://{this.Request.Host}/api/Account/reset-password?userid={user.Id}&token={token}"; //probably replace later with above

            //Get TemplateFile located at wwwroot/Templates/EmailTemplate/Reset_Password.html
            var pathToFile = _env.WebRootPath
                    + Path.DirectorySeparatorChar.ToString()
                    + "Templates"
                    + Path.DirectorySeparatorChar.ToString()
                    + "EmailTemplate"
                    + Path.DirectorySeparatorChar.ToString()
                    + "Reset_Password.html";

            var subject = "Reset password request";

            var builder = new BodyBuilder();
            using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
            {
                builder.HtmlBody = SourceReader.ReadToEnd();
            }

            //{0} : Subject
            //{1} : DisplayName
            //{2} : callback

            string messageBody = string.Format(builder.HtmlBody,
                subject,
                user.DisplayName,
                callback
                );

            var message = new Message(new List<EmailAddress> { new EmailAddress { DisplayName = user.DisplayName, Address = user.Email } }, subject, messageBody, null);
            await _emailService.SendEmail(message);
            return Ok(new ApiResponse { StatusCode = 200, Message = "Password reset link sent succesfully!" });
        }
        /// <summary>
        /// To confirm email of the registered user.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpGet("confirmemail")]
        public async Task<IActionResult> ConfirmEmail(string code, string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return BadRequest("User not found");
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (!result.Succeeded)
            {
                return BadRequest("Invalid token");
            }
            else
            {
                return Ok(new ApiResponse { StatusCode = 200, Message = $"{User.Identity.Name}, Your email has been confirmed succesfully." });
            }
        }
        /// <summary>
        /// To reset user password.
        /// </summary>
        /// <param name="resetPassword"></param>
        /// <returns></returns>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto resetPassword)
        {
            var user = await _userManager.FindByIdAsync(resetPassword.UserId);
            if (user == null)
                return BadRequest("User not found");
            var result = await _userManager.ResetPasswordAsync(user, resetPassword.Token, resetPassword.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new ApiResponse(400));
            }
            else
            {
                return Ok(new ApiResponse { StatusCode = 200, Message = $"{User.Identity.Name}, Your password has been reset succesfully." });
            }
        }
        /// <summary>
        /// signin with google.
        /// </summary>
        /// <param name="externalAuth"></param>
        /// <returns></returns>
        [HttpPost("externallogin")]
        public async Task<IActionResult> ExternalLogin([FromBody] ExternalAuthDto externalAuth)
        {
            var payload = await _tokenService.VerifyGoogleToken(externalAuth);
            if (payload == null)
                return BadRequest("Invalid External Authentication.");
            var info = new UserLoginInfo(externalAuth.Provider, payload.Subject, externalAuth.Provider);
            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(payload.Email);
                if (user == null)
                {
                    user = new AppUser { Email = payload.Email, UserName = payload.Email };
                    await _userManager.CreateAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                    //token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));  //maybe checkout later

                    var confirmationLink = Url.Action(nameof(ConfirmEmail), "Account", new { code, email = user.Email }, Request.Scheme);

                    //Get TemplateFile located at wwwroot/Templates/EmailTemplate/Confirm_Account_Registration.html
                    var pathToFile = _env.WebRootPath
                            + Path.DirectorySeparatorChar.ToString()
                            + "Templates"
                            + Path.DirectorySeparatorChar.ToString()
                            + "EmailTemplate"
                            + Path.DirectorySeparatorChar.ToString()
                            + "Confirm_Account_Registration.html";

                    var subject = "Confirm Account Registration";

                    var builder = new BodyBuilder();
                    using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                    {
                        builder.HtmlBody = SourceReader.ReadToEnd();
                    }

                    //{0} : Subject
                    //{1} : DisplayName
                    //{2} : confirmationLink

                    string messageBody = string.Format(builder.HtmlBody,
                        subject,
                        user.Email,
                        confirmationLink
                        );

                    var message = new Message(new List<EmailAddress> { new EmailAddress { DisplayName = user.Email, Address = user.Email } }, subject, messageBody, null);

                    await _emailService.SendEmail(message);

                    if (await _roleManager.RoleExistsAsync("User"))
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                    }
                    await _userManager.AddLoginAsync(user, info);
                }
                else
                {
                    await _userManager.AddLoginAsync(user, info);
                }
            }
            if (user == null)
                return BadRequest("Invalid External Authentication.");
            return Ok(new UserDto
            {
                Email = user.Email,
                Token = await _tokenService.CreateToken(user),
                DisplayName = user.Email
            });
        }
        /// <summary>
        /// Returns true/false if email exists in database.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpGet("emailexists")]
        public async Task<ActionResult<bool>> CheckEmailExistsAsync([FromQuery] string email)
        {
            return await _userManager.FindByEmailAsync(email) != null;
        }
        /// <summary>
        /// Returns true/false if username exists in database.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [HttpGet("usernameexists")]
        public async Task<ActionResult<bool>> CheckUserNameExistsAsync([FromQuery] string username)
        {
            return await _userManager.FindByNameAsync(username) != null;
        }
    }
}

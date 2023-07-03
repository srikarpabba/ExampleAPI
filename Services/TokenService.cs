﻿namespace ExampleAPI.Services
{
    public class TokenService : ITokenService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _key;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public TokenService(IConfiguration config, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, ILogger<ExceptionMiddleware> logger)
        {
            _config = config;
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Token:Key"]));
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }
        public async Task<string> CreateToken(AppUser user)
        {
            var claims = await GetValidClaims(user);

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(7),
                SigningCredentials = creds,
                Issuer = _config["Token:Issuer"],
            };
            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        private async Task<List<Claim>> GetValidClaims(AppUser user)
        {
            //IdentityOptions _options = new();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.DisplayName),
                //new Claim(JwtRegisteredClaimNames.Email, user.Email),
                //new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                //new Claim(_options.ClaimsIdentity.UserIdClaimType, user.Id.ToString()),
                //new Claim(_options.ClaimsIdentity.UserNameClaimType, user.UserName),
                //add more properties from claimtypes to test
            };
            var userClaims = await _userManager.GetClaimsAsync(user);
            var userRoles = await _userManager.GetRolesAsync(user);
            claims.AddRange(userClaims);
            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
                //maybe remove below code later if not needed.
                var role = await _roleManager.FindByNameAsync(userRole);
                if (role != null)
                {
                    var roleClaims = await _roleManager.GetClaimsAsync(role);
                    foreach (Claim roleClaim in roleClaims)
                    {
                        claims.Add(roleClaim);
                    }
                }
            }
            return claims;
        }

        public async Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(ExternalAuthDto externalAuth)
        {
            try
            {
                var googleAuth = _config.GetSection("Authentication:Google");
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string>() { googleAuth["ClientId"] }
                };
                var payload = await GoogleJsonWebSignature.ValidateAsync(externalAuth.AccessToken, settings);
                return payload;
            }
            catch (Exception ex)
            {
                //log an exception
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }
    }
}

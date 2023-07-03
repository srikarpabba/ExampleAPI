namespace ExampleAPI.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        public UserController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpPost("ChangeUserName")]
        public async Task ChangeUserName(string userName)
        {
            var x = await _userManager.FindByNameAsync(userName);
        }
    }
}

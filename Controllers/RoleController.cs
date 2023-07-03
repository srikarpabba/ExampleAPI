namespace ExampleAPI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoleController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IMapper _mapper;

        public RoleController(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, IMapper mapper)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
        }

        /// <summary>
        /// Creates a role.
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// POST /api/Role/createrole
        /// {
        ///   "name": "Manager",
        ///   "description": "Takes care of the inventory and staff"
        /// }
        /// </remarks>
        /// <param name="appRole"></param>
        /// <returns></returns>
        [HttpPost("createrole")]
        public async Task<ActionResult<AppRole>> CreateRole([FromBody] AppRole appRole)
        {
            if (appRole == null || appRole.Name == null)
            {
                return BadRequest(new ApiResponse(400));
            }
            appRole.Name = char.ToUpper(appRole.Name[0]) + appRole.Name.Substring(1);
            var role = _mapper.Map<AppRole>(appRole);
            IdentityResult result = await _roleManager.CreateAsync(role);

            if (!result.Succeeded)
            {
                foreach (IdentityError error in result.Errors)
                {
                    return BadRequest(error.Description);
                }
                return BadRequest("Unable to create a role.");
            }

            return Ok(new ApiResponse { StatusCode = 200, Message = $"{role.Name} role created successfully!" });
        }

        /// <summary>
        /// Gets list of all roles from AspNetRoles table.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("getallroles")]
        public async Task<ActionResult<IList<AppRole>>> GetAllRoles(CancellationToken cancellationToken = default)
        {
            var roles = await _roleManager.Roles.ToListAsync(cancellationToken);
            if (roles == null)
            {
                return BadRequest(new ApiResponse(404));
            }
            return roles;
        }

        /// <summary>
        /// Gets role from AspNetRoles table by id.
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("getrolebyId")]
        public async Task<ActionResult<AppRole>> GetRoleById(string roleId)
        {
            if (string.IsNullOrEmpty(roleId))
            {
                return BadRequest(new ApiResponse(400));
            }
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return BadRequest(new ApiResponse(404));
            }
            return role;
        }

        /// <summary>
        /// Deletes role from AspNetRoles table by id.
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        [HttpDelete("deleterole")]
        public async Task<ActionResult<AppRole>> DeleteRole(string roleId)
        {
            if (string.IsNullOrEmpty(roleId))
            {
                return BadRequest(new ApiResponse(400));
            }
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return BadRequest(new ApiResponse(404));
            }
            IdentityResult result = await _roleManager.DeleteAsync(role);

            if (!result.Succeeded)
            {
                return BadRequest("Unable to delete the role.");
            }
            return Ok(new ApiResponse { StatusCode = 200, Message = $"{role.Name} deleted successfully!" });
        }
    }
}

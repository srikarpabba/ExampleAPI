namespace ExampleAPI.Data.Identity
{
    public class AppIdentityDbContextSeed
    {
        public static async Task SeedUsersAsync(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
        {
            if (!userManager.Users.Any())
            {
                var user = new AppUser
                {
                    DisplayName = "sri123",
                    Email = "pabbasrikar@gmail.com",
                    UserName = "srikarpabba",
                    FirstName = "Srikar",
                    LastName = "pabba",
                    Address = new Address
                    {
                        Street = "10 The street",
                        City = "Hyderabad",
                        State = "TS",
                        Zipcode = "502032"
                    }
                };

                await userManager.CreateAsync(user, "Pa$$w0rd");
                var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                await userManager.ConfirmEmailAsync(user, code);

                if (await roleManager.RoleExistsAsync("Admin"))
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }
        }
    }
}

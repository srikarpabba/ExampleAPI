namespace ExampleAPI.Data.Identity
{
    public class RoleConfiguration : IEntityTypeConfiguration<AppRole>
    {
        public void Configure(EntityTypeBuilder<AppRole> builder)
        {
            builder.HasData(
                 new AppRole
                 {
                     Name = "User",
                     NormalizedName = "USER",
                     Description = "general user"
                 },
                 new AppRole
                 {
                     Name = "Admin",
                     NormalizedName = "ADMIN",
                     Description = "has all the admin privileges"
                 });
        }
    }
}
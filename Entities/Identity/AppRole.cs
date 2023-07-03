namespace ExampleAPI.Entities.Identity
{
    public class AppRole : IdentityRole
    {
        public AppRole() { }
        public AppRole(string roleName) : base(roleName)
        {
            Name = roleName;
        }
        public string Description { get; set; }
        [IgnoreDataMember]
        public override string Id { get; set; } = Guid.NewGuid().ToString();
        [IgnoreDataMember]
        public override string NormalizedName { get; set; }
        [IgnoreDataMember]
        public override string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
    }
}

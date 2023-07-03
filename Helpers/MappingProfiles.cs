namespace ExampleAPI.Helpers
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            CreateMap<RegisterDto, AppUser>()
               .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.EmailAddress));
        }
    }
}

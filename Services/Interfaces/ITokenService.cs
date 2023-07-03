namespace ExampleAPI.Services.Interfaces
{
    public interface ITokenService
    {
        Task<string> CreateToken(AppUser user);
        Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(ExternalAuthDto externalAuth);
    }
}

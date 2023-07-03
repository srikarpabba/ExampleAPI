namespace ExampleAPI.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmail(Message message);
    }
}

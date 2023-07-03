namespace ExampleAPI.Dtos
{
    public class ExternalAuthDto
    {
        [Required]
        public string Provider { get; set; }

        [Required]
        public string AccessToken { get; set; }
    }
}

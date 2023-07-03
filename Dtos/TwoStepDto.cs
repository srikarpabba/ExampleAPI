namespace ExampleAPI.Dtos
{
    public class TwoStepDto
    {
        [Required]
        [DataType(DataType.Text)]
        public string TwoFactorCode { get; set; }
        public bool RememberMe { get; set; }
    }
}

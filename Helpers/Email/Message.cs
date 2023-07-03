namespace ExampleAPI.Helpers.Email
{
    public class EmailAddress
    {
        public string Address { get; set; }
        public string DisplayName { get; set; }
    }
    public class Message
    {
        public List<MailboxAddress> To { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
        public IFormFileCollection Attachments { get; set; }

        public Message(IEnumerable<EmailAddress> to, string subject, string content, IFormFileCollection attachments)
        {
            To = new List<MailboxAddress>();

            To.AddRange(to.Select(x => new MailboxAddress(x.DisplayName, x.Address)));
            Subject = subject;
            Content = content;
            Attachments = attachments;
        }
    }
}

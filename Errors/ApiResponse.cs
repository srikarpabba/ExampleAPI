namespace ExampleAPI.Errors
{
    public class ApiResponse
    {
        public ApiResponse()
        { }

        public ApiResponse(int statusCode, string message = null)
        {
            StatusCode = statusCode;
            Message = message ?? GetDefaultMessageForStatusCode(statusCode);
        }

        public int StatusCode { get; set; }
        public string Message { get; set; }

        private static string GetDefaultMessageForStatusCode(int statusCode)
        {
            return statusCode switch
            {
                //maybe change messages later
                400 => "Bad request made",
                401 => "Unauthorized",
                404 => "Not found",
                500 => "Internal server error",
                _ => null
            };
        }
    }
}

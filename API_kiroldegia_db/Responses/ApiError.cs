namespace API_kiroldegia_db.Responses
{
    public class ApiError
    {
        public string Status { get; set; } = "error";
        public string Type { get; set; } = "ERROR";
        public int Code { get; set; } = 500;
        public string Message { get; set; } = "";
        public string? Detail { get; set; }

        public static ApiError Create(int code, string type, string message, string? detail = null)
            => new() { Code = code, Type = type, Message = message, Detail = detail };
    }
}

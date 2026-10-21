using API_kiroldegia_db.Responses;
using Microsoft.AspNetCore.Mvc;

namespace API_kiroldegia_db.Controllers
{
    [ApiController]
    public abstract class BaseApiKontroladorea : ControllerBase
    {
        protected IActionResult Success<T>(T data, int code = 200, string message = "OK")
            => Ok(ApiResponse<T>.Ok(data, code, message));

        protected IActionResult SuccessList<T>(List<T> datalist, int code = 200, string message = "OK")
            => Ok(ApiResponse<T>.Ok(datalist, code, message));

        protected IActionResult Created<T>(T data, int code = 201, string message = "Zuzen sortu da")
            => StatusCode(201, ApiResponse<T>.Ok(data, code, message));

        protected IActionResult Error(int code, string type, string message, string? detail = null)
            => StatusCode(code, ApiError.Create(code, type, message, detail));

        protected IActionResult NotFoundError(string detail)
            => Error(404, "NOT_FOUND", "Eskatutakoa ez da aurkitu", detail);
    }
}

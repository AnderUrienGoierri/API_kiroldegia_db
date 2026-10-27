using API_kiroldegia_db.Responses;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers
{
    [ApiController]
    public abstract class BaseApiKontroladorea : ControllerBase
    {
        protected readonly IConfiguration _config;

        protected BaseApiKontroladorea(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// MySQL datu-basearekin konexioa irekitzen du ('Kiroldegia' edo 'KiroldegiaRoot' erabiliz).
        /// Klase ume guztiek metodo hau heredatzen dute.
        /// </summary>
        protected async Task<MySqlConnection> SortuKonexioaAsync()
        {
            string? katea = _config.GetConnectionString("Kiroldegia");
            try
            {
                var kon = new MySqlConnection(katea);
                await kon.OpenAsync();
                return kon;
            }
            catch
            {
                string? rootKatea = _config.GetConnectionString("KiroldegiaRoot");
                var kon = new MySqlConnection(rootKatea);
                await kon.OpenAsync();
                return kon;
            }
        }

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

using Microsoft.AspNetCore.Mvc;

namespace API_kiroldegia_db.Controllers;

[ApiController]
[Route("api/pistak")]
public partial class PistakKontroladorea : BaseApiKontroladorea
{
    private readonly ILogger<PistakKontroladorea> _logger;

    public PistakKontroladorea(IConfiguration config, ILogger<PistakKontroladorea> logger)
        : base(config)
    {
        _logger = logger;
    }
}


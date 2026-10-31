using Microsoft.AspNetCore.Mvc;

namespace API_kiroldegia_db.Controllers;

[ApiController]
[Route("api/bonoak")]
[Route("api/[controller]")]
public partial class BonoakKontroladorea : BaseApiKontroladorea
{
    private readonly ILogger<BonoakKontroladorea> _logger;

    public BonoakKontroladorea(IConfiguration config, ILogger<BonoakKontroladorea> logger)
        : base(config)
    {
        _logger = logger;
    }
}


using Microsoft.AspNetCore.Mvc;

namespace API_kiroldegia_db.Controllers;

[ApiController]
[Route("api/bazkideak")]
public partial class BazkideakKontroladorea : BaseApiKontroladorea
{
    private readonly ILogger<BazkideakKontroladorea> _logger;

    public BazkideakKontroladorea(IConfiguration config, ILogger<BazkideakKontroladorea> logger)
        : base(config)
    {
        _logger = logger;
    }
}


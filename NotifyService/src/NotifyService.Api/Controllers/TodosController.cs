using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace NotifyService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NotifyController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<NotifyController> _logger;

    public NotifyController(IMediator mediator, ILogger<NotifyController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
}
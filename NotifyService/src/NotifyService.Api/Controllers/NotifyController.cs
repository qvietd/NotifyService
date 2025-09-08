using MediatR;
using Microsoft.AspNetCore.Mvc;
using NotifyService.Application.Features.Notify.Queries;

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

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications(
    [FromQuery] string userId,
    [FromQuery] int page = 1,
    [FromQuery] int limit = 20)
    {
        var result = await _mediator.Send(new GetNotifyQuery(userId,page,limit));
        return Ok(result);
    }
}
using EnergyOrigin.VerifiableEventStore.Api.Features.PublishEvent.Models;
using Microsoft.AspNetCore.Mvc;

namespace EnergyOrigin.VerifiableEventStore.Api.Features.PublishEvent;

[ApiController]
[Route("[controller]")]
public class PublishEventController : ControllerBase
{
    [HttpPost(Name = "PublishEvent")]
    public void PublishEvent(PublishEventRequest request)
    {
        throw new NotImplementedException("Will be implemented when needed.");
    }
}

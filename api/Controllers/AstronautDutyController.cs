using MediatR;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Queries;
using System.Net;

namespace StargateAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AstronautDutyController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AstronautDutyController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetAstronautDutiesByName(string name)
        {
            return await SendResponseAsync(() => _mediator.Send(new GetAstronautDutiesByName()
            {
                Name = name
            }));
        }

        [HttpPost("")]
        public async Task<IActionResult> CreateAstronautDuty([FromBody] CreateAstronautDuty request)
        {
            try
            {
                var result = await _mediator.Send(request);
                return this.GetResponse(result);
            }
            catch (BadHttpRequestException ex)
            {
                return this.GetErrorResponse(ex.Message, HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                return this.GetErrorResponse(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        private async Task<IActionResult> SendResponseAsync<TResponse>(Func<Task<TResponse>> send)
            where TResponse : BaseResponse
        {
            try
            {
                var result = await send();
                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                return this.GetErrorResponse(ex.Message, HttpStatusCode.InternalServerError);
            }
        }
    }
}

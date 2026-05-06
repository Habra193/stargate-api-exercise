using MediatR;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Queries;
using System.Net;

namespace StargateAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PersonController : ControllerBase
    {
        private readonly IMediator _mediator;
        public PersonController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetPeople()
        {
            return await SendResponseAsync(() => _mediator.Send(new GetPeople()));
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetPersonByName(string name)
        {
            return await SendResponseAsync(() => _mediator.Send(new GetPersonByName()
            {
                Name = name
            }));
        }

        [HttpPost("")]
        public async Task<IActionResult> CreatePerson([FromBody] string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return this.GetErrorResponse("Person name is required", HttpStatusCode.BadRequest);
                }

                name = name.Trim();

                var exists = await _mediator.Send(new GetPersonByName()
                {
                    Name = name
                });

                if (exists.Person is not null)
                {
                    return PersonAlreadyExistsResponse(name);
                }

                var result = await _mediator.Send(new CreatePerson()
                {
                    Name = name
                });

                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                return this.GetErrorResponse(ex.Message, HttpStatusCode.InternalServerError);
            }

        }

        [HttpPut("{oldName}")]
        public async Task<IActionResult> UpdatePersonName(string oldName, [FromBody] UpdatePersonNameRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.NewName))
                {
                    return this.GetErrorResponse("Person name is required", HttpStatusCode.BadRequest);
                }

                oldName = oldName.Trim();
                request.NewName = request.NewName.Trim();

                var existingPerson = await _mediator.Send(new GetPersonByName()
                {
                    Name = oldName
                });

                if (existingPerson.Person is null)
                {
                    return this.GetResponse(existingPerson);
                }

                var existingNewName = await _mediator.Send(new GetPersonByName()
                {
                    Name = request.NewName
                });

                if (existingNewName.Person is not null)
                {
                    return PersonAlreadyExistsResponse(request.NewName);
                }

                var result = await _mediator.Send(new UpdatePersonName()
                {
                    OldName = oldName,
                    NewName = request.NewName
                });

                return this.GetResponse(result);
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

        private IActionResult PersonAlreadyExistsResponse(string name)
        {
            return this.GetErrorResponse("Person already exists in database: " + name, HttpStatusCode.BadRequest);
        }
    }

    public class UpdatePersonNameRequest
    {
        public string NewName { get; set; } = string.Empty;
    }
}

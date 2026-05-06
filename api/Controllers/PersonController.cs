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
            try
            {
                var result = await _mediator.Send(new GetPeople()
                {

                });

                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                return this.GetResponse(new BaseResponse()
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetPersonByName(string name)
        {
            try
            {
                var result = await _mediator.Send(new GetPersonByName()
                {
                    Name = name
                });

                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                return this.GetResponse(new BaseResponse()
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpPost("")]
        public async Task<IActionResult> CreatePerson([FromBody] string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return this.GetResponse(new BaseResponse()
                    {
                        Message = "Person name is required",
                        Success = false,
                        ResponseCode = (int)HttpStatusCode.BadRequest
                    });
                }

                var exists = await _mediator.Send(new GetPersonByName()
                {
                    Name = name
                });

                if (exists.Person is not null)
                {
                    return this.GetResponse(new BaseResponse()
                    {
                        Message = "Person already exists in database: " + name,
                        Success = false,
                        ResponseCode = (int)HttpStatusCode.BadRequest
                    });
                }
                var result = await _mediator.Send(new CreatePerson()
                {
                    Name = name
                });

                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                return this.GetResponse(new BaseResponse()
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                });
            }

        }

        [HttpPut("{oldName}")]
        public async Task<IActionResult> UpdatePersonName(string oldName, [FromBody] UpdatePersonNameRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.NewName))
                {
                    return this.GetResponse(new BaseResponse()
                    {
                        Message = "Person name is required",
                        Success = false,
                        ResponseCode = (int)HttpStatusCode.BadRequest
                    });
                }

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
                    return this.GetResponse(new BaseResponse()
                    {
                        Message = "Person already exists in database: " + request.NewName,
                        Success = false,
                        ResponseCode = (int)HttpStatusCode.BadRequest
                    });
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
                return this.GetResponse(new BaseResponse()
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                });
            }
        }
    }

    public class UpdatePersonNameRequest
    {
        public string NewName { get; set; } = string.Empty;
    }
}

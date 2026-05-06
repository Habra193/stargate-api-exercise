using MediatR;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;
using System.Net;

namespace StargateAPI.Business.Commands
{
    public class UpdatePersonName : IRequest<UpdatePersonNameResult>
    {
        public required string OldName { get; set; } = string.Empty;

        public required string NewName { get; set; } = string.Empty;
    }

    public class UpdatePersonNameHandler : IRequestHandler<UpdatePersonName, UpdatePersonNameResult>
    {
        private readonly StargateContext _context;

        public UpdatePersonNameHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<UpdatePersonNameResult> Handle(UpdatePersonName request, CancellationToken cancellationToken)
        {
            var person = await _context.People
                .SingleOrDefaultAsync(x => x.Name == request.OldName, cancellationToken);

            if (person is null)
            {
                return new UpdatePersonNameResult()
                {
                    Success = false,
                    Message = "Person not found",
                    ResponseCode = (int)HttpStatusCode.NotFound
                };
            }

            person.Name = request.NewName;

            await _context.SaveChangesAsync(cancellationToken);

            return new UpdatePersonNameResult()
            {
                Id = person.Id,
                Name = person.Name
            };
        }
    }

    public class UpdatePersonNameResult : BaseResponse
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}

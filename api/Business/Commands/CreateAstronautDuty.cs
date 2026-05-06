using Dapper;
using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;
using System.Net;

namespace StargateAPI.Business.Commands
{
    public class CreateAstronautDuty : IRequest<CreateAstronautDutyResult>
    {
        public required string Name { get; set; }

        public required string Rank { get; set; }

        public required string DutyTitle { get; set; }

        public DateTime DutyStartDate { get; set; }
    }

    public class CreateAstronautDutyPreProcessor : IRequestPreProcessor<CreateAstronautDuty>
    {
        private readonly StargateContext _context;

        public CreateAstronautDutyPreProcessor(StargateContext context)
        {
            _context = context;
        }

        public Task Process(CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Rank) ||
                string.IsNullOrWhiteSpace(request.DutyTitle) ||
                request.DutyStartDate == default)
            {
                throw new BadHttpRequestException("Bad Request");
            }

            request.Name = request.Name.Trim();
            request.Rank = request.Rank.Trim();
            request.DutyTitle = ToTitleCase(request.DutyTitle);

            var person = _context.People.AsNoTracking().FirstOrDefault(z => z.Name == request.Name);

            if (person is null) throw new BadHttpRequestException("Bad Request");

            var verifyNoPreviousDuty = _context.AstronautDuties.FirstOrDefault(z => z.DutyTitle == request.DutyTitle && z.DutyStartDate == request.DutyStartDate);

            if (verifyNoPreviousDuty is not null) throw new BadHttpRequestException("Bad Request");

            return Task.CompletedTask;
        }

        private static string ToTitleCase(string value)
        {
            return string.Join(
                " ",
                value.Trim()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
        }
    }

    public class CreateAstronautDutyHandler : IRequestHandler<CreateAstronautDuty, CreateAstronautDutyResult>
    {
        private const string RetiredDutyTitle = "Retired";
        private readonly StargateContext _context;

        public CreateAstronautDutyHandler(StargateContext context)
        {
            _context = context;
        }
        public async Task<CreateAstronautDutyResult> Handle(CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var query = @"
                SELECT
                    Id,
                    Name
                FROM [Person]
                WHERE Name = @Name";

            var person = await _context.Connection.QueryFirstOrDefaultAsync<Person>(
                query,
                new { Name = request.Name });

            if (person is null)
            {
                return new CreateAstronautDutyResult()
                {
                    Success = false,
                    Message = "Person not found",
                    ResponseCode = (int)HttpStatusCode.BadRequest
                };
            }

            query = @"
                SELECT
                    Id,
                    CareerEndDate,
                    CareerStartDate,
                    CurrentDutyTitle,
                    CurrentRank,
                    PersonId
                FROM [AstronautDetail]
                WHERE PersonId = @PersonId";

            var astronautDetail = await _context.Connection.QueryFirstOrDefaultAsync<AstronautDetail>(
                query,
                new { PersonId = person.Id });

            if (astronautDetail == null)
            {
                astronautDetail = new AstronautDetail();
                astronautDetail.PersonId = person.Id;
                astronautDetail.CurrentDutyTitle = request.DutyTitle;
                astronautDetail.CurrentRank = request.Rank;
                astronautDetail.CareerStartDate = request.DutyStartDate.Date;
                if (request.DutyTitle == RetiredDutyTitle)
                {
                    astronautDetail.CareerEndDate = request.DutyStartDate.Date;
                }

                await _context.AstronautDetails.AddAsync(astronautDetail);

            }
            else
            {
                astronautDetail.CurrentDutyTitle = request.DutyTitle;
                astronautDetail.CurrentRank = request.Rank;
                if (request.DutyTitle == RetiredDutyTitle)
                {
                    astronautDetail.CareerEndDate = request.DutyStartDate.AddDays(-1).Date;
                }
                _context.AstronautDetails.Update(astronautDetail);
            }

            query = @"
                SELECT
                    Id,
                    PersonId,
                    Rank,
                    DutyTitle,
                    DutyStartDate,
                    DutyEndDate
                FROM [AstronautDuty]
                WHERE PersonId = @PersonId
                Order By DutyStartDate Desc";

            var astronautDuty = await _context.Connection.QueryFirstOrDefaultAsync<AstronautDuty>(
                query,
                new { PersonId = person.Id });

            if (astronautDuty != null)
            {
                astronautDuty.DutyEndDate = request.DutyStartDate.AddDays(-1).Date;
                _context.AstronautDuties.Update(astronautDuty);
            }

            var newAstronautDuty = new AstronautDuty()
            {
                PersonId = person.Id,
                Rank = request.Rank,
                DutyTitle = request.DutyTitle,
                DutyStartDate = request.DutyStartDate.Date,
                DutyEndDate = null
            };

            await _context.AstronautDuties.AddAsync(newAstronautDuty);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync(cancellationToken);

            return new CreateAstronautDutyResult()
            {
                Id = newAstronautDuty.Id
            };
        }
    }

    public class CreateAstronautDutyResult : BaseResponse
    {
        public int? Id { get; set; }
    }
}

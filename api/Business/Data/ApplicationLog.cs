using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
namespace StargateAPI.Business.Data
{
    [Table("ApplicationLog")] //prevents EF from infering table name
    public class ApplicationLog
    {
        public int Id {get; set;}
        public string LogLevel {get; set;}  = string.Empty;
        public string Operation {get; set;}  = string.Empty;
        public string Message {get; set;}  = string.Empty;
        public string? ExceptionMessage {get;set;}
        public string? ExceptionStackTrace {get;set;}
        public DateTime CreatedAt {get; set;} = DateTime.UtcNow;

    }

}

using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace StargateAPI.Controllers
{
    public static class ControllerBaseExtensions
    {
        public static IActionResult GetResponse(this ControllerBase controllerBase, BaseResponse response)
        {
            var httpResponse = new ObjectResult(response);
            httpResponse.StatusCode = response.ResponseCode;
            return httpResponse;
        }

        public static IActionResult GetErrorResponse(this ControllerBase controllerBase, string message, HttpStatusCode responseCode)
        {
            return controllerBase.GetResponse(new BaseResponse()
            {
                Message = message,
                Success = false,
                ResponseCode = (int)responseCode
            });
        }
    }
}

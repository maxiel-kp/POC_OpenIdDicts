using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ClientCredentials.Server.Controllers
{
    [Route("api")]
    public class ResourceController : ControllerBase
    {
        private readonly IOpenIddictApplicationManager _applicationManager;

        public ResourceController(IOpenIddictApplicationManager applicationManager)
            => _applicationManager = applicationManager;

        [HttpGet("message")]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetMessage()
        {
            var subject = User.FindFirst(Claims.Subject)?.Value;
            if (string.IsNullOrEmpty(subject))
            {
                return BadRequest();
            }

            var application = await _applicationManager.FindByClientIdAsync(subject);
            if (application == null)
            {
                return BadRequest();
            }

            return Content($"{await _applicationManager.GetDisplayNameAsync(application)} has been successfully authenticated.");
        }
    }
}
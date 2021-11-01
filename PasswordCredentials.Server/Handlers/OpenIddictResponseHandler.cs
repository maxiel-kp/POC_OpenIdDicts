using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using IC.Common.Helpers;
using IC.Common.Web.DTOs;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace PasswordCredentials.Server.Handlers
{
    /// <summary>
    /// Customize response for <see cref="BaseResponseResult"/>.
    /// </summary>
    public class OpenIddictResponseHandler : IOpenIddictServerHandler<ApplyTokenResponseContext>
    {
        /// <inheritdoc/>
        public ValueTask HandleAsync(ApplyTokenResponseContext context)
        {
            bool isSuccess = context.Error.IsEmpty();
            var dictResults = isSuccess ? context.Response.GetParameters().ToDictionary(k => k.Key, v => v.Value.Value) : null;
            string json = JsonSerializer.Serialize(new BaseResponseResult
            {
                StatusCode = 200,
                Result = dictResults,
                IsSuccess = isSuccess,
                Errors = isSuccess ? null : new List<ErrorResponseResult>
                {
                    new ErrorResponseResult
                    {
                        Id = context.Error,
                        Code = context.Response.ErrorUri,
                        Message = context.Response.ErrorDescription
                    }
                }
            });

            context.Response = new OpenIddictResponse(JsonDocument.Parse(json).RootElement);
            return default;
        }
    }
}

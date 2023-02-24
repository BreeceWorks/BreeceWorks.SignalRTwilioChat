using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twilio.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Twilio.Security;

namespace BreeceWorks.Authentication
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ValidateTwilioRequestAttribute : TypeFilterAttribute
    {
        public ValidateTwilioRequestAttribute() : base(typeof(ValidateTwilioRequestFilter))
        {
        }
    }

    internal class ValidateTwilioRequestFilter : IAsyncActionFilter
    {
        private readonly RequestValidator _requestValidator;
        private readonly bool _isEnabled;

        public ValidateTwilioRequestFilter(IConfiguration configuration)
        {
            Boolean isEnabledValue = false;
            var authToken = configuration["Twilio:AuthToken"] ?? throw new Exception("'Twilio:AuthToken' not configured.");
            _requestValidator = new RequestValidator(authToken);
            _isEnabled = Boolean.TryParse(configuration["Twilio:RequestValidation:Enabled"], out isEnabledValue) ? isEnabledValue : false;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!_isEnabled)
            {
                await next();
                return;
            }

            var httpContext = context.HttpContext;
            var request = httpContext.Request;

            var requestUrl = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
            Dictionary<string, string> parameters = null;

            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync(httpContext.RequestAborted).ConfigureAwait(false);
                parameters = form.ToDictionary(p => p.Key, p => p.Value.ToString());
            }

            var signature = request.Headers["X-Twilio-Signature"];
            var isValid = _requestValidator.Validate(requestUrl, parameters, signature);

            if (!isValid)
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            await next();
        }
    }
}

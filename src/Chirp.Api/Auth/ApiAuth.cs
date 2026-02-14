using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace Chirp.Api.Auth
{
    /// <summary>
    /// Dummy authentication handler. Does nothing. Actual validation happens in authorization. 
    /// Required for using the asp net auth pipeline.
    /// </summary>
    public class ApiAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        /// <summary>
        /// 
        /// </summary>
        public ApiAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Create a generic identity. Actual validation happens in authorization
            var claims = new[] { new Claim(ClaimTypes.Name, "ApiUser") };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    /// <summary>
    /// A requirement that an ApiKey must be present.
    /// </summary>
    public class ApiKeyRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Get the list of api keys
        /// </summary>
        public IReadOnlyList<string> ApiKeys { get; }

        /// <summary>
        /// Get the policy name,
        /// </summary>
        public string PolicyName { get; }

        /// <summary>
        /// Create a new instance of the <see cref="ApiKeyRequirement"/> class.
        /// </summary>
        /// <param name="apiKeys"></param>
        /// <param name="policyName"></param>
        public ApiKeyRequirement(IEnumerable<string> apiKeys, string policyName)
        {
            ApiKeys = apiKeys?.ToList() ?? new List<string>();
            PolicyName = policyName;
        }
    }

    /// <summary>
    /// Authorization handler. Enforces that the correct api key is present.
    /// </summary>
    public class ApiAuthorizationHandler : AuthorizationHandler<ApiKeyRequirement>
    {
        /// <copydoc cref="AuthorizationHandler{T}.HandleRequirementAsync" />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ApiKeyRequirement requirement)
        {
            SucceedRequirementIfApiKeyPresentAndValid(context, requirement);
            return Task.CompletedTask;
        }

        private void SucceedRequirementIfApiKeyPresentAndValid(AuthorizationHandlerContext context, ApiKeyRequirement requirement)
        {
            if (context.Resource is not HttpContext httpContext) return;
            var headers = httpContext.Request.Headers;

            if (!headers.TryGetValue("Authorization", out var values)) return;
            string? apiKey = values.FirstOrDefault();

            if (apiKey != null && requirement.ApiKeys.Contains(apiKey))
            {
                context.Succeed(requirement);
            }
        }
    }
}

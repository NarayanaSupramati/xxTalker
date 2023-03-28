using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace xxTalker.Server.Filters
{
    /// <summary>    
    /// ActionFilterAttribute to validate referrer url    
    /// </summary>    
    /// <seealso cref="Microsoft.AspNetCore.Mvc.Filters.ActionFilterAttribute" />    
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ValidateReferrerAttribute : ActionFilterAttribute
    {
        private IConfiguration _configuration;
        /// <summary>    
        /// Initializes a new instance of the <see cref="ValidateReferrerAttribute"/> class.    
        /// </summary>    
        public ValidateReferrerAttribute() { }
        /// <summary>    
        /// Called when /[action executing].    
        /// </summary>    
        /// <param name="context">The action context.</param>    
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _configuration = (IConfiguration)context.HttpContext.RequestServices.GetService(typeof(IConfiguration));
            base.OnActionExecuting(context);
            if (!IsValidRequest(context.HttpContext.Request))
            {
                context.Result = new ContentResult
                {
                    Content = $"Invalid referer header"
                };
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.ExpectationFailed;
            }
        }
        /// <summary>    
        /// Determines whether /[is valid request] [the specified request].    
        /// </summary>    
        /// <param name="request">The request.</param>    
        /// <returns>    
        /// <c>true</c> if [is valid request] [the specified request]; otherwise, <c>false</c>.    
        /// </returns>    
        private bool IsValidRequest(HttpRequest request)
        {
            string referrerURL = "";
            if (request.Headers.ContainsKey("Referer"))
            {
                referrerURL = request.Headers["Referer"];
            }
            if (string.IsNullOrWhiteSpace(referrerURL)) return false;
            // get allowed client list to check
            var allowedUrls = new List<string>();
            var corsOrigins = _configuration.GetSection("CorsOrigin").Get<string[]>()?.Select(url => new Uri(url).Authority).ToList();
            if (corsOrigins != null)
            {
                allowedUrls.AddRange(corsOrigins);
            }
            //add current host for swagger calls    
            var host = request.Host.Value;
            allowedUrls.Add(host);
            bool isValidClient = allowedUrls.Contains(new Uri(referrerURL).Authority); // comapre with base uri    
            return isValidClient;
        }
    }
}

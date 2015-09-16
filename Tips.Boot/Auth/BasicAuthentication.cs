using Nancy;
using Nancy.Bootstrapper;
using Nancy.Extensions;
using Nancy.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.Boot.Auth
{
    /// <summary>
    /// Nancy basic authentication implementation
    /// </summary>
    public static class BasicAuthentication
    {
        private const string SCHEME = "Basic";

        /// <summary>
        /// Enables basic authentication for the application
        /// </summary>
        /// <param name="pipelines">Pipelines to add handlers to (usually "this")</param>
        /// <param name="configuration">Forms authentication configuration</param>
        public static void Enable(IPipelines pipelines, BasicAuthenticationConfiguration configuration)
        {
            if (pipelines == null)
            {
                throw new ArgumentNullException("pipelines");
            }

            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            pipelines.BeforeRequest.AddItemToStartOfPipeline(GetCredentialRetrievalHook(configuration));
            pipelines.AfterRequest.AddItemToEndOfPipeline(GetAuthenticationPromptHook(configuration));
        }

        /// <summary>
        /// Enables basic authentication for a module
        /// </summary>
        /// <param name="module">Module to add handlers to (usually "this")</param>
        /// <param name="configuration">Forms authentication configuration</param>
        public static void Enable(INancyModule module, BasicAuthenticationConfiguration configuration)
        {
            if (module == null)
            {
                throw new ArgumentNullException("module");
            }

            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            module.RequiresAuthentication();
            module.Before.AddItemToStartOfPipeline(GetCredentialRetrievalHook(configuration));
            module.After.AddItemToEndOfPipeline(GetAuthenticationPromptHook(configuration));
        }

        /// <summary>
        /// Gets the pre request hook for loading the authenticated user's details
        /// from the auth header.
        /// </summary>
        /// <param name="configuration">Basic authentication configuration to use</param>
        /// <returns>Pre request hook delegate</returns>
        private static Func<NancyContext, Response> GetCredentialRetrievalHook(BasicAuthenticationConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            return context =>
            {
                RetrieveCredentials(context, configuration);
                return null;
            };
        }

        private static Action<NancyContext> GetAuthenticationPromptHook(BasicAuthenticationConfiguration configuration)
        {
            return context =>
            {
                if (context.Response.StatusCode == HttpStatusCode.Unauthorized && SendAuthenticateResponseHeader(context, configuration))
                {
                    context.Response.Headers["WWW-Authenticate"] = String.Format("{0} realm=\"{1}\"", SCHEME, configuration.Realm);
                }
            };
        }

        private static void RetrieveCredentials(NancyContext context, BasicAuthenticationConfiguration configuration)
        {
            var credentials =
                ExtractCredentialsFromHeaders(context.Request);

            if (credentials != null && credentials.Length == 2)
            {
                var user = configuration.UserValidator.Validate(credentials[0], credentials[1]);

                if (user != null)
                {
                    context.CurrentUser = user;
                }
            }
        }

        private static string[] ExtractCredentialsFromHeaders(Request request)
        {
            var authorization =
                request.Headers.Authorization;

            if (string.IsNullOrEmpty(authorization))
            {
                return null;
            }

            if (!authorization.StartsWith(SCHEME))
            {
                return null;
            }

            try
            {
                var encodedUserPass = authorization.Substring(SCHEME.Length).Trim();
                var userPass = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUserPass));

                return String.IsNullOrWhiteSpace(userPass) ? null : userPass.Split(new[] { ':' }, 2);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static bool SendAuthenticateResponseHeader(NancyContext context, BasicAuthenticationConfiguration configuration)
        {
            return configuration.UserPromptBehaviour == UserPromptBehaviour.Always || (configuration.UserPromptBehaviour == UserPromptBehaviour.NonAjax && !context.Request.IsAjaxRequest());
        }
    }

    /// <summary>
    /// Configuration options for forms authentication
    /// </summary>
    public class BasicAuthenticationConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BasicAuthenticationConfiguration"/> class.
        /// </summary>
        /// <param name="userValidator">A valid instance of <see cref="IUserValidator"/> class</param>
        /// <param name="realm">Basic authentication realm</param>
        /// <param name="userPromptBehaviour">Control when the browser should be instructed to prompt for credentials</param>
        public BasicAuthenticationConfiguration(IUserValidator userValidator, string realm, UserPromptBehaviour userPromptBehaviour = UserPromptBehaviour.NonAjax)
        {
            if (userValidator == null)
            {
                throw new ArgumentNullException("userValidator");
            }

            if (string.IsNullOrEmpty(realm))
            {
                throw new ArgumentException("realm");
            }

            this.UserValidator = userValidator;
            this.Realm = realm;
            this.UserPromptBehaviour = userPromptBehaviour;
        }

        /// <summary>
        /// Gets the user validator
        /// </summary>
        public IUserValidator UserValidator { get; private set; }

        /// <summary>
        /// Gets the basic authentication realm
        /// </summary>
        public string Realm { get; private set; }

        /// <summary>
        /// Determines when the browser should prompt the user for credentials
        /// </summary>
        public UserPromptBehaviour UserPromptBehaviour { get; private set; }
    }
    
    /// <summary>
    /// Some simple helpers give some nice authentication syntax in the modules.
    /// </summary>
    public static class BasicHttpExtensions
    {
        /// <summary>
        /// Module requires basic authentication
        /// </summary>
        /// <param name="module">Module to enable</param>
        /// <param name="configuration">Basic authentication configuration</param>
        public static void EnableBasicAuthentication(this INancyModule module, BasicAuthenticationConfiguration configuration)
        {
            BasicAuthentication.Enable(module, configuration);
        }

        /// <summary>
        /// Module requires basic authentication
        /// </summary>
        /// <param name="pipeline">Bootstrapper to enable</param>
        /// <param name="configuration">Basic authentication configuration</param>
        public static void EnableBasicAuthentication(this IPipelines pipeline, BasicAuthenticationConfiguration configuration)
        {
            BasicAuthentication.Enable(pipeline, configuration);
        }
    }

    /// <summary>
    /// Provides a way to validate the username and password
    /// </summary>
    public interface IUserValidator
    {
        /// <summary>
        /// Validates the username and password
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>A value representing the authenticated user, null if the user was not authenticated.</returns>
        IUserIdentity Validate(string username, string password);
    }

    /// <summary>
    /// Options to control when the browser prompts the user for credentials
    /// </summary>
    public enum UserPromptBehaviour
    {
        /// <summary>
        /// Never present user with login prompt
        /// </summary>
        Never,

        /// <summary>
        /// Always present user with login prompt
        /// </summary>
        Always,

        /// <summary>
        /// Only prompt the user for credentials on non-ajax requests
        /// </summary>
        NonAjax
    }
}

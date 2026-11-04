using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Rock.Model;
using System.Net;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using com.bemaservices.MinistrySafe.Constants;
using Rock.Data;
using Rock.Security;

namespace com.bemaservices.MinistrySafe.Webhooks
{
    public class MinistrySafeMiddleware : OwinMiddleware
    {
        private enum WebhookAuthenticationStatus
        {
            NotConfigured,
            Authorized,
            Unauthorized,
            Misconfigured
        }

        public MinistrySafeMiddleware( OwinMiddleware next )
            : base( next )
        {
        }

        /// <inheritdoc/>
        public override async Task Invoke( IOwinContext context )
        {
            var path = context.Request.Uri.AbsolutePath;

            if ( !path.EndsWith( "/MinistrySafe.ashx", StringComparison.OrdinalIgnoreCase ) )
            {
                await Next.Invoke( context );
                return;
            }

            context.Response.ContentType = "text/plain";

            if ( !string.Equals( context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase ) )
            {
                context.Response.StatusCode = ( int ) HttpStatusCode.NotImplemented;
                await context.Response.WriteAsync( "Invalid request type." );
                return;
            }

            var authenticatingWebhook = true;
            try
            {
                var authenticationStatus = AuthenticateWebhook( context.Request );
                if ( authenticationStatus == WebhookAuthenticationStatus.Misconfigured )
                {
                    ExceptionLogService.LogException(
                        new InvalidOperationException( "MinistrySafe webhook authentication requires a username and password. The username must not contain a colon." ),
                        null );
                    SendAuthenticationConfigurationError( context );
                    return;
                }

                if ( authenticationStatus == WebhookAuthenticationStatus.Unauthorized )
                {
                    SendNotAuthorized( context );
                    return;
                }

                authenticatingWebhook = false;

                string postedData;
                using ( var reader = new StreamReader( context.Request.Body ) )
                {
                    postedData = await reader.ReadToEndAsync();
                }

                string responseMessage;
                if ( !MinistrySafe.SaveWebhookResults( postedData, out responseMessage ) )
                {
                    context.Response.StatusCode = ( int ) HttpStatusCode.OK; // If not OK, the website will redirect to the error screen
                    await context.Response.WriteAsync( responseMessage );
                    return;
                }

                context.Response.StatusCode = ( int ) HttpStatusCode.OK;
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex, null );
                if ( authenticatingWebhook )
                {
                    SendAuthenticationConfigurationError( context );
                }
                else
                {
                    SendBadRequest( context );
                }
            }
        }

        private static WebhookAuthenticationStatus AuthenticateWebhook( IOwinRequest request )
        {
            using ( var rockContext = new RockContext() )
            {
                var settings = MinistrySafe.GetSettings( rockContext );
                if ( settings == null )
                {
                    return WebhookAuthenticationStatus.NotConfigured;
                }

                var username = settings
                    .Where( value => value.AttributeKey == MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_WEBHOOK_USERNAME )
                    .Select( value => value.Value )
                    .FirstOrDefault();
                var encryptedPassword = settings
                    .Where( value => value.AttributeKey == MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_WEBHOOK_PASSWORD )
                    .Select( value => value.Value )
                    .FirstOrDefault();

                var hasUsername = !string.IsNullOrEmpty( username );
                var hasPassword = !string.IsNullOrEmpty( encryptedPassword );
                if ( !hasUsername && !hasPassword )
                {
                    return WebhookAuthenticationStatus.NotConfigured;
                }

                if ( !hasUsername || string.IsNullOrWhiteSpace( username ) || username.Contains( ":" ) || !hasPassword )
                {
                    return WebhookAuthenticationStatus.Misconfigured;
                }

                var password = Encryption.DecryptString( encryptedPassword );
                if ( string.IsNullOrEmpty( password ) )
                {
                    return WebhookAuthenticationStatus.Misconfigured;
                }

                return HasValidBasicCredentials( request.Headers.Get( "Authorization" ), username, password )
                    ? WebhookAuthenticationStatus.Authorized
                    : WebhookAuthenticationStatus.Unauthorized;
            }
        }

        private static bool HasValidBasicCredentials( string authorizationHeader, string expectedUsername, string expectedPassword )
        {
            AuthenticationHeaderValue authenticationHeader;
            if ( !AuthenticationHeaderValue.TryParse( authorizationHeader, out authenticationHeader ) ||
                !string.Equals( authenticationHeader.Scheme, "Basic", StringComparison.OrdinalIgnoreCase ) ||
                string.IsNullOrWhiteSpace( authenticationHeader.Parameter ) )
            {
                return false;
            }

            byte[] credentialBytes;
            try
            {
                credentialBytes = Convert.FromBase64String( authenticationHeader.Parameter );
            }
            catch ( FormatException )
            {
                return false;
            }

            string credentials;
            try
            {
                credentials = new UTF8Encoding( false, true ).GetString( credentialBytes );
            }
            catch ( DecoderFallbackException )
            {
                return false;
            }

            var separatorIndex = credentials.IndexOf( ':' );
            if ( separatorIndex < 0 )
            {
                return false;
            }

            var username = credentials.Substring( 0, separatorIndex );
            var password = credentials.Substring( separatorIndex + 1 );
            return string.Equals( username, expectedUsername, StringComparison.Ordinal ) &&
                string.Equals( password, expectedPassword, StringComparison.Ordinal );
        }

        private void SendNotAuthorized( IOwinContext context )
        {
            context.Response.StatusCode = ( int ) HttpStatusCode.Unauthorized;
            context.Response.Headers.Set( "WWW-Authenticate", "Basic realm=\"MinistrySafe\"" );
            context.Response.ReasonPhrase = "Unauthorized.";
        }

        private void SendAuthenticationConfigurationError( IOwinContext context )
        {
            context.Response.StatusCode = ( int ) HttpStatusCode.InternalServerError;
            context.Response.ReasonPhrase = "Webhook authentication is not configured correctly.";
        }

        private void SendBadRequest( IOwinContext context, string addlInfo = "" )
        {
            context.Response.StatusCode = ( int ) HttpStatusCode.BadRequest;
            context.Response.ReasonPhrase = "Request is invalid or malformed. " + addlInfo;
        }
    }
}

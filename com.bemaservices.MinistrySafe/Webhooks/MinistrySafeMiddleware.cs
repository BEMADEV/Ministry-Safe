using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Rock.Model;
using System.Net;
using System.IO;

namespace com.bemaservices.MinistrySafe.Webhooks
{
    public class MinistrySafeMiddleware : OwinMiddleware
    {
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

            try
            {
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
                ExceptionLogService.LogException( ex, System.Web.HttpContext.Current );
                SendBadRequest( context );
            }
        }

        private void SendNotAuthorized( IOwinContext context )
        {
            context.Response.StatusCode = ( int ) HttpStatusCode.Forbidden;
            context.Response.ReasonPhrase = "Not authorized to view reservation type.";
        }

        private void SendBadRequest( IOwinContext context, string addlInfo = "" )
        {
            context.Response.StatusCode = ( int ) HttpStatusCode.BadRequest;
            context.Response.ReasonPhrase = "Request is invalid or malformed. " + addlInfo;
        }
    }
}

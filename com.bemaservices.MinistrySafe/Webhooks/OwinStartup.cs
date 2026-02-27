using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Owin;
using Rock.Utility;

namespace com.bemaservices.MinistrySafe.Webhooks
{
    public class OwinStartup : IRockOwinStartup
    {
        /// <inheritdoc/>
        public int StartupOrder => 0;

        /// <inheritdoc/>
        public void OnStartup( IAppBuilder app )
        {
            app.Use( typeof( MinistrySafeMiddleware ) );
        }
    }
}

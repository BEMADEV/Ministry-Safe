// <copyright>
// Copyright by BEMA Software Services
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using com.bemaservices.MinistrySafe.Constants;
using com.bemaservices.MinistrySafe.Migrations;
using com.bemaservices.MinistrySafe.MinistrySafeApi;
using com.bemaservices.MinistrySafe.MinistrySafeApi.V2;
using com.bemaservices.MinistrySafe.MinistrySafeApi.V3;
using com.bemaservices.MinistrySafe.MinistrySafeApi.V3.BackgroundChecks;
using com.bemaservices.MinistrySafe.MinistrySafeApi.V3.Trainings;
using com.bemaservices.MinistrySafe.MinistrySafeApi.V3.Users;
using com.bemaservices.MinistrySafe.Model;
using com.bemaservices.MinistrySafe.Utility;
using Humanizer;
using Newtonsoft.Json;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.IpAddress;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace com.bemaservices.MinistrySafe
{
    /// <summary>
    /// MinistrySafe Background Check
    /// </summary>
    [Description( "MinistrySafe Background Check" )]
    [Export( typeof( BackgroundCheckComponent ) )]
    [ExportMetadata( "ComponentName", "MinistrySafe" )]

    [EncryptedTextField( "Access Token",
        Description = "MinistrySafe Access Token",
        Key = MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_ACCESS_TOKEN,
        IsRequired = true,
        DefaultValue = "",
        Order = 0,
        IsPassword = true )]
    [TextField( "MinistrySafe Server Url",
        Description = "MinistrySafe Access Token",
        Key = MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_SERVER_URL,
        IsRequired = true,
        DefaultValue = MinistrySafeConstants.MINISTRYSAFE_APISERVER,
        Order = 1 )]
    [BooleanField( "Enable Debugging?",
        Key = MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_ENABLE_DEBUGGING,
        IsRequired = true,
        DefaultBooleanValue = false,
        Order = 2
        )]

    public class MinistrySafe : BackgroundCheckComponent
    {
        /// <summary>
        /// Sends a background request to MinistrySafe.  This method is called by the BackgroundCheckRequest action's Execute
        /// method for the MinistrySafe component.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="workflow">The Workflow initiating the request.</param>
        /// <param name="personAttribute">The person attribute.</param>
        /// <param name="ssnAttribute">The SSN attribute.</param>
        /// <param name="requestTypeAttribute">The request type attribute.</param>
        /// <param name="billingCodeAttribute">The billing code attribute.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        public override bool SendRequest( RockContext rockContext, Rock.Model.Workflow workflow,
                    AttributeCache personAttribute, AttributeCache ssnAttribute, AttributeCache requestTypeAttribute,
                    AttributeCache billingCodeAttribute, out List<string> errorMessages )
        {
            errorMessages = new List<string>();
            return BackgroundCheckHelper.SendRequest(
                rockContext,
                workflow,
                personAttribute,
                ssnAttribute,
                requestTypeAttribute,
                billingCodeAttribute,
                out errorMessages );            
        }

        /// <summary>
        /// Gets the URL to the background check report.
        /// Note: Also used by GetBackgroundCheck.ashx.cs, ProcessRequest( HttpContext context )
        /// </summary>
        /// <param name="backgroundCheckId">The background check identifier.</param>
        /// <returns>System.String.</returns>
        public override string GetReportUrl( string backgroundCheckId )
        {
            var isAuthorized = this.IsAuthorized( Rock.Security.Authorization.VIEW, UserHelper.GetCurrentPerson() );

            if ( isAuthorized )
            {
                return BackgroundCheckHelper.GetReportUrl( backgroundCheckId );
            }
            else
            {
                return "Unauthorized";
            }
        }
    }
}
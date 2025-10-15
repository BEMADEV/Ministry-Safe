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
using com.bemaservices.MinistrySafe.Model;
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
        #region Private Fields

        /// <summary>
        /// The objects to use when locking our use of the workflow's attribute values and the webhook's use of them.
        /// We're using a concurrent dictionary to hold small lock objects that are based on the workflow id so
        /// we don't needlessly lock two different workflow's from being worked on at the same time.
        /// Based on https://kofoedanders.com/c-sharp-dynamic-locking/
        /// </summary>
        private static ConcurrentDictionary<int, object> _lockObjects = new ConcurrentDictionary<int, object>();

        #endregion

        #region BackgroundCheck Implementation

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

            try
            {
                // Check to make sure workflow is not null
                if ( workflow == null )
                {
                    errorMessages.Add( "The 'MinistrySafe' background check provider requires a valid workflow." );
                    UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                    UpdateWorkflowRequestMessage( workflow, rockContext, errorMessages.AsDelimited( ", " ) );
                    return true;
                }

                // Lock the workflow until we're finished saving so the webhook can't start working on it.
                var lockObject = _lockObjects.GetOrAdd( workflow.Id, new object() );
                lock ( lockObject )
                {
                    Person person;
                    int? personAliasId;
                    if ( !GetPerson( rockContext, workflow, personAttribute, out person, out personAliasId, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to get Person." );
                        UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                        UpdateWorkflowRequestMessage( workflow, rockContext, errorMessages.AsDelimited( ", " ) );
                        return true;
                    }

                    string level = null;
                    string packageCode = null;
                    string userType = null;
                    string packageName = null;
                    if ( !GetPackageName( rockContext, workflow, requestTypeAttribute, out level, out packageCode, out userType, out packageName, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to get Package." );
                        UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                        UpdateWorkflowRequestMessage( workflow, rockContext, errorMessages.AsDelimited( ", " ) );
                        return true;
                    }

                    bool? childServing = null;
                    if ( !GetChildServing( rockContext, workflow, out childServing, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to determine whether the role is Child-Serving." );
                        UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                        UpdateWorkflowRequestMessage( workflow, rockContext, errorMessages.AsDelimited( ", " ) );
                        return true;
                    }

                    bool? over13 = null;
                    if ( !GetOverThirteen( rockContext, workflow, out over13, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to determine whether the applicant is over 13." );
                        UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                        UpdateWorkflowRequestMessage( workflow, rockContext, errorMessages.AsDelimited( ", " ) );
                        return true;
                    }

                    string salaryRange = null;
                    if ( !GetSalaryRange( rockContext, workflow, out salaryRange, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to determine the Applicant's salary range." );
                        UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                        UpdateWorkflowRequestMessage( workflow, rockContext, errorMessages.AsDelimited( ", " ) );
                        return true;
                    }

                    string tagList = null;
                    if ( !GetTags( rockContext, workflow, out tagList, errorMessages ) )
                    {
                        workflow.AddLogEntry( "Unable to get Tags." );
                    }

                    string userId;
                    string directLoginUrl;
                    if ( !GetOrCreateUser( workflow, person, personAliasId.Value, userType, tagList, out userId, out directLoginUrl, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to create user." );
                        UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                        UpdateWorkflowRequestMessage( workflow, rockContext, errorMessages.AsDelimited( ", " ) );
                        return true;
                    }

                    string requestId;
                    string applicantInterfaceUrl;
                    if ( !CreateBackgroundCheck( userId, level, packageCode, userType, childServing, over13, salaryRange, out requestId, out applicantInterfaceUrl, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to create background check." );
                        UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                        UpdateWorkflowRequestMessage( workflow, rockContext, errorMessages.AsDelimited( ", " ) );
                        return true;
                    }
                    else
                    {
                        UpdateWorkflowApplicantInterfaceUrl( workflow, rockContext, applicantInterfaceUrl );
                    }

                    using ( var newRockContext = new RockContext() )
                    {
                        var backgroundCheckService = new BackgroundCheckService( newRockContext );
                        var backgroundCheck = backgroundCheckService.Queryable()
                                .Where( c =>
                                    c.WorkflowId.HasValue &&
                                    c.WorkflowId.Value == workflow.Id )
                                .FirstOrDefault();

                        if ( backgroundCheck == null )
                        {
                            backgroundCheck = new BackgroundCheck();
                            backgroundCheck.WorkflowId = workflow.Id;
                            backgroundCheckService.Add( backgroundCheck );
                        }

                        backgroundCheck.PersonAliasId = personAliasId.Value;
                        backgroundCheck.ForeignId = 4;
                        backgroundCheck.PackageName = packageName;
                        backgroundCheck.RequestDate = RockDateTime.Now;
                        backgroundCheck.RequestId = requestId;
                        newRockContext.SaveChanges();
                    }

                    UpdateWorkflowRequestStatus( workflow, rockContext, "SUCCESS" );

                    if ( workflow.IsPersisted )
                    {
                        // Make sure the AttributeValues are saved to the database immediately because the MinistrySafe WebHook
                        // (which might otherwise get called before they are saved by the workflow processing) needs to
                        // have the correct attribute values.
                        workflow.SaveAttributeValues( rockContext );
                    }

                    _lockObjects.TryRemove( workflow.Id, out _ ); // we no longer need that lock for this workflow
                }

                return true;

            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex, null );
                errorMessages.Add( ex.Message );
                UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                UpdateWorkflowRequestMessage( workflow, rockContext, errorMessages.AsDelimited( ", " ) );
                return true;
            }
        }

        /// <summary>
        /// Gets the URL to the background check report.
        /// Note: Also used by GetBackgroundCheck.ashx.cs, ProcessRequest( HttpContext context )
        /// </summary>
        /// <param name="backgroundCheckId">The background check identifier.</param>
        /// <returns>System.String.</returns>
        public override string GetReportUrl( string backgroundCheckId )
        {
            var isAuthorized = this.IsAuthorized( Rock.Security.Authorization.VIEW, this.GetCurrentPerson() );

            if ( isAuthorized )
            {
                BackgroundCheckResponse getDocumentResponse;
                List<string> errorMessages = new List<string>();

                if ( MinistrySafeApiUtility.GetBackgroundCheck( backgroundCheckId, out getDocumentResponse, errorMessages ) )
                {
                    return getDocumentResponse.ResultsUrl;
                }
                else
                {
                    LogErrors( errorMessages );
                }
                return backgroundCheckId;
            }
            else
            {
                return "Unauthorized";
            }
        }

        /// <summary>
        /// Updates the workflow, closing it if the reportStatus is blank and the recommendation is "Invitation Expired".
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="recommendation">The recommendation.</param>
        /// <param name="documentId">The document identifier.</param>
        /// <param name="reportStatus">The report status.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="personAliasId">The person alias identifier.</param>
        private static void UpdateBackgroundCheckWorkflow( int id, string recommendation, string documentId, string reportStatus, RockContext rockContext, int? personAliasId = null, string resultsUrl = null, int? interactionId = null )//, string customPackageCode = null, int? level = null, string userType = null )
        {
            // Make sure the workflow isn't locked (i.e., it's still being worked on by the 'SendRequest' method of the workflow
            // BackgroundCheckComponent) before we start working on it -- especially before we load the workflow's attributes.
            var lockObject = _lockObjects.GetOrAdd( id, new object() );
            lock ( lockObject )
            {
                var workflowService = new WorkflowService( rockContext );
                var workflow = workflowService.Get( id );
                if ( workflow != null && workflow.IsActive )
                {
                    LogMessageToDebuggingInteraction( interactionId, "Updating Workflow." );

                    workflow.LoadAttributes();
                    if ( workflow.Attributes.ContainsKey( "ReportStatus" ) )
                    {
                        if ( workflow.GetAttributeValue( "ReportStatus" ).IsNotNullOrWhiteSpace() && reportStatus.IsNullOrWhiteSpace() )
                        {
                            // Don't override current values if Webhook is older than current values
                            return;
                        }
                    }

                    if ( workflow.Attributes.ContainsKey( "Report" ) )
                    {
                        if ( workflow.GetAttributeValue( "Report" ).IsNotNullOrWhiteSpace() && documentId.IsNullOrWhiteSpace() )
                        {
                            // Don't override current values if Webhook is older than current values
                            return;
                        }
                    }


                    if ( workflow.Attributes.ContainsKey( "Person" ) )
                    {
                        if ( workflow.GetAttributeValue( "Person" ).IsNullOrWhiteSpace() && personAliasId != null )
                        {
                            var personAlias = new PersonAliasService( rockContext ).Get( personAliasId.Value );
                            if ( personAlias != null )
                            {
                                if ( SaveAttributeValue( workflow, "Person", personAlias.Guid.ToString(),
                                FieldTypeCache.Get( Rock.SystemGuid.FieldType.PERSON.AsGuid() ), rockContext ) )
                                {
                                }
                            }
                        }
                    }

                    // Save the recommendation
                    if ( !string.IsNullOrWhiteSpace( recommendation ) )
                    {
                        if ( SaveAttributeValue( workflow, "ReportRecommendation", recommendation,
                            FieldTypeCache.Get( Rock.SystemGuid.FieldType.TEXT.AsGuid() ), rockContext,
                            new Dictionary<string, string> { { "ispassword", "false" } } ) )
                        {
                        }

                        if ( reportStatus.IsNullOrWhiteSpace() && ( recommendation == "Invitation Expired" || recommendation == "Cancelled" ) )
                        {
                            workflow.CompletedDateTime = RockDateTime.Now;
                            workflow.MarkComplete( recommendation );
                        }
                    }

                    // Save the report link
                    if ( documentId.IsNotNullOrWhiteSpace() )
                    {
                        int entityTypeId = EntityTypeCache.Get( typeof( MinistrySafe ) ).Id;
                        if ( SaveAttributeValue( workflow, "Report", $"{entityTypeId},{documentId}",
                            FieldTypeCache.Get( Rock.SystemGuid.FieldType.TEXT.AsGuid() ), rockContext,
                            new Dictionary<string, string> { { "ispassword", "false" } } ) )
                        {
                        }

                        if ( workflow.Attributes.ContainsKey( "ReportFile" ) && resultsUrl.IsNotNullOrWhiteSpace() )
                        {
                            var attributeCache = workflow.Attributes["ReportFile"];
                            // Save the report
                            Guid? binaryFileGuid = null;
                            binaryFileGuid = SaveFile( attributeCache, resultsUrl, workflow.Id.ToString() + ".pdf" );
                            if ( binaryFileGuid.HasValue )
                            {
                                workflow.SetAttributeValue( attributeCache.Key, binaryFileGuid.Value.ToString() );
                            }
                        }
                    }

                    if ( !string.IsNullOrWhiteSpace( reportStatus ) )
                    {
                        // Save the status
                        if ( SaveAttributeValue( workflow, "ReportStatus", reportStatus,
                        FieldTypeCache.Get( Rock.SystemGuid.FieldType.SINGLE_SELECT.AsGuid() ), rockContext,
                        new Dictionary<string, string> { { "fieldtype", "ddl" }, { "values", "Pass,Fail,Review" } } ) )
                        {
                        }
                    }

                    //// Set the background check type if blank
                    //if ( workflow.GetAttributeValue( "SurveyType" ).IsNullOrWhiteSpace() )
                    //{
                    //    DefinedValueCache definedValue = null;
                    //    var definedType = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.BACKGROUND_CHECK_TYPES.AsGuid() );

                    //    // Match on custom package code
                    //    if ( customPackageCode.IsNotNullOrWhiteSpace() )
                    //    {
                    //        definedValue = definedType.DefinedValues.Where( dv => dv.AttributeValues.ContainsKey( "MinistrySafePackageCode" ) &&
                    //                dv.AttributeValues["MinistrySafePackageCode"].Value == customPackageCode )
                    //            .FirstOrDefault();
                    //    }

                    //    // Else match on Level and User Type
                    //    if ( definedValue == null && level.HasValue )
                    //    {
                    //        var levelString = level.ToString();
                    //        var levelMatches = definedType.DefinedValues.Where( dv => dv.AttributeValues.ContainsKey( "MinistrySafePackageLevel" ) &&
                    //                dv.AttributeValues["MinistrySafePackageLevel"].Value == levelString );
                    //        if ( userType.IsNotNullOrWhiteSpace() )
                    //        {
                    //            var userTypeDefinedType = DefinedTypeCache.Get( "559E79C6-2EAB-4A0D-A16F-59D9B63F002F".AsGuid() );
                    //            var userTypeDefinedValue = userTypeDefinedType.DefinedValues.Where( dv => dv.Value == userType ).FirstOrDefault();
                    //            if ( userTypeDefinedValue != null )
                    //            {
                    //                var userTypeGuid = userTypeDefinedValue.Guid.ToString();
                    //                definedValue = levelMatches.Where( dv => dv.AttributeValues.ContainsKey( "MinistrySafeUserType" ) &&
                    //                        dv.AttributeValues["MinistrySafeUserType"].Value == userTypeGuid )
                    //                    .FirstOrDefault();
                    //            }
                    //        }

                    //        // Fall back to first available background check type of that level
                    //        if ( definedValue == null )
                    //        {
                    //            definedValue = levelMatches.FirstOrDefault();
                    //        }
                    //    }

                    //    if ( definedValue != null )
                    //    {
                    //        SaveAttributeValue( workflow, "PackageType", definedValue.Guid.ToString(),
                    //                FieldTypeCache.Get( Rock.SystemGuid.FieldType.DEFINED_VALUE.AsGuid() ), rockContext );
                    //    }
                    //}

                    rockContext.WrapTransaction( () =>
                    {
                        rockContext.SaveChanges();
                        workflow.SaveAttributeValues( rockContext );
                        foreach ( var activity in workflow.Activities )
                        {
                            activity.SaveAttributeValues( rockContext );
                        }
                    } );
                }

                rockContext.SaveChanges();

                LogMessageToDebuggingInteraction( interactionId, "Workflow Updated. Processing now." );


                List<string> workflowErrors;
                workflowService.Process( workflow, out workflowErrors );
                _lockObjects.TryRemove( id, out _ ); // we no longer need that lock for this workflow
                LogMessageToDebuggingInteraction( interactionId, "Workflow Processing Complete." );

            }
        }

        private static Guid? SaveFile( AttributeCache binaryFileAttribute, string url, string fileName )
        {
            // get BinaryFileType info
            if ( binaryFileAttribute != null &&
                binaryFileAttribute.QualifierValues != null &&
                binaryFileAttribute.QualifierValues.ContainsKey( "binaryFileType" ) )
            {
                Guid? fileTypeGuid = binaryFileAttribute.QualifierValues["binaryFileType"].Value.AsGuidOrNull();
                if ( fileTypeGuid.HasValue )
                {
                    RockContext rockContext = new RockContext();
                    BinaryFileType binaryFileType = new BinaryFileTypeService( rockContext ).Get( fileTypeGuid.Value );

                    if ( binaryFileType != null )
                    {
                        byte[] data = null;

                        using ( WebClient wc = new WebClient() )
                        {
                            data = wc.DownloadData( url );
                        }

                        BinaryFile binaryFile = new BinaryFile();
                        binaryFile.Guid = Guid.NewGuid();
                        binaryFile.IsTemporary = true;
                        binaryFile.BinaryFileTypeId = binaryFileType.Id;
                        binaryFile.MimeType = "application/pdf";
                        binaryFile.FileName = fileName;
                        binaryFile.FileSize = data.Length;
                        binaryFile.ContentStream = new MemoryStream( data );

                        var binaryFileService = new BinaryFileService( rockContext );
                        binaryFileService.Add( binaryFile );

                        rockContext.SaveChanges();

                        return binaryFile.Guid;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Updates the background check and workflow values.
        /// </summary>
        /// <param name="backgroundCheckWebhook">The background check webhook.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        private static bool UpdateBackgroundCheckAndWorkFlow( BackgroundCheckWebhook backgroundCheckWebhook, int? interactionId = null )
        {
            try
            {
                var requestId = backgroundCheckWebhook.Id;
                var externalId = backgroundCheckWebhook.ExternalId;
                var resultsUrl = backgroundCheckWebhook.ResultsUrl;
                var userId = backgroundCheckWebhook.UserId;
                var level = backgroundCheckWebhook.Level;
                var customPackageCode = backgroundCheckWebhook.CustomBackgroundCheckPackageCode;
                var status = backgroundCheckWebhook.Status;
                var completionDate = backgroundCheckWebhook.CompleteDate.AsDateTime();
                var orderDate = backgroundCheckWebhook.OrderDate.AsDateTime();

                LogMessageToDebuggingInteraction( interactionId, "Loaded Background Check Properties from Webhook Data." );

                bool? tazworkFlagged = null;
                BackgroundCheckResponse getDocumentResponse;
                List<string> errorMessages = new List<string>();

                if ( MinistrySafeApiUtility.GetBackgroundCheck( backgroundCheckWebhook.Id, out getDocumentResponse, errorMessages ) )
                {
                    tazworkFlagged = getDocumentResponse.TazworkFlagged;
                }
                else
                {
                    LogErrors( errorMessages );
                    LogMessageToDebuggingInteraction( interactionId, "Serveral Errors have occurred. See the exception log for more details." );

                }

                LogMessageToDebuggingInteraction( interactionId, "Loaded tazworkFlagged info." );

                return UpdateBackgroundCheck( requestId, externalId, resultsUrl, userId, level, customPackageCode, status, completionDate, orderDate, tazworkFlagged, null, interactionId );
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException(
                    new Exception(
                        String.Format( "MinistrySafe Error{0}"
                        , interactionId != null ? String.Format( " on webhook data id {0}", interactionId ) : ""
                        )
                    , ex ), null );
                LogMessageToDebuggingInteraction( interactionId, "An Error has occurred. See the exception log for more details." );
                return false;
            }
        }

        /// <summary>
        /// Updates the background check.
        /// </summary>
        /// <param name="requestId">The request identifier.</param>
        /// <param name="externalId">The external identifier.</param>
        /// <param name="resultsUrl">The results URL.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="level">The level.</param>
        /// <param name="customPackageCode">The custom package code.</param>
        /// <param name="status">The status.</param>
        /// <param name="completionDate">The completion date.</param>
        /// <param name="orderDate">The order date.</param>
        /// <param name="workflowTypeCache">The workflow type cache.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private static bool UpdateBackgroundCheck( string requestId, string externalId, string resultsUrl, int? userId, int? level, string customPackageCode, string status, DateTime? completionDate, DateTime? orderDate, bool? tazworkFlagged = null, WorkflowTypeCache workflowTypeCache = null, int? interactionId = null )
        {
            try
            {
                LogMessageToDebuggingInteraction( interactionId, "Searching for PersonAliasId." );

                using ( var rockContext = new RockContext() )
                {
                    //string userType = null;
                    var backgroundCheckService = new BackgroundCheckService( rockContext );
                    var errorMessages = new List<string>();
                    if ( externalId == null )
                    {
                        externalId = FindRockPerson( userId.ToString(), rockContext, errorMessages );
                    }

                    int? personAliasId = externalId.RemoveAllNonNumericCharacters().AsIntegerOrNull();
                    LogMessageToDebuggingInteraction( interactionId, "Found PersonAliasId. Searching for Background Check." );

                    var backgroundCheck = new BackgroundCheckService( rockContext )
                        .Queryable( "PersonAlias.Person" )
                        .Where( g => ( requestId != null && g.RequestId == requestId ) || ( requestId == null && g.PersonAliasId == personAliasId ) )
                        .Where( g => g.ForeignId == 4 )
                        .OrderBy( m => m.ResponseDate.HasValue )
                        .ThenByDescending( m => m.ResponseDate )
                        .ThenByDescending( m => m.RequestDate )
                        .FirstOrDefault();

                    if ( backgroundCheck != null )
                    {
                        LogMessageToDebuggingInteraction( interactionId, String.Format( "Matched on BackgroundCheck Id {0}", backgroundCheck.Id ) );
                    }

                    if ( backgroundCheck == null )
                    {
                        LogMessageToDebuggingInteraction( interactionId, "No Matching Background Check. Creating New Record." );

                        backgroundCheck = new BackgroundCheck();
                        backgroundCheckService.Add( backgroundCheck );

                        backgroundCheck.PersonAliasId = personAliasId.Value;
                        backgroundCheck.ForeignId = 4;
                        backgroundCheck.PackageName = "";
                        backgroundCheck.RequestDate = orderDate ?? RockDateTime.Now;

                        backgroundCheck.RequestId = requestId;
                        rockContext.SaveChanges();
                        LogMessageToDebuggingInteraction( interactionId, String.Format( "New Record Created: Id {0}", backgroundCheck.Id ) );

                    }

                    LogMessageToDebuggingInteraction( interactionId, "Setting Status." );

                    backgroundCheck.Status = status;
                    if ( backgroundCheck.Status.IsNullOrWhiteSpace() )
                    {
                        backgroundCheck.Status = "consider";
                    }

                    if ( tazworkFlagged != null && tazworkFlagged != true )
                    {
                        backgroundCheck.Status = "clear";
                    }

                    LogMessageToDebuggingInteraction( interactionId, "Status Set. Updating Response Info" );


                    backgroundCheck.ResponseId = requestId;
                    backgroundCheck.ResponseDate = completionDate ?? ( orderDate ?? RockDateTime.Now );
                    if ( resultsUrl.IsNotNullOrWhiteSpace() )
                    {
                        backgroundCheck.ResponseData = resultsUrl;
                    }

                    LogMessageToDebuggingInteraction( interactionId, "Response Info Updated. Saving Changes." );

                    rockContext.SaveChanges();

                    LogMessageToDebuggingInteraction( interactionId, "Changes Saved. Setting Recommendation and Report Status." );

                    string recommendation = null;
                    string reportStatus = null; //Pass,Fail,Review
                    switch ( backgroundCheck.Status )
                    {
                        case "pending":
                            recommendation = "Report Pending";
                            break;
                        case "clear":
                            recommendation = "Candidate Pass";
                            reportStatus = "Pass";
                            break;
                        case "consider":
                            recommendation = "Candidate Review";
                            reportStatus = "Review";
                            break;
                        case "ready":
                            recommendation = "Candidate Review";
                            reportStatus = "Review";
                            break;
                        case "suspended":
                            recommendation = "Report Suspended";
                            break;
                        case "dispute":
                            recommendation = "Report Disputed";
                            break;
                        case "InvitationCreated":
                            recommendation = "Invitation Sent";
                            break;
                        case "InvitationCompleted":
                            recommendation = "Invitation Completed";
                            break;
                        case "InvitationExpired":
                            recommendation = "Invitation Expired";
                            break;
                        case "awaiting_applicant":
                            recommendation = "Awaiting Applicant";
                            break;
                        case "complete":
                            recommendation = "Candidate Review";
                            reportStatus = "Review";
                            break;
                        case "cancelled":
                            recommendation = "Cancelled";
                            break;
                    }

                    LogMessageToDebuggingInteraction( interactionId,
                        String.Format(
                            "Recommendation set to {0}. Report Status set to {1}. Grabbing Workflow."
                            , recommendation
                            , reportStatus
                            )
                        );


                    var workflowService = new WorkflowService( rockContext );
                    Rock.Model.Workflow workflow = null;
                    if ( backgroundCheck.WorkflowId.HasValue )
                    {
                        workflow = workflowService.Get( backgroundCheck.WorkflowId.Value );
                    }

                    if ( workflow != null )
                    {
                        LogMessageToDebuggingInteraction( interactionId, String.Format( "Found Matching Workflow Id: {0}.", workflow.Id ) );
                    }

                    if ( workflow == null && workflowTypeCache != null )
                    {
                        // Add Workflow
                        var personAlias = new PersonAliasService( rockContext ).Get( personAliasId.Value );
                        workflow = Rock.Model.Workflow.Activate( workflowTypeCache, personAlias.Person.FullName );
                        workflowService.Add( workflow );
                        rockContext.SaveChanges();
                        backgroundCheck.WorkflowId = workflow.Id;
                        LogMessageToDebuggingInteraction( interactionId, String.Format( "Created New Workflow Id: {0}.", workflow.Id ) );

                    }

                    rockContext.SaveChanges();

                    LogMessageToDebuggingInteraction( interactionId, "Background Check Update Complete." );


                    if ( backgroundCheck.WorkflowId.HasValue && backgroundCheck.WorkflowId > 0 )
                    {
                        LogMessageToDebuggingInteraction( interactionId, "Launching UpdateBackgroundCheckWorkflow method." );

                        UpdateBackgroundCheckWorkflow( backgroundCheck.WorkflowId.Value, recommendation, backgroundCheck.ResponseId, reportStatus, rockContext, backgroundCheck.PersonAliasId, resultsUrl, interactionId );//, customPackageCode, level, userType );
                    }
                }

                return true;
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException(
                    new Exception(
                        String.Format( "MinistrySafe Error{0}"
                        , interactionId != null ? String.Format( " on webhook data id {0}", interactionId ) : ""
                        )
                    , ex ), null );
                LogMessageToDebuggingInteraction( interactionId, "An Error has occurred. See the exception log for more details." );
                return false;
            }
        }

        /// <summary>
        /// Sets the workflow RequestStatus attribute.
        /// </summary>
        /// <param name="workflow">The workflow.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="requestStatus">The request status.</param>
        private void UpdateWorkflowApplicantInterfaceUrl( Rock.Model.Workflow workflow, RockContext rockContext, string applicantInterfaceUrl )
        {
            if ( SaveAttributeValue( workflow, "ApplicantInterfaceUrl", applicantInterfaceUrl,
                FieldTypeCache.Get( Rock.SystemGuid.FieldType.TEXT.AsGuid() ), rockContext, null ) )
            {
                rockContext.SaveChanges();
            }
        }

        /// <summary>
        /// Sets the workflow RequestStatus attribute.
        /// </summary>
        /// <param name="workflow">The workflow.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="requestStatus">The request status.</param>
        private void UpdateWorkflowRequestStatus( Rock.Model.Workflow workflow, RockContext rockContext, string requestStatus )
        {
            if ( SaveAttributeValue( workflow, "RequestStatus", requestStatus,
                FieldTypeCache.Get( Rock.SystemGuid.FieldType.TEXT.AsGuid() ), rockContext, null ) )
            {
                rockContext.SaveChanges();
            }
        }

        /// <summary>
        /// Sets the workflow RequestMessage attribute.
        /// </summary>
        /// <param name="workflow">The workflow.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="requestStatus">The request message.</param>
        private void UpdateWorkflowRequestMessage( Rock.Model.Workflow workflow, RockContext rockContext, string requestMessage )
        {
            if ( SaveAttributeValue( workflow, "RequestMessage", requestMessage,
                FieldTypeCache.Get( Rock.SystemGuid.FieldType.TEXT.AsGuid() ), rockContext, null ) )
            {
                rockContext.SaveChanges();
            }
        }

        /// <summary>
        /// Get the MinistrySafe packages and update the list on the server.
        /// </summary>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        public static bool UpdatePackages( List<string> errorMessages )
        {
            List<PackageResponse> customPackageResponseList;

            if ( !MinistrySafeApiUtility.GetPackages( out customPackageResponseList, errorMessages ) )
            {
                //return false;
            }

            if ( customPackageResponseList == null )
            {
                customPackageResponseList = new List<PackageResponse>();
            }

            var defaultPackageResponseList = new List<PackageResponse>();
            var packageResponseList = new List<PackageResponse>();

            for ( var level = 1; level <= 7; level++ )
            {
                var packageResponse = new PackageResponse();
                packageResponse.Name = String.Format( "Search Level {0}", level.ToWords().ToUpper() );
                packageResponse.Level = level;
                defaultPackageResponseList.Add( packageResponse );
            }

            Dictionary<string, DefinedValue> packages;
            using ( var rockContext = new RockContext() )
            {
                var definedType = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.BACKGROUND_CHECK_TYPES.AsGuid() );

                DefinedValueService definedValueService = new DefinedValueService( rockContext );
                packages = definedValueService
                    .GetByDefinedTypeGuid( Rock.SystemGuid.DefinedType.BACKGROUND_CHECK_TYPES.AsGuid() )
                    .Where( v => v.ForeignId == 4 )
                    .ToList()
                    .Select( v => { v.LoadAttributes( rockContext ); return v; } ) // v => v.Value.Substring( MinistrySafeConstants.TYPENAME_PREFIX.Length ) )
                    .GroupBy( v => v.GetAttributeValue( "MinistrySafePackageName" ).ToString() )
                    .ToDictionary( v => v.Key, v => v.First() );

                var userTypes = definedValueService
                     .GetByDefinedTypeGuid( "559E79C6-2EAB-4A0D-A16F-59D9B63F002F".AsGuid() )
                     .ToList();

                foreach ( var packageResponse in customPackageResponseList )
                {
                    string packageName = packageResponse.Name;
                    if ( !packages.ContainsKey( packageName ) )
                    {
                        AddPackage( rockContext, definedType, definedValueService, packageResponse, null );
                    }

                    packageResponseList.Add( packageResponse );
                }

                foreach ( var packageResponse in defaultPackageResponseList )
                {
                    string packageName = packageResponse.Name;
                    if ( !packages.ContainsKey( packageName ) )
                    {
                        foreach ( var userType in userTypes )
                        {
                            AddPackage( rockContext, definedType, definedValueService, packageResponse, userType );
                        }
                    }

                    packageResponseList.Add( packageResponse );
                }

                var packageRestResponseNames = packageResponseList.Select( pr => pr.Name );
                foreach ( var package in packages )
                {
                    package.Value.IsActive = packageRestResponseNames.Contains( package.Key );
                }

                rockContext.SaveChanges();
            }

            DefinedValueCache.Clear();
            return true;
        }

        /// <summary>
        /// Adds the package.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="definedType">Type of the defined.</param>
        /// <param name="definedValueService">The defined value service.</param>
        /// <param name="packageResponse">The package response.</param>
        /// <param name="userType">Type of the user.</param>
        private static void AddPackage( RockContext rockContext, DefinedTypeCache definedType, DefinedValueService definedValueService, PackageResponse packageResponse, DefinedValue userType = null )
        {
            DefinedValue definedValue = null;

            definedValue = new DefinedValue()
            {
                IsActive = true,
                DefinedTypeId = definedType.Id,
                ForeignId = 4,
                Value = string.Format( "{0}{1} {2}", MinistrySafeConstants.MINISTRYSAFE_TYPENAME_PREFIX, userType != null ? userType.Description : "", packageResponse.Name.Replace( '_', ' ' ) )
            };

            definedValueService.Add( definedValue );

            rockContext.SaveChanges();

            definedValue.LoadAttributes( rockContext );

            definedValue.SetAttributeValue( "MinistrySafePackageName", packageResponse.Name );
            definedValue.SetAttributeValue( "MinistrySafePackageLevel", packageResponse.Level );
            definedValue.SetAttributeValue( "MinistrySafePackageCode", packageResponse.Code );
            definedValue.SetAttributeValue( "MinistrySafePackagePrice", packageResponse.Price );

            if ( userType != null )
            {
                definedValue.SetAttributeValue( "MinistrySafeUserType", userType.Guid.ToString() );
            }
            definedValue.SaveAttributeValues( rockContext );
        }

        /// <summary>
        /// Get the background check type that the request is for.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="workflow">The Workflow initiating the request.</param>
        /// <param name="requestTypeAttribute">The request type attribute.</param>
        /// <param name="level">The level.</param>
        /// <param name="packageCode">The package code.</param>
        /// <param name="userType">Type of the user.</param>
        /// <param name="packageName">Name of the package.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        private bool GetPackageName( RockContext rockContext, Rock.Model.Workflow workflow, AttributeCache requestTypeAttribute, out string level, out string packageCode, out string userType, out string packageName, List<string> errorMessages )
        {
            level = null;
            packageCode = null;
            userType = null;
            packageName = null;
            if ( requestTypeAttribute == null )
            {
                errorMessages.Add( "The 'MinistrySafe' background check provider requires a background check type." );
                return false;
            }

            DefinedValueCache pkgTypeDefinedValue = DefinedValueCache.Get( workflow.GetAttributeValue( requestTypeAttribute.Key ).AsGuid() );
            if ( pkgTypeDefinedValue == null )
            {
                errorMessages.Add( "The 'MinistrySafe' background check provider couldn't load background check type." );
                return false;
            }

            if ( pkgTypeDefinedValue.Attributes == null )
            {
                // shouldn't happen since pkgTypeDefinedValue is a ModelCache<,> type 
                return false;
            }

            string rawUserType = null;
            DefinedValueCache userTypeDefinedValue = DefinedValueCache.Get( pkgTypeDefinedValue.GetAttributeValue( "MinistrySafeUserType" ).AsGuid() );
            if ( userTypeDefinedValue != null )
            {
                rawUserType = userTypeDefinedValue.Value;
            }

            var formattedUserType = rawUserType.ToLower().Trim();
            if ( formattedUserType == "employee" || formattedUserType == "volunteer" )
            {
                userType = formattedUserType;
            }
            else if ( pkgTypeDefinedValue.Value.ToLower().Contains( "employee" ) )
            {
                userType = "employee";
            }
            else if ( pkgTypeDefinedValue.Value.ToLower().Contains( "volunteer" ) )
            {
                userType = "volunteer";
            }

            level = pkgTypeDefinedValue.GetAttributeValue( "MinistrySafePackageLevel" );
            packageCode = pkgTypeDefinedValue.GetAttributeValue( "MinistrySafePackageCode" );
            packageName = pkgTypeDefinedValue.Value;
            return true;
        }

        /// <summary>
        /// Updates the tags.
        /// </summary>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool UpdateTags( List<string> errorMessages )
        {
            List<TagResponse> tagResponseList;

            if ( !MinistrySafeApiUtility.GetTags( out tagResponseList, errorMessages ) )
            {
                //return false;
            }

            if ( tagResponseList == null )
            {
                tagResponseList = new List<TagResponse>();
            }

            List<DefinedValue> tags;
            using ( var rockContext = new RockContext() )
            {
                var definedType = DefinedTypeCache.Get( MinistrySafeSystemGuid.MINISTRYSAFE_TAGS.AsGuid() );

                DefinedValueService definedValueService = new DefinedValueService( rockContext );
                tags = definedValueService
                    .GetByDefinedTypeGuid( definedType.Guid )
                    .Where( v => v.ForeignId == 4 )
                    .ToList();
                var tagNames = tags.Select( dv => dv.Value ).ToList();

                foreach ( var tagResponse in tagResponseList )
                {
                    string tagName = tagResponse.Name;
                    if ( !tagNames.Contains( tagName ) )
                    {

                        DefinedValue definedValue = null;

                        definedValue = new DefinedValue()
                        {
                            IsActive = true,
                            DefinedTypeId = definedType.Id,
                            ForeignId = 4,
                            Value = tagName
                        };

                        definedValueService.Add( definedValue );

                        rockContext.SaveChanges();
                    }
                }

                var packageRestResponseNames = tagResponseList.Select( pr => pr.Name );
                foreach ( var tag in tags )
                {
                    tag.IsActive = packageRestResponseNames.Contains( tag.Value );
                }

                rockContext.SaveChanges();
            }

            DefinedValueCache.Clear();
            return true;
        }

        /// <summary>
        /// Updates the survey types.
        /// </summary>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool UpdateSurveyTypes( List<string> errorMessages )
        {
            List<string> surveyTypeResponseList;

            if ( !MinistrySafeApiUtility.GetSurveyTypes( out surveyTypeResponseList, errorMessages ) )
            {
                //return false;
            }

            if ( surveyTypeResponseList == null )
            {
                surveyTypeResponseList = new List<string>();
            }

            List<DefinedValue> surveyTypes;
            using ( var rockContext = new RockContext() )
            {
                var definedType = DefinedTypeCache.Get( MinistrySafeSystemGuid.MINISTRYSAFE_SURVEY_TYPES.AsGuid() );

                DefinedValueService definedValueService = new DefinedValueService( rockContext );
                surveyTypes = definedValueService
                    .GetByDefinedTypeGuid( definedType.Guid )
                    //.Where( v => v.ForeignId == 4 )
                    .ToList();
                var surveyTypeNames = surveyTypes.Select( dv => dv.Value ).ToList();

                foreach ( var surveyTypeResponse in surveyTypeResponseList )
                {
                    if ( !surveyTypeNames.Contains( surveyTypeResponse ) )
                    {
                        DefinedValue definedValue = null;

                        definedValue = new DefinedValue()
                        {
                            IsActive = true,
                            DefinedTypeId = definedType.Id,
                            ForeignId = 4,
                            Value = surveyTypeResponse,
                            Description = surveyTypeResponse
                        };

                        definedValueService.Add( definedValue );

                        rockContext.SaveChanges();
                    }
                }

                foreach ( var surveyType in surveyTypes )
                {
                    if ( surveyTypeResponseList.Contains( surveyType.Value ) )
                    {
                        surveyType.IsActive = true;
                        surveyType.ForeignId = 4;
                    }
                    else
                    {
                        surveyType.IsActive = false;
                    }
                }

                rockContext.SaveChanges();
            }

            DefinedValueCache.Clear();
            return true;
        }

        /// <summary>
        /// Gets the child serving.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="workflow">The workflow.</param>
        /// <param name="childServing">if set to <c>true</c> [child serving].</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool GetChildServing( RockContext rockContext, Rock.Model.Workflow workflow, out bool? childServing, List<string> errorMessages )
        {
            childServing = null;

            if ( !workflow.Attributes.ContainsKey( "ChildServing" ) )
            {
                errorMessages.Add( "The 'MinistrySafe' background check provider couldn't find the 'Child Serving' attribute." );
                return false;
            }

            childServing = workflow.GetAttributeValue( "ChildServing" ).AsBooleanOrNull();

            return true;
        }

        /// <summary>
        /// Gets the over thirteen.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="workflow">The workflow.</param>
        /// <param name="over13">if set to <c>true</c> [over13].</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool GetOverThirteen( RockContext rockContext, Rock.Model.Workflow workflow, out bool? over13, List<string> errorMessages )
        {
            over13 = null;

            if ( !workflow.Attributes.ContainsKey( "AgeOver13" ) )
            {
                errorMessages.Add( "The 'MinistrySafe' background check provider couldn't find the 'Age Over 13' attribute." );
                return false;
            }

            over13 = workflow.GetAttributeValue( "AgeOver13" ).AsBooleanOrNull();

            return true;
        }

        /// <summary>
        /// Gets the salary range.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="workflow">The workflow.</param>
        /// <param name="salaryRange">The salary range.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool GetSalaryRange( RockContext rockContext, Rock.Model.Workflow workflow, out string salaryRange, List<string> errorMessages )
        {
            salaryRange = null;

            if ( !workflow.Attributes.ContainsKey( "SalaryRange" ) )
            {
                errorMessages.Add( "The 'MinistrySafe' background check provider couldn't find the 'Salary Range' attribute." );
                return false;
            }

            salaryRange = workflow.GetAttributeValue( "SalaryRange" );
            return true;
        }
        /// <summary>
        /// Gets the type of the employee.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="workflow">The workflow.</param>
        /// <param name="employeeType">Type of the employee.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool GetEmployeeType( RockContext rockContext, Rock.Model.Workflow workflow, out string employeeType, List<string> errorMessages )
        {
            employeeType = null;

            if ( !workflow.Attributes.ContainsKey( "EmployeeType" ) )
            {
                errorMessages.Add( "The 'MinistrySafe' background check provider couldn't find the 'Salary Range' attribute." );
                return false;
            }

            employeeType = workflow.GetAttributeValue( "EmployeeType" );


            return true;
        }

        /// <summary>
        /// Creates the invitation.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="level">The level.</param>
        /// <param name="packageCode">The package code.</param>
        /// <param name="userType">Type of the user.</param>
        /// <param name="childServing">if set to <c>true</c> [child serving].</param>
        /// <param name="over13">if set to <c>true</c> [over13].</param>
        /// <param name="salaryRange">The salary range.</param>
        /// <param name="requestId">The request identifier.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        public static bool CreateBackgroundCheck( string userId, string level, string packageCode, string userType, bool? childServing, bool? over13, string salaryRange, out string requestId, out string applicantInterfaceUrl, List<string> errorMessages )
        {
            requestId = null;
            applicantInterfaceUrl = null;
            BackgroundCheckResponse backgroundCheckResponse;
            if ( MinistrySafeApiUtility.CreateBackgroundCheck( userId, level, packageCode, userType, childServing, over13, salaryRange, out backgroundCheckResponse, errorMessages ) )
            {
                userId = backgroundCheckResponse.UserId.ToString();
                requestId = backgroundCheckResponse.Id;
                applicantInterfaceUrl = backgroundCheckResponse.ApplicantInterfaceUrl;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Imports the background checks.
        /// </summary>
        /// <param name="dateRange">The date range.</param>
        /// <param name="workflowType">Type of the workflow.</param>
        /// <param name="backgroundChecksProcessed">The background checks processed.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal bool ImportBackgroundChecks( DateRange dateRange, WorkflowTypeCache workflowType, out int backgroundChecksProcessed, out List<string> errorMessages )
        {
            var startDate = dateRange.Start;
            var endDate = dateRange.End;
            backgroundChecksProcessed = 0;
            int pageNumber = 1;
            errorMessages = new List<string>();
            List<BackgroundCheckResponse> getAllBackgroundCheckResponses;

            // Save Interaction storing information
            var errorMessage = string.Empty;
            int? interactionId = CreateDebuggingInteraction( "Background Check Import", out errorMessage );
            if ( errorMessage.IsNotNullOrWhiteSpace() )
            {
                errorMessages.Add( errorMessage );
                return false;
            }

            if ( MinistrySafeApiUtility.GetAllBackgroundChecks( pageNumber, startDate, endDate, out getAllBackgroundCheckResponses, errorMessages ) )
            {
                LogMessageToDebuggingInteraction(
                    interactionId,
                    String.Format(
                        "Pulled Page {0} of background checks from {1} to {2}",
                        pageNumber,
                        startDate,
                        endDate
                        )
                    );

                LogMessageToDebuggingInteraction(
                    interactionId,
                    String.Format(
                        "Received Api Data </br> {0}</br></br>",
                        getAllBackgroundCheckResponses.ToJson()
                        )
                    );

                while ( getAllBackgroundCheckResponses.Any() )
                {
                    // Loop through trainings
                    foreach ( var getAllBackgroundCheckResponse in getAllBackgroundCheckResponses )
                    {
                        LogMessageToDebuggingInteraction(
                            interactionId,
                            String.Format(
                                "Processing Background Check for id:{0}",
                                getAllBackgroundCheckResponse.Id
                                )
                            );

                        var requestId = getAllBackgroundCheckResponse.Id;
                        var resultsUrl = getAllBackgroundCheckResponse.ResultsUrl;
                        var userId = getAllBackgroundCheckResponse.UserId;
                        var level = getAllBackgroundCheckResponse.Level;
                        var customPackageCode = getAllBackgroundCheckResponse.CustomBackgroundCheckPackageCode;
                        var status = getAllBackgroundCheckResponse.Status;
                        var completionDate = getAllBackgroundCheckResponse.CompleteDate.AsDateTime();
                        var orderDate = getAllBackgroundCheckResponse.OrderDate.AsDateTime();
                        var tazworkFlagged = getAllBackgroundCheckResponse.TazworkFlagged;
                        if ( completionDate.HasValue || getAllBackgroundCheckResponse.Status == "complete" )
                        {
                            if ( UpdateBackgroundCheck( requestId, null, resultsUrl, userId, level, customPackageCode, status, completionDate, orderDate, tazworkFlagged, workflowType ) )
                            {
                                backgroundChecksProcessed++;
                            }
                            else
                            {
                                errorMessages.Add( String.Format( "Error updating background check for id:{0}", getAllBackgroundCheckResponse.Id ) );
                            }
                        }
                    }

                    // Get New Trainings
                    pageNumber++;
                    if ( !MinistrySafeApiUtility.GetAllBackgroundChecks( pageNumber, startDate, endDate, out getAllBackgroundCheckResponses, errorMessages ) )
                    {
                        return false;
                    }

                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Archives the linked background checks.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="workflow">The workflow.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal bool ArchiveLinkedBackgroundChecks( RockContext rockContext, Rock.Model.Workflow workflow, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            var backgroundCheckService = new BackgroundCheckService( rockContext );
            var backgroundChecks = backgroundCheckService.Queryable().Where( bc => bc.WorkflowId == workflow.Id ).ToList();
            foreach ( BackgroundCheck backgroundCheck in backgroundChecks )
            {
                var backgroundCheckErrorMessages = new List<string>();
                BackgroundCheckResponse backgroundCheckResponse = null;

                if ( !MinistrySafeApiUtility.ArchiveBackgroundCheck( backgroundCheck.RequestId, out backgroundCheckResponse, backgroundCheckErrorMessages ) )
                {
                    errorMessages.Add( String.Format( "Error archiving BackgroundCheck with RockId:{0} and MinistrySafeId:{1}"
                        , backgroundCheck.Id
                        , backgroundCheck.RequestId ) );
                    errorMessages.AddRange( backgroundCheckErrorMessages );
                    return false;
                }

                var requestId = backgroundCheckResponse.Id;
                var resultsUrl = backgroundCheckResponse.ResultsUrl;
                var completionDate = backgroundCheckResponse.CompleteDate.AsDateTime();
                var orderDate = backgroundCheckResponse.OrderDate.AsDateTime();


                backgroundCheck.Status = "archived";
                backgroundCheck.ResponseId = requestId;
                backgroundCheck.ResponseDate = RockDateTime.Now;
                if ( resultsUrl.IsNotNullOrWhiteSpace() )
                {
                    backgroundCheck.ResponseData = resultsUrl;
                }

                rockContext.SaveChanges();
            }

            return true;
        }

        #endregion

        #region Training Implementation

        /// <summary>
        /// Sends the training.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="workflow">The workflow.</param>
        /// <param name="personAttribute">The person attribute.</param>
        /// <param name="userTypeAttribute">The user type attribute.</param>
        /// <param name="surveyTypeAttribute">The survey type attribute.</param>
        /// <param name="directLoginUrlAttribute">The direct login URL attribute.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool SendTraining( RockContext rockContext, Rock.Model.Workflow workflow,
                 AttributeCache personAttribute, AttributeCache userTypeAttribute, AttributeCache surveyTypeAttribute, AttributeCache directLoginUrlAttribute,
                 out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            try
            {
                // Check to make sure workflow is not null
                if ( workflow == null )
                {
                    errorMessages.Add( "The 'MinistrySafe' provider requires a valid workflow." );
                    UpdateWorkflowTrainingStatus( workflow, rockContext, "FAIL" );
                    return true;
                }

                // Lock the workflow until we're finished saving so the webhook can't start working on it.
                var lockObject = _lockObjects.GetOrAdd( workflow.Id, new object() );
                lock ( lockObject )
                {
                    Person person;
                    int? personAliasId;
                    if ( !GetPerson( rockContext, workflow, personAttribute, out person, out personAliasId, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to get Person." );
                        UpdateWorkflowTrainingStatus( workflow, rockContext, "FAIL" );
                        return true;
                    }

                    string surveyTypeName;
                    if ( !GetSurveyTypeName( rockContext, workflow, surveyTypeAttribute, out surveyTypeName, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to get Survey Type." );
                        UpdateWorkflowTrainingStatus( workflow, rockContext, "FAIL" );
                        return true;
                    }

                    string userTypeName;
                    if ( !GetUserTypeName( rockContext, workflow, userTypeAttribute, out userTypeName, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to get User Type." );
                        UpdateWorkflowTrainingStatus( workflow, rockContext, "FAIL" );
                        return true;
                    }

                    string tagList = null;
                    if ( !GetTags( rockContext, workflow, out tagList, errorMessages ) )
                    {
                        workflow.AddLogEntry( "Unable to get Tags." );
                    }

                    string userId;
                    string directLoginUrl;
                    if ( !GetOrCreateUser( workflow, person, personAliasId.Value, userTypeName, tagList, out userId, out directLoginUrl, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to create user." );
                        UpdateWorkflowTrainingStatus( workflow, rockContext, "FAIL" );
                        return true;
                    }

                    if ( !AssignTraining( userId, surveyTypeName, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to assign training." );
                        UpdateWorkflowTrainingStatus( workflow, rockContext, "FAIL" );
                        return true;
                    }

                    using ( var newRockContext = new RockContext() )
                    {
                        var ministrySafeUserService = new MinistrySafeUserService( newRockContext );
                        var ministrySafeUser = ministrySafeUserService.Queryable()
                                .Where( c =>
                                    c.WorkflowId.HasValue &&
                                    c.WorkflowId.Value == workflow.Id )
                                .FirstOrDefault();

                        if ( ministrySafeUser == null )
                        {
                            ministrySafeUser = new MinistrySafeUser();
                            ministrySafeUser.WorkflowId = workflow.Id;
                            ministrySafeUserService.Add( ministrySafeUser );
                        }

                        ministrySafeUser.PersonAliasId = personAliasId.Value;
                        ministrySafeUser.ForeignId = 4;
                        ministrySafeUser.SurveyCode = surveyTypeName;
                        ministrySafeUser.UserType = userTypeName;
                        ministrySafeUser.RequestDate = RockDateTime.Now;
                        ministrySafeUser.DirectLoginUrl = directLoginUrl;
                        ministrySafeUser.UserId = userId.AsInteger();
                        newRockContext.SaveChanges();
                    }

                    UpdateWorkflowTrainingStatus( workflow, rockContext, "SUCCESS" );

                    if ( SaveAttributeValue( workflow, directLoginUrlAttribute.Key, directLoginUrl,
                        FieldTypeCache.Get( Rock.SystemGuid.FieldType.TEXT.AsGuid() ), rockContext, null ) )
                    {
                        rockContext.SaveChanges();
                    }

                    if ( workflow.IsPersisted )
                    {
                        // Make sure the AttributeValues are saved to the database immediately because the MinistrySafe WebHook
                        // (which might otherwise get called before they are saved by the workflow processing) needs to
                        // have the correct attribute values.
                        workflow.SaveAttributeValues( rockContext );
                    }

                    _lockObjects.TryRemove( workflow.Id, out _ ); // we no longer need that lock for this workflow
                }

                return true;

            }
            catch ( Exception ex )
            {
                Rock.Model.ExceptionLogService.LogException( ex, null );
                errorMessages.Add( ex.Message );
                UpdateWorkflowTrainingStatus( workflow, rockContext, "FAIL" );
                return true;
            }
        }

        /// <summary>
        /// Updates the workflow, closing it if the reportStatus is blank and the recommendation is "Invitation Expired".
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="score">The score.</param>
        /// <param name="completedDateTime">The completed date time.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="surveyCode">The survey code.</param>
        /// <param name="personAliasId">The person alias identifier.</param>
        private static void UpdateTrainingWorkflow( int id, int? score, DateTime completedDateTime, RockContext rockContext, string surveyCode = null, int? personAliasId = null, int? interactionId = null )
        {
            // Make sure the workflow isn't locked (i.e., it's still being worked on by the 'SendRequest' method of the workflow
            // BackgroundCheckComponent) before we start working on it -- especially before we load the workflow's attributes.
            var lockObject = _lockObjects.GetOrAdd( id, new object() );
            lock ( lockObject )
            {
                var workflowService = new WorkflowService( rockContext );
                var workflow = workflowService.Get( id );
                if ( workflow != null && workflow.IsActive )
                {
                    LogMessageToDebuggingInteraction( interactionId, "Updating Workflow." );

                    workflow.LoadAttributes();
                    if ( workflow.Attributes.ContainsKey( "Person" ) )
                    {
                        if ( workflow.GetAttributeValue( "Person" ).IsNullOrWhiteSpace() && personAliasId != null )
                        {
                            var personAlias = new PersonAliasService( rockContext ).Get( personAliasId.Value );
                            if ( personAlias != null )
                            {
                                if ( SaveAttributeValue( workflow, "Person", personAlias.Guid.ToString(),
                                FieldTypeCache.Get( Rock.SystemGuid.FieldType.PERSON.AsGuid() ), rockContext ) )
                                {
                                }
                            }
                        }
                    }

                    if ( workflow.Attributes.ContainsKey( "TrainingScore" ) )
                    {
                        if ( workflow.GetAttributeValue( "TrainingScore" ).IsNotNullOrWhiteSpace() && score == null )
                        {
                            // Don't override current values if Webhook is older than current values
                            return;
                        }
                    }

                    if ( workflow.Attributes.ContainsKey( "TrainingDate" ) )
                    {
                        if ( workflow.GetAttributeValue( "TrainingDate" ).IsNotNullOrWhiteSpace() && completedDateTime == null )
                        {
                            // Don't override current values if Webhook is older than current values
                            return;
                        }
                    }

                    // Save the score
                    if ( score != null )
                    {
                        if ( SaveAttributeValue( workflow, "TrainingScore", score.ToString(),
                            FieldTypeCache.Get( Rock.SystemGuid.FieldType.INTEGER.AsGuid() ), rockContext ) )
                        {
                        }
                    }

                    if ( completedDateTime != null )
                    {
                        // Save the training date
                        if ( SaveAttributeValue( workflow, "TrainingDate", completedDateTime.ToString(),
                        FieldTypeCache.Get( Rock.SystemGuid.FieldType.SINGLE_SELECT.AsGuid() ), rockContext ) )
                        {
                        }
                    }

                    // Set the training type if blank
                    if ( workflow.GetAttributeValue( "SurveyType" ).IsNullOrWhiteSpace() && surveyCode.IsNotNullOrWhiteSpace() )
                    {
                        var definedType = DefinedTypeCache.Get( "95EF81D2-C192-4B9E-A7A3-5E1E90BDA3CE".AsGuid() );
                        foreach ( var rockPackage in definedType.DefinedValues )
                        {
                            if ( rockPackage.Value == surveyCode )
                            {
                                SaveAttributeValue( workflow, "SurveyType", rockPackage.Guid.ToString(),
                                    FieldTypeCache.Get( Rock.SystemGuid.FieldType.DEFINED_VALUE.AsGuid() ), rockContext );
                            }
                        }
                    }

                    rockContext.WrapTransaction( () =>
                    {
                        rockContext.SaveChanges();
                        workflow.SaveAttributeValues( rockContext );
                        foreach ( var activity in workflow.Activities )
                        {
                            activity.SaveAttributeValues( rockContext );
                        }
                    } );
                }

                rockContext.SaveChanges();

                LogMessageToDebuggingInteraction( interactionId, "Workflow Updated. Processing now." );

                List<string> workflowErrors;
                workflowService.Process( workflow, out workflowErrors );
                _lockObjects.TryRemove( id, out _ ); // we no longer need that lock for this workflow
                LogMessageToDebuggingInteraction( interactionId, "Workflow Processing Complete." );
            }
        }

        /// <summary>
        /// Imports the trainings.
        /// </summary>
        /// <param name="dateRange">The date range.</param>
        /// <param name="workflowType">Type of the workflow.</param>
        /// <param name="trainingsProcessed">The trainings processed.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal bool ImportTrainings( DateRange dateRange, WorkflowTypeCache workflowType, out int trainingsProcessed, out List<string> errorMessages )
        {
            var startDate = dateRange.Start;
            var endDate = dateRange.End;
            trainingsProcessed = 0;
            int pageNumber = 1;
            errorMessages = new List<string>();
            List<GetAllTrainingResponse> getAllTrainingResponses;

            // Save Interaction storing information
            var errorMessage = string.Empty;
            int? interactionId = CreateDebuggingInteraction( "Training Import", out errorMessage );
            if ( errorMessage.IsNotNullOrWhiteSpace() )
            {
                errorMessages.Add( errorMessage );
                return false;
            }

            if ( MinistrySafeApiUtility.GetAllTrainings( pageNumber, startDate, endDate, out getAllTrainingResponses, errorMessages ) )
            {
                LogMessageToDebuggingInteraction(
                    interactionId,
                    String.Format(
                        "Pulled Page {0} of trainings from {1} to {2}",
                        pageNumber,
                        startDate,
                        endDate
                        )
                    );

                LogMessageToDebuggingInteraction(
                    interactionId,
                    String.Format(
                        "Received Api Data </br> {0}</br></br>",
                        getAllTrainingResponses.ToJson()
                        )
                    );

                while ( getAllTrainingResponses.Any() )
                {
                    // Loop through trainings
                    foreach ( var getAllTrainingResponse in getAllTrainingResponses )
                    {
                        LogMessageToDebuggingInteraction(
                            interactionId,
                            String.Format(
                                "Processing Training for id:{0}",
                                getAllTrainingResponse.Id
                                )
                            );

                        var externalId = getAllTrainingResponse.Participant.PersonAliasId ?? getAllTrainingResponse.Participant.EmployeeId;
                        var userId = getAllTrainingResponse.Participant.Id;
                        var score = getAllTrainingResponse.Score;
                        var completedDateTime = getAllTrainingResponse.CompleteDateTime;
                        var surveyCode = getAllTrainingResponse.SurveyCode;
                        var createdDateTime = getAllTrainingResponse.CreatedDateTime;
                        if ( completedDateTime.HasValue )
                        {
                            if ( UpdateTraining( externalId, userId, score, surveyCode, completedDateTime.Value, createdDateTime, workflowType, interactionId ) )
                            {
                                trainingsProcessed++;
                            }
                            else
                            {
                                errorMessages.Add( String.Format( "Error updating training for id:{0}", getAllTrainingResponse.Id ) );
                            }
                        }
                    }

                    // Get New Trainings
                    pageNumber++;
                    if ( !MinistrySafeApiUtility.GetAllTrainings( pageNumber, startDate, endDate, out getAllTrainingResponses, errorMessages ) )
                    {
                        return false;
                    }

                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the user and workflow values.
        /// </summary>
        /// <param name="trainingWebhook">The training webhook.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        private static bool UpdateTrainingFromWebhook( TrainingWebhook trainingWebhook )
        {
            var externalId = trainingWebhook.ExternalId;
            var userId = trainingWebhook.UserId;
            var score = trainingWebhook.Score;
            var completedDateTime = trainingWebhook.CompleteDateTime;
            var surveyCode = trainingWebhook.SurveyCode;

            return UpdateTraining( externalId, userId, score, surveyCode, completedDateTime, null, null );
        }

        /// <summary>
        /// Updates the training.
        /// </summary>
        /// <param name="externalId">The external identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="score">The score.</param>
        /// <param name="surveyCode">The survey code.</param>
        /// <param name="completedDateTime">The completed date time.</param>
        /// <param name="createdDateTime">The created date time.</param>
        /// <param name="workflowTypeCache">The workflow type cache.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private static bool UpdateTraining( string externalId, string userId, int? score, string surveyCode, DateTime completedDateTime, DateTime? createdDateTime, WorkflowTypeCache workflowTypeCache = null, int? interactionId = null )
        {
            var rockContext = new RockContext();
            var errorMessages = new List<string>();

            LogMessageToDebuggingInteraction( interactionId, "Searching for PersonAliasId." );
            if ( externalId.IsNullOrWhiteSpace() )
            {
                externalId = FindRockPerson( userId, rockContext, errorMessages );
            }

            var isExternalIdPersonAlias = externalId.Contains( "pa" );
            var numericExternalId = externalId.RemoveAllNonNumericCharacters().AsIntegerOrNull();
            LogMessageToDebuggingInteraction( interactionId, "Found PersonAliasId. Searching for Training." );

            var ministrySafeUserService = new MinistrySafeUserService( rockContext );
            var ministrySafeUsers = ministrySafeUserService
                .Queryable( "PersonAlias.Person" )
                .Where( m =>
                        (
                            ( !isExternalIdPersonAlias && m.WorkflowId == numericExternalId && m.ForeignId == 3 ) ||
                            ( m.PersonAliasId == numericExternalId && ( m.ForeignId == 2 || m.ForeignId == 4 ) )
                        )
                    )
                .OrderBy( m => m.CompletedDateTime.HasValue )
                .ThenByDescending( m => m.CompletedDateTime )
                .ThenByDescending( m => m.ResponseDate )
                .ToList();

            if ( ministrySafeUsers != null && ministrySafeUsers.Count > 0 )
            {
                LogMessageToDebuggingInteraction( interactionId, String.Format( "Found {0} Matches: {1}",
                    ministrySafeUsers.Count,
                    ministrySafeUsers.Select( u =>
                        string.Format( "ID {0} Completed on {1}", u.Id, u.CompletedDateTime ) )
                        .JoinStringsWithCommaAnd()
                        )
                    );
            }

            var latestUser = ministrySafeUsers.FirstOrDefault();
            ministrySafeUsers = ministrySafeUsers.Where( m => m.CompletedDateTime == null
                    )
                .ToList();

            if ( ministrySafeUsers == null || ministrySafeUsers.Count <= 0 )
            {
                LogMessageToDebuggingInteraction( interactionId, "No Matching Open Trainings. Creating New Record." );

                // Is it older than the most recent completed one?
                if ( latestUser != null )
                {
                    LogMessageToDebuggingInteraction( interactionId, String.Format( "Using User Id {0} with CompletedDateTime {1} as Latest User", latestUser.Id, latestUser.CompletedDateTime ) );

                    var existingDateTime = DateTime.Parse( latestUser.CompletedDateTime.Value.ToShortDateTimeString() );
                    var importedDateTime = DateTime.Parse( completedDateTime.ToShortDateTimeString() );
                    int dateCompareResult = DateTime.Compare( existingDateTime, importedDateTime );
                    string relationship = string.Empty;
                    if ( dateCompareResult < 0 )
                        relationship = "is earlier than";
                    else if ( dateCompareResult == 0 )
                        relationship = "is the same time as";
                    else
                        relationship = "is later than";

                    LogMessageToDebuggingInteraction( interactionId, String.Format(
                        "Existing CompletionDate of {0} {1} Imported CompletionDate of {2}"
                        , existingDateTime
                        , relationship
                        , importedDateTime ) );

                    LogMessageToDebuggingInteraction( interactionId, String.Format(
                        "Existing CompletionDate.Ticks of {0} {1} Imported CompletionDate.Ticks of {2}"
                        , existingDateTime.Ticks
                        , relationship
                        , importedDateTime.Ticks ) );

                    if ( dateCompareResult <= 0 )
                    {
                        LogMessageToDebuggingInteraction( interactionId, "Existing user is up to date. Skipping import." );
                        return true;
                    }
                    else
                    {
                        LogMessageToDebuggingInteraction( interactionId, "Imported Training is newer. Proceeding with import." );
                    }
                }
                else
                {
                    LogMessageToDebuggingInteraction( interactionId, "No previous user to compare to." );
                }

                var ministrySafeUser = new MinistrySafeUser();
                ministrySafeUserService.Add( ministrySafeUser );
                ministrySafeUser.PersonAliasId = numericExternalId.Value;
                ministrySafeUser.ForeignId = 4;
                ministrySafeUser.SurveyCode = surveyCode;
                ministrySafeUser.RequestDate = createdDateTime ?? RockDateTime.Now;
                ministrySafeUser.UserId = userId.AsInteger();
                rockContext.SaveChanges();
                ministrySafeUser = ministrySafeUserService.Get( ministrySafeUser.Guid );
                ministrySafeUsers.Add( ministrySafeUser );

                LogMessageToDebuggingInteraction( interactionId, String.Format( "New Record Created: Id {0}", ministrySafeUser.Id ) );
            }

            foreach ( var ministrySafeUser in ministrySafeUsers )
            {
                LogMessageToDebuggingInteraction( interactionId, String.Format( "Updating Record: Id {0}", ministrySafeUser.Id ) );

                ministrySafeUser.Score = score;
                ministrySafeUser.CompletedDateTime = completedDateTime;
                ministrySafeUser.ResponseDate = RockDateTime.Now;
                ministrySafeUser.SurveyCode = surveyCode ?? ministrySafeUser.SurveyCode;

                LogMessageToDebuggingInteraction( interactionId, "Record Updated. Grabbing Workflow." );

                var workflowService = new WorkflowService( rockContext );
                Rock.Model.Workflow workflow = null;
                if ( ministrySafeUser.WorkflowId.HasValue )
                {
                    workflow = workflowService.Get( ministrySafeUser.WorkflowId.Value );
                }

                if ( workflow != null )
                {
                    LogMessageToDebuggingInteraction( interactionId, String.Format( "Found Matching Workflow Id: {0}.", workflow.Id ) );
                }

                if ( workflow == null && workflowTypeCache != null )
                {
                    // Add Workflow                    
                    workflow = Rock.Model.Workflow.Activate( workflowTypeCache, ministrySafeUser?.PersonAlias?.Person?.FullName );
                    workflowService.Add( workflow );
                    rockContext.SaveChanges();
                    ministrySafeUser.WorkflowId = workflow.Id;
                    LogMessageToDebuggingInteraction( interactionId, String.Format( "Created New Workflow Id: {0}.", workflow.Id ) );
                }

                rockContext.SaveChanges();

                LogMessageToDebuggingInteraction( interactionId, "Training Update Complete." );

                if ( ministrySafeUser.WorkflowId.HasValue && ministrySafeUser.WorkflowId > 0 )
                {
                    LogMessageToDebuggingInteraction( interactionId, "Launching UpdateTrainingWorkflow method." );
                    UpdateTrainingWorkflow( ministrySafeUser.WorkflowId.Value, score, completedDateTime, rockContext, ministrySafeUser.SurveyCode, ministrySafeUser.PersonAliasId, interactionId );
                }
            }

            return true;
        }

        /// <summary>
        /// Finds the rock person.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>System.String.</returns>
        private static string FindRockPerson( string userId, RockContext rockContext, List<string> errorMessages )
        {
            var externalId = string.Empty;
            UserResponse userResponse = null;
            if ( MinistrySafeApiUtility.GetUser( userId, out userResponse, errorMessages ) )
            {
                // Find Existing Match
                if ( userResponse.PersonAliasId.IsNotNullOrWhiteSpace() )
                {
                    return userResponse.PersonAliasId;
                }

                // Find Rock Match
                var personService = new PersonService( rockContext );
                var personQuery = new PersonService.PersonMatchQuery( userResponse.FirstName, userResponse.LastName, userResponse.Email, null );
                var person = personService.FindPerson( personQuery, false );

                if ( person == null )
                {
                    // Add New Person
                    person = new Person();
                    person.FirstName = userResponse.FirstName.FixCase();
                    person.LastName = userResponse.LastName.FixCase();
                    person.IsEmailActive = true;
                    person.Email = userResponse.Email;
                    person.EmailPreference = EmailPreference.EmailAllowed;
                    person.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;

                    var defaultConnectionStatus = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_PROSPECT.AsGuid() );
                    if ( defaultConnectionStatus != null )
                    {
                        person.ConnectionStatusValueId = defaultConnectionStatus.Id;
                    }

                    var defaultRecordStatus = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() );
                    if ( defaultRecordStatus != null )
                    {
                        person.RecordStatusValueId = defaultRecordStatus.Id;
                    }

                    var familyGroup = PersonService.SaveNewPerson( person, rockContext, null, false );
                    if ( familyGroup != null && familyGroup.Members.Any() )
                    {
                        person = familyGroup.Members.Select( m => m.Person ).First();
                    }
                }

                if ( person != null )
                {
                    externalId = string.Format( "pa{0}", person.PrimaryAliasId );
                }
            }

            return externalId;
        }

        /// <summary>
        /// Finds the type of the user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>System.String.</returns>
        private static string FindUserType( string userId, RockContext rockContext, List<string> errorMessages )
        {
            var userType = string.Empty;
            UserResponse userResponse = null;
            if ( !MinistrySafeApiUtility.GetUser( userId, out userResponse, errorMessages ) )
            {
                userType = userResponse.UserType;
            }

            return userType;
        }

        /// <summary>
        /// Sets the workflow RequestStatus attribute.
        /// </summary>
        /// <param name="workflow">The workflow.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="requestStatus">The request status.</param>
        private void UpdateWorkflowTrainingStatus( Rock.Model.Workflow workflow, RockContext rockContext, string requestStatus )
        {
            if ( SaveAttributeValue( workflow, "RequestStatus", requestStatus,
                FieldTypeCache.Get( Rock.SystemGuid.FieldType.TEXT.AsGuid() ), rockContext, null ) )
            {
                rockContext.SaveChanges();
            }
        }

        /// <summary>
        /// Get the survey type that the request is for.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="workflow">The Workflow initiating the request.</param>
        /// <param name="surveyTypeAttribute">The survey type attribute.</param>
        /// <param name="packageName">Name of the package.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        private bool GetSurveyTypeName( RockContext rockContext, Rock.Model.Workflow workflow, AttributeCache surveyTypeAttribute, out string packageName, List<string> errorMessages )
        {
            packageName = null;
            if ( surveyTypeAttribute == null )
            {
                errorMessages.Add( "The 'MinistrySafe' provider requires a survey type." );
                return false;
            }

            var definedValueGuid = workflow.GetAttributeValue( surveyTypeAttribute.Key ).AsGuid();
            DefinedValueCache surveyTypeDefinedValue = DefinedValueCache.Get( definedValueGuid );
            if ( surveyTypeDefinedValue == null )
            {
                errorMessages.Add( "The 'MinistrySafe' provider couldn't load survey type." );
                return false;
            }

            if ( surveyTypeDefinedValue.Attributes == null )
            {
                // shouldn't happen since pkgTypeDefinedValue is a ModelCache<,> type 
                return false;
            }

            packageName = surveyTypeDefinedValue.Value;
            return true;
        }

        /// <summary>
        /// Gets the name of the user type.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="workflow">The workflow.</param>
        /// <param name="userTypeAttribute">The user type attribute.</param>
        /// <param name="userTypeName">Name of the user type.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool GetUserTypeName( RockContext rockContext, Rock.Model.Workflow workflow, AttributeCache userTypeAttribute, out string userTypeName, List<string> errorMessages )
        {
            userTypeName = null;
            if ( userTypeAttribute == null )
            {
                errorMessages.Add( "The 'MinistrySafe' provider requires a user type." );
                return false;
            }

            DefinedValueCache userTypeDefinedValue = DefinedValueCache.Get( workflow.GetAttributeValue( userTypeAttribute.Key ).AsGuid() );
            if ( userTypeDefinedValue == null )
            {
                errorMessages.Add( "The 'MinistrySafe' provider couldn't load user type." );
                return false;
            }

            if ( userTypeDefinedValue.Attributes == null )
            {
                // shouldn't happen since pkgTypeDefinedValue is a ModelCache<,> type 
                return false;
            }

            userTypeName = userTypeDefinedValue.Value;
            return true;
        }

        /// <summary>
        /// Creates the invitation.
        /// </summary>
        /// <param name="candidateId">The candidate identifier.</param>
        /// <param name="surveyCode">The survey code.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        public static bool AssignTraining( string candidateId, string surveyCode, List<string> errorMessages )
        {
            TrainingResponse assignTrainingResponse;
            if ( MinistrySafeApiUtility.AssignTraining( candidateId, surveyCode, out assignTrainingResponse, errorMessages ) )
            {
                candidateId = assignTrainingResponse.Id;
                return true;
            }

            return false;
        }


        #endregion

        #region Shared Methods

        /// <summary>
        /// Logs the errors.
        /// </summary>
        /// <param name="errorMessages">The error messages.</param>
        private static void LogErrors( List<string> errorMessages )
        {
            if ( errorMessages.Any() )
            {
                foreach ( string errorMsg in errorMessages )
                {
                    ExceptionLogService.LogException( new Exception( "MinistrySafe Error: " + errorMsg ), null );
                }
            }
        }

        private static int? CreateDebuggingInteraction( string componentName, out string errorMessage )
        {
            errorMessage = string.Empty;
            using ( var rockContext = new RockContext() )
            {
                var settings = GetSettings( rockContext );
                if ( settings != null )
                {
                    var enableDebugging = GetSettingValue( settings, MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_ENABLE_DEBUGGING, false ).AsBoolean();
                    if ( enableDebugging )
                    {
                        var channelName = "MinistrySafe";

                        InteractionChannelCache channel = null;
                        // Find by Name
                        int? interactionChannelId = new InteractionChannelService( rockContext )
                            .Queryable()
                            .AsNoTracking()
                            .Where( c => c.Name == channelName )
                            .Select( c => c.Id )
                            .Cast<int?>()
                            .FirstOrDefault();

                        if ( interactionChannelId != null )
                        {
                            channel = InteractionChannelCache.Get( interactionChannelId.Value );
                        }
                        else
                        {
                            // If still no match, and we have a name, create a new channel
                            using ( var newRockContext = new RockContext() )
                            {
                                Rock.Model.InteractionChannel interactionChannel = new Rock.Model.InteractionChannel();
                                interactionChannel.Name = channelName;
                                new InteractionChannelService( newRockContext ).Add( interactionChannel );
                                newRockContext.SaveChanges();
                                channel = InteractionChannelCache.Get( interactionChannel.Id );
                            }
                        }

                        if ( channel == null )
                        {
                            errorMessage = "Interaction Channel could not be found to saved posted data to.";
                            return null;
                        }

                        // Get Interaction Component
                        InteractionComponentCache component = null;
                        int? interactionComponentId = new InteractionComponentService( rockContext )
                            .Queryable()
                            .AsNoTracking()
                            .Where( c => c.InteractionChannelId == channel.Id )
                            .Where( c => c.Name.Equals( componentName, StringComparison.OrdinalIgnoreCase ) )
                            .Select( c => c.Id )
                            .Cast<int?>()
                            .FirstOrDefault();

                        if ( interactionComponentId != null )
                        {
                            component = InteractionComponentCache.Get( interactionComponentId.Value );
                        }
                        else
                        {
                            // If still no match, and we have a name, create a new channel
                            using ( var newRockContext = new RockContext() )
                            {
                                var interactionComponent = new InteractionComponent();
                                interactionComponent.Name = componentName;
                                interactionComponent.InteractionChannelId = channel.Id;
                                new InteractionComponentService( newRockContext ).Add( interactionComponent );
                                newRockContext.SaveChanges();

                                component = InteractionComponentCache.Get( interactionComponent.Id );
                            }
                        }

                        if ( component == null )
                        {
                            errorMessage = "Interaction Component could not be found to saved posted data to.";
                            return null;
                        }

                        var interaction = new InteractionService( rockContext )
                            .AddInteraction(
                            interactionComponentId: component.Id,
                            entityId: null,
                            operation: "Data Posted",
                            interactionData: string.Empty,
                            personAliasId: null,
                            dateTime: RockDateTime.Now,
                            deviceApplication: null,
                            deviceOs: null,
                            deviceClientType: null,
                            deviceTypeData: null,
                            ipAddress: null,
                            browserSessionId: null );
                        rockContext.SaveChanges();

                        return interaction.Id;
                    }
                }
            }

            return null;
        }

        private static void LogMessageToDebuggingInteraction( int? interactionId, string logMessage )
        {
            if ( interactionId != null )
            {
                using ( var rockContext = new RockContext() )
                {
                    var interactionService = new InteractionService( rockContext );
                    var interaction = interactionService.Get( interactionId.Value );
                    if ( interaction != null )
                    {
                        StringBuilder sb = new StringBuilder( interaction.InteractionData );
                        sb.AppendFormat( "</br>[{0}] {1}"
                                , RockDateTime.Now.ToString()
                                , logMessage
                            );
                        interaction.InteractionData = sb.ToString();
                        rockContext.SaveChanges();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the person that is currently logged in.
        /// </summary>
        /// <returns>Person.</returns>
        private Person GetCurrentPerson()
        {
            using ( var rockContext = new RockContext() )
            {
                var currentUser = new UserLoginService( rockContext ).GetByUserName( UserLogin.GetCurrentUserName() );
                return currentUser != null ? currentUser.Person : null;
            }
        }

        /// <summary>
        /// Saves the attribute value.
        /// </summary>
        /// <param name="workflow">The workflow.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="fieldType">Type of the field.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="qualifiers">The qualifiers.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        private static bool SaveAttributeValue( Rock.Model.Workflow workflow, string key, string value,
            FieldTypeCache fieldType, RockContext rockContext, Dictionary<string, string> qualifiers = null )
        {
            bool createdNewAttribute = false;

            if ( workflow.Attributes.ContainsKey( key ) )
            {
                workflow.SetAttributeValue( key, value );
            }
            else
            {
                // Read the attribute
                var attributeService = new AttributeService( rockContext );
                var attribute = attributeService
                    .Get( workflow.TypeId, "WorkflowTypeId", workflow.WorkflowTypeId.ToString() )
                    .Where( a => a.Key == key )
                    .FirstOrDefault();

                // If workflow attribute doesn't exist, create it
                // ( should only happen first time a background check is processed for given workflow type)
                if ( attribute == null )
                {
                    attribute = new Rock.Model.Attribute();
                    attribute.EntityTypeId = workflow.TypeId;
                    attribute.EntityTypeQualifierColumn = "WorkflowTypeId";
                    attribute.EntityTypeQualifierValue = workflow.WorkflowTypeId.ToString();
                    attribute.Name = key.SplitCase();
                    attribute.Key = key;
                    attribute.FieldTypeId = fieldType.Id;
                    attributeService.Add( attribute );

                    if ( qualifiers != null )
                    {
                        foreach ( var keyVal in qualifiers )
                        {
                            var qualifier = new AttributeQualifier();
                            qualifier.Key = keyVal.Key;
                            qualifier.Value = keyVal.Value;
                            attribute.AttributeQualifiers.Add( qualifier );
                        }
                    }

                    createdNewAttribute = true;
                }

                // Set the value for this attribute
                var attributeValue = new AttributeValue();
                attributeValue.Attribute = attribute;
                attributeValue.EntityId = workflow.Id;
                attributeValue.Value = value;
                new AttributeValueService( rockContext ).Add( attributeValue );
            }

            return createdNewAttribute;
        }

        /// <summary>
        /// Get the person that the request is for.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="workflow">The Workflow initiating the request.</param>
        /// <param name="personAttribute">The person attribute.</param>
        /// <param name="person">Return the person.</param>
        /// <param name="personAliasId">Return the person alias ID.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        private bool GetPerson( RockContext rockContext, Rock.Model.Workflow workflow, AttributeCache personAttribute, out Person person, out int? personAliasId, List<string> errorMessages )
        {
            person = null;
            personAliasId = null;
            if ( personAttribute != null )
            {
                Guid? personAliasGuid = workflow.GetAttributeValue( personAttribute.Key ).AsGuidOrNull();
                if ( personAliasGuid.HasValue )
                {
                    person = new PersonAliasService( rockContext ).Queryable()
                        .Where( p => p.Guid.Equals( personAliasGuid.Value ) )
                        .Select( p => p.Person )
                        .FirstOrDefault();
                    person.LoadAttributes( rockContext );
                }
            }

            if ( person == null )
            {
                errorMessages.Add( "The 'MinistrySafe' provider requires the workflow to have a 'Person' attribute that contains the person who the training is for." );
                return false;
            }

            personAliasId = person.PrimaryAliasId;
            if ( !personAliasId.HasValue )
            {
                errorMessages.Add( "The 'MinistrySafe' provider requires the workflow to have a 'Person' attribute that contains the person who the training is for." );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates the candidate.
        /// </summary>
        /// <param name="workflow">The workflow.</param>
        /// <param name="person">The person.</param>
        /// <param name="personAliasId">The person alias identifier.</param>
        /// <param name="userTypeName">Name of the user type.</param>
        /// <param name="tagList">The tag list.</param>
        /// <param name="candidateId">The candidate identifier.</param>
        /// <param name="directLoginUrl">The direct login URL.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        public static bool GetOrCreateUser( Rock.Model.Workflow workflow, Person person, int personAliasId, string userTypeName, string tagList, out string candidateId, out string directLoginUrl, List<string> errorMessages )
        {
            UserResponse userResponse;
            candidateId = null;
            directLoginUrl = null;
            if ( MinistrySafeApiUtility.GetUser( workflow, person, personAliasId, out userResponse, errorMessages ) )
            {
                candidateId = userResponse.Id;
                directLoginUrl = userResponse.DirectLoginUrl;

                if ( tagList.IsNotNullOrWhiteSpace() )
                {
                    if ( MinistrySafeApiUtility.UpdateUser( candidateId.AsInteger(), person.Email, tagList, out errorMessages ) )
                    {
                        return true;
                    }
                    else
                    {
                        errorMessages.Add( "Error updating tags on existing user" );
                        return false;
                    }
                }

                return true;
            }
            else
            {
                if ( MinistrySafeApiUtility.CreateUser( workflow, person, personAliasId, userTypeName, tagList, out userResponse, errorMessages ) )
                {
                    candidateId = userResponse.Id;
                    directLoginUrl = userResponse.DirectLoginUrl;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Saves the webhook results.
        /// </summary>
        /// <param name="postedData">The posted data.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        public static bool SaveWebhookResults( string postedData, out string responseMessage )
        {
            responseMessage = string.Empty;

            // Save Interaction storing information
            var errorMessage = string.Empty;
            int? interactionId = CreateDebuggingInteraction( "Webhook Data", out errorMessage );
            if ( errorMessage.IsNotNullOrWhiteSpace() )
            {
                responseMessage = errorMessage;
                Rock.Model.ExceptionLogService.LogException( new Exception( responseMessage ), null );
                return false;
            }

            LogMessageToDebuggingInteraction(
                interactionId,
                String.Format(
                    "Received Webhook Data </br> {0}</br></br>",
                    postedData
                    )
                );

            // Try casting as Training
            TrainingWebhook trainingWebhook = JsonConvert.DeserializeObject<TrainingWebhook>( postedData, new JsonSerializerSettings()
            {
                Error = ( sender, errorEventArgs ) =>
                {
                    errorEventArgs.ErrorContext.Handled = true;
                    Rock.Model.ExceptionLogService.LogException( new Exception( errorEventArgs.ErrorContext.Error.Message ), null );
                }
            } );

            if ( trainingWebhook.CertificateUrl != null )
            {
                responseMessage = "Valid Training Data Received";
                return UpdateTrainingFromWebhook( trainingWebhook );
            }

            // Try casting as Background Check
            BackgroundCheckWebhook backgroundCheckWebhook = JsonConvert.DeserializeObject<BackgroundCheckWebhook>( postedData, new JsonSerializerSettings()
            {
                Error = ( sender, errorEventArgs ) =>
                {
                    errorEventArgs.ErrorContext.Handled = true;
                    Rock.Model.ExceptionLogService.LogException( new Exception( errorEventArgs.ErrorContext.Error.Message ), null );
                }
            } );

            if ( backgroundCheckWebhook != null )
            {
                responseMessage = "Valid Background Check Data Received";
                return UpdateBackgroundCheckAndWorkFlow( backgroundCheckWebhook, interactionId );
            }

            // Return Invalid Data
            responseMessage = "Webhook data is not valid: " + postedData;
            Rock.Model.ExceptionLogService.LogException( new Exception( responseMessage ), null );
            return false;
        }

        /// <summary>
        /// Gets the user tags.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="workflow">The workflow.</param>
        /// <param name="personAttribute">The person attribute.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal bool GetUserTags( RockContext rockContext, Rock.Model.Workflow workflow, AttributeCache personAttribute, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            try
            {
                // Check to make sure workflow is not null
                if ( workflow == null )
                {
                    errorMessages.Add( "The 'MinistrySafe' provider requires a valid workflow." );
                    UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                    return true;
                }

                // Lock the workflow until we're finished saving so the webhook can't start working on it.
                var lockObject = _lockObjects.GetOrAdd( workflow.Id, new object() );
                lock ( lockObject )
                {
                    Person person;
                    int? personAliasId;
                    if ( !GetPerson( rockContext, workflow, personAttribute, out person, out personAliasId, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to get Person." );
                        UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                        return true;
                    }

                    string tagList = null;
                    if ( !GetTags( rockContext, workflow, out tagList, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to get Tags." );
                        UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                        return true;
                    }

                    UserResponse userResponse;
                    if ( MinistrySafeApiUtility.GetUser( workflow, person, personAliasId.Value, out userResponse, errorMessages ) )
                    {
                        var rockTagList = tagList.SplitDelimitedValues().ToList();
                        var ministrySafeTagList = userResponse.TagList;
                        rockTagList.AddRange( ministrySafeTagList );
                        var newTagDefinedValueGuids = DefinedTypeCache.Get( com.bemaservices.MinistrySafe.Constants.MinistrySafeSystemGuid.MINISTRYSAFE_TAGS ).DefinedValues
                            .Where( dv => rockTagList.Contains( dv.Value ) )
                            .Select( dv => dv.Guid )
                            .ToList()
                            .AsDelimited( "," );

                        if ( SaveAttributeValue( workflow, "UserTags", newTagDefinedValueGuids,
                            FieldTypeCache.Get( Rock.SystemGuid.FieldType.TEXT.AsGuid() ), rockContext, null ) )
                        {
                            rockContext.SaveChanges();
                        }

                        if ( workflow.IsPersisted )
                        {
                            // Make sure the AttributeValues are saved to the database immediately because the MinistrySafe WebHook
                            // (which might otherwise get called before they are saved by the workflow processing) needs to
                            // have the correct attribute values.
                            workflow.SaveAttributeValues( rockContext );
                        }
                    }

                    _lockObjects.TryRemove( workflow.Id, out _ ); // we no longer need that lock for this workflow
                }

                return true;

            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex, null );
                errorMessages.Add( ex.Message );
                UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                return true;
            }
        }


        /// <summary>
        /// Gets the tags.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="workflow">The workflow.</param>
        /// <param name="tagList">The tag list.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool GetTags( RockContext rockContext, Rock.Model.Workflow workflow, out string tagList, List<string> errorMessages )
        {
            tagList = null;

            if ( !workflow.Attributes.ContainsKey( "UserTags" ) )
            {
                workflow.AddLogEntry( "The 'MinistrySafe' provider couldn't find the 'User Tags' attribute." );
                return false;
            }

            var definedValueGuids = workflow.GetAttributeValue( "UserTags" ).SplitDelimitedValues().AsGuidList();
            var definedValues = new DefinedValueService( rockContext ).GetByGuids( definedValueGuids );
            tagList = definedValues.Select( dv => dv.Value ).ToList().AsDelimited( "," );

            return true;
        }


        /// <summary>
        /// Gets the settings.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <returns>List&lt;AttributeValue&gt;.</returns>
        public static List<AttributeValue> GetSettings( RockContext rockContext )
        {
            var ministrySafeEntityType = EntityTypeCache.Get( typeof( com.bemaservices.MinistrySafe.MinistrySafe ) );
            if ( ministrySafeEntityType != null )
            {
                var service = new AttributeValueService( rockContext );
                return service.Queryable( "Attribute" )
                    .Where( v => v.Attribute.EntityTypeId == ministrySafeEntityType.Id )
                    .ToList();
            }

            return null;
        }

        /// <summary>
        /// Gets the setting value.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="key">The key.</param>
        /// <param name="encryptedValue">if set to <c>true</c> [encrypted value].</param>
        /// <returns>System.String.</returns>
        public static string GetSettingValue( List<AttributeValue> values, string key, bool encryptedValue = false )
        {
            string value = values
                .Where( v => v.AttributeKey == key )
                .Select( v => v.Value )
                .FirstOrDefault();
            if ( encryptedValue && !string.IsNullOrWhiteSpace( value ) )
            {
                try
                { value = Encryption.DecryptString( value ); }
                catch { }
            }

            return value;
        }



        /// <summary>
        /// Sets the setting value.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="values">The values.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void SetSettingValue( RockContext rockContext, List<AttributeValue> values, string key, string value, bool encryptValue = false )
        {
            if ( encryptValue && !string.IsNullOrWhiteSpace( value ) )
            {
                try
                { value = Encryption.EncryptString( value ); }
                catch { }
            }

            var attributeValue = values
                .Where( v => v.AttributeKey == key )
                .FirstOrDefault();
            if ( attributeValue != null )
            {
                attributeValue.Value = value;
            }
            else
            {
                var ministrySafeEntityType = EntityTypeCache.Get( typeof( com.bemaservices.MinistrySafe.MinistrySafe ) );
                if ( ministrySafeEntityType != null )
                {
                    var attribute = new AttributeService( rockContext )
                        .Queryable()
                        .Where( a =>
                            a.EntityTypeId == ministrySafeEntityType.Id &&
                            a.Key == key
                        )
                        .FirstOrDefault();

                    if ( attribute != null )
                    {
                        attributeValue = new AttributeValue();
                        new AttributeValueService( rockContext ).Add( attributeValue );
                        attributeValue.AttributeId = attribute.Id;
                        attributeValue.Value = value;
                        attributeValue.EntityId = 0;
                    }
                }
            }
        }

        #endregion
    }
}
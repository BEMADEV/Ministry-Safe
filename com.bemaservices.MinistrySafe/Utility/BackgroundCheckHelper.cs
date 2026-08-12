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
using Humanizer;
using Newtonsoft.Json;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.IpAddress;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace com.bemaservices.MinistrySafe.Utility
{
    internal class BackgroundCheckHelper
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

        #region Component Implementation / Outgoing Data Methods

        internal static bool SendRequest( RockContext rockContext, Rock.Model.Workflow workflow,
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
                    SharedHelper.UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                    UpdateWorkflowRequestMessage( workflow, rockContext, errorMessages.AsDelimited( ", " ) );
                    return true;
                }

                // Lock the workflow until we're finished saving so the webhook can't start working on it.
                var lockObject = _lockObjects.GetOrAdd( workflow.Id, new object() );
                lock ( lockObject )
                {
                    Person person;
                    int? personAliasId;
                    if ( !UserHelper.GetPerson( rockContext, workflow, personAttribute, out person, out personAliasId, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to get Person." );
                        SharedHelper.UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                        UpdateWorkflowRequestMessage( workflow, rockContext, errorMessages.AsDelimited( ", " ) );
                        return true;
                    }

                    int? level = null;
                    string packageCode = null;
                    string userType = null;
                    string packageName = null;
                    if ( !GetPackageName( rockContext, workflow, requestTypeAttribute, out level, out packageCode, out userType, out packageName, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to get Package." );
                        SharedHelper.UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                        UpdateWorkflowRequestMessage( workflow, rockContext, errorMessages.AsDelimited( ", " ) );
                        return true;
                    }

                    bool? childServing = null;
                    if ( !UserHelper.GetChildServing( rockContext, workflow, out childServing, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to determine whether the role is Child-Serving." );
                        SharedHelper.UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                        UpdateWorkflowRequestMessage( workflow, rockContext, errorMessages.AsDelimited( ", " ) );
                        return true;
                    }

                    bool? over13 = null;
                    if ( !UserHelper.GetOverThirteen( rockContext, workflow, out over13, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to determine whether the applicant is over 13." );
                        SharedHelper.UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                        UpdateWorkflowRequestMessage( workflow, rockContext, errorMessages.AsDelimited( ", " ) );
                        return true;
                    }

                    string salaryRange = null;
                    if ( !UserHelper.GetSalaryRange( rockContext, workflow, out salaryRange, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to determine the Applicant's salary range." );
                        SharedHelper.UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                        UpdateWorkflowRequestMessage( workflow, rockContext, errorMessages.AsDelimited( ", " ) );
                        return true;
                    }

                    string tagList = null;
                    if ( !UserHelper.GetTags( rockContext, workflow, out tagList, errorMessages ) )
                    {
                        workflow.AddLogEntry( "Unable to get Tags." );
                    }

                    int? userId;
                    if ( !UserHelper.GetOrCreateUser( person, personAliasId.Value, userType, tagList, out userId, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to create user." );
                        SharedHelper.UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                        UpdateWorkflowRequestMessage( workflow, rockContext, errorMessages.AsDelimited( ", " ) );
                        return true;
                    }

                    int? requestId;
                    string applicantInterfaceUrl;
                    if ( !CreateBackgroundCheck( userId.Value, level, packageCode, userType, childServing, over13, salaryRange, out requestId, out applicantInterfaceUrl, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to create background check." );
                        SharedHelper.UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
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
                        backgroundCheck.RequestId = requestId.ToString();
                        newRockContext.SaveChanges();
                    }

                    SharedHelper.UpdateWorkflowRequestStatus( workflow, rockContext, "SUCCESS" );

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
                SharedHelper.UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
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
        internal static string GetReportUrl( string backgroundCheckId )
        {
            BackgroundCheckV3 getDocumentResponse;
            List<string> errorMessages = new List<string>();

            if ( ApiHelper.GetBackgroundCheck( backgroundCheckId.AsInteger(), out getDocumentResponse, errorMessages ) )
            {
                return getDocumentResponse.ResultsUrl;
            }
            else
            {
                SharedHelper.LogErrors( errorMessages );
            }
            return backgroundCheckId;
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
        public static bool CreateBackgroundCheck(
            int userId,
            int? level,
            string packageCode,
            string userType,
            bool? childServing,
            bool? over13,
            string salaryRange,
            out int? requestId,
            out string applicantInterfaceUrl,
            List<string> errorMessages )
        {
            requestId = null;
            applicantInterfaceUrl = null;
            BackgroundCheckV3 backgroundCheck;
            if ( ApiHelper.CreateQuickApp(
                userId,
                level,
                packageCode,
                userType,
                childServing,
                over13,
                salaryRange,
                out backgroundCheck,
                errorMessages )
                )
            {
                requestId = backgroundCheck.Id;
                applicantInterfaceUrl = backgroundCheck.ApplicantInterfaceUrl;
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
        internal static bool ArchiveLinkedBackgroundChecks( RockContext rockContext, Rock.Model.Workflow workflow, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            var backgroundCheckService = new BackgroundCheckService( rockContext );
            var backgroundChecks = backgroundCheckService.Queryable().Where( bc => bc.WorkflowId == workflow.Id ).ToList();
            foreach ( BackgroundCheck backgroundCheck in backgroundChecks )
            {
                var backgroundCheckErrorMessages = new List<string>();
                BackgroundCheckV3 backgroundCheckResponse = null;

                if ( !ApiHelper.ArchiveBackgroundCheck( backgroundCheck.RequestId, out backgroundCheckResponse, backgroundCheckErrorMessages ) )
                {
                    errorMessages.Add( String.Format( "Error archiving BackgroundCheck with RockId:{0} and MinistrySafeId:{1}"
                        , backgroundCheck.Id
                        , backgroundCheck.RequestId ) );
                    errorMessages.AddRange( backgroundCheckErrorMessages );
                    return false;
                }

                var requestId = backgroundCheckResponse.Id;
                var resultsUrl = backgroundCheckResponse.ResultsUrl;
                var completionDate = backgroundCheckResponse.CompleteDate;
                var orderDate = backgroundCheckResponse.OrderDate;


                backgroundCheck.Status = "archived";
                backgroundCheck.ResponseId = requestId.ToString();
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

        #region Response / Incoming Data Methods

        /// <summary>
        /// Updates the background check and workflow values.
        /// </summary>
        /// <param name="backgroundCheckWebhook">The background check webhook.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        internal static bool UpdateBackgroundCheckFromWebhook( BackgroundCheckV3_Webhook backgroundCheckWebhook, int? interactionId = null )
        {
            try
            {
                var requestId = backgroundCheckWebhook.Id;
                var externalId = backgroundCheckWebhook.ExternalId;
                var resultsUrl = backgroundCheckWebhook.ResultsUrl;
                var userId = backgroundCheckWebhook.UserId;
                var status = backgroundCheckWebhook.Status;
                var completionDate = backgroundCheckWebhook.CompleteDate;
                var orderDate = backgroundCheckWebhook.OrderDate;
                var tazworkFlagged = backgroundCheckWebhook.TazworkFlagged;

                SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Loaded Background Check Properties from Webhook Data." );

                return UpdateBackgroundCheck(
                    requestId,
                    externalId,
                    resultsUrl,
                    userId,
                    status,
                    completionDate,
                    orderDate,
                    tazworkFlagged,
                    null,
                    false,
                    interactionId );
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException(
                    new Exception(
                        String.Format( "MinistrySafe Error{0}"
                        , interactionId != null ? String.Format( " on webhook data id {0}", interactionId ) : ""
                        )
                    , ex ), null );
                SharedHelper.LogMessageToDebuggingInteraction( interactionId, "An Error has occurred. See the exception log for more details." );
                return false;
            }
        }

        /// <summary>
        /// Imports the background checks.
        /// </summary>
        /// <param name="dateRange">The date range.</param>
        /// <param name="workflowType">Type of the workflow.</param>
        /// <param name="relaunchCompletedWorkflows">if set to <c>true</c>, launch a new workflow if the existing workflow has already completed.</param>
        /// <param name="backgroundChecksProcessed">The background checks processed.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool ImportBackgroundChecks( DateRange dateRange, WorkflowTypeCache workflowType, bool relaunchCompletedWorkflows, out int backgroundChecksProcessed, out List<string> errorMessages )
        {
            var startDate = dateRange.Start;
            var endDate = dateRange.End;
            backgroundChecksProcessed = 0;
            errorMessages = new List<string>();
            List<BackgroundCheckV3> backgroundCheckList;

            // Save Interaction storing information
            var errorMessage = string.Empty;
            int? interactionId = SharedHelper.CreateDebuggingInteraction( "Background Check Import", out errorMessage );
            if ( errorMessage.IsNotNullOrWhiteSpace() )
            {
                errorMessages.Add( errorMessage );
                return false;
            }

            if ( ApiHelper.GetAllBackgroundChecks( 1, startDate, endDate, out backgroundCheckList, errorMessages ) )
            {
                SharedHelper.LogMessageToDebuggingInteraction(
                    interactionId,
                    String.Format(
                        "Pulled Page {0} of background checks from {1} to {2}",
                        1,
                        startDate,
                        endDate
                        )
                    );

                SharedHelper.LogMessageToDebuggingInteraction(
                    interactionId,
                    String.Format(
                        "Received Api Data </br> {0}</br></br>",
                        backgroundCheckList.ToJson()
                        )
                    );

                while ( backgroundCheckList.Any() )
                {
                    // Loop through trainings
                    foreach ( var backgroundCheck in backgroundCheckList )
                    {
                        SharedHelper.LogMessageToDebuggingInteraction(
                            interactionId,
                            String.Format(
                                "Processing Background Check for id:{0}",
                                backgroundCheck.Id
                                )
                            );

                        var requestId = backgroundCheck.Id;
                        var resultsUrl = backgroundCheck.ResultsUrl;
                        var userId = backgroundCheck.UserId;
                        var status = backgroundCheck.Status;
                        var completionDate = backgroundCheck.CompleteDate;
                        var orderDate = backgroundCheck.OrderDate;
                        var tazworkFlagged = backgroundCheck.TazworkFlagged;
                        if ( completionDate.HasValue ||
                            BackgroundCheckStatuses.COMPLETED_NEEDS_REVIEW.Contains( status ) ||
                            BackgroundCheckStatuses.COMPLETED_CLEARED.Contains( status ) ||
                            BackgroundCheckStatuses.CANCELLED.Contains( status ) )
                        {
                            if ( UpdateBackgroundCheck( requestId,
                                null,
                                resultsUrl,
                                userId,
                                status,
                                completionDate,
                                orderDate,
                                tazworkFlagged,
                                workflowType,
                                relaunchCompletedWorkflows ) )
                            {
                                backgroundChecksProcessed++;
                            }
                            else
                            {
                                errorMessages.Add( String.Format( "Error updating background check for id:{0}", backgroundCheck.Id ) );
                            }
                        }
                    }
                }

                return true;
            }

            return false;
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
        /// <param name="tazworkFlagged">if set to <c>true</c> [tazwork flagged].</param>
        /// <param name="workflowTypeCache">The workflow type cache.</param>
        /// <param name="relaunchCompletedWorkflows">if set to <c>true</c>, launch a new workflow if the existing workflow has already completed.</param>
        /// <param name="interactionId">The interaction identifier.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool UpdateBackgroundCheck(
            int? requestId,
            string externalId,
            string resultsUrl,
            int? userId,
            string status,
            DateTime? completionDate,
            DateTime? orderDate,
            bool? tazworkFlagged = null,
            WorkflowTypeCache workflowTypeCache = null,
            bool relaunchCompletedWorkflows = false,
            int? interactionId = null )
        {
            try
            {
                SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Searching for PersonAliasId." );

                using ( var rockContext = new RockContext() )
                {
                    //string userType = null;
                    var backgroundCheckService = new BackgroundCheckService( rockContext );
                    var errorMessages = new List<string>();

                    int? personAliasId = externalId?.RemoveAllNonNumericCharacters().AsIntegerOrNull();
                    if ( personAliasId == null )
                    {
                        personAliasId = UserHelper.FindRockPerson( userId.ToString(), rockContext, errorMessages );
                    }

                    if ( personAliasId == null )
                    {
                        SharedHelper.LogMessageToDebuggingInteraction( interactionId, string.Format( "Failed to find PersonAliasId for {0}. Skipping import.", externalId ) );
                        return false;
                    }

                    SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Found PersonAliasId. Searching for Background Check." );

                    var requestIdString = requestId?.ToString();
                    var personBackgroundChecks = new BackgroundCheckService( rockContext )
                        .Queryable( "PersonAlias.Person" )
                        .Where( g => ( requestId != null && g.RequestId == requestIdString ) || ( g.PersonAliasId == personAliasId ) )
                        .Where( g => g.ForeignId == 4 )
                        .OrderBy( m => m.ResponseDate.HasValue )
                        .ThenByDescending( m => m.ResponseDate )
                        .ThenByDescending( m => m.RequestDate );

                    var backgroundCheck = personBackgroundChecks
                        .Where( g => ( requestId != null && g.RequestId == requestIdString ) || ( requestId == null && g.PersonAliasId == personAliasId ) )
                        .OrderBy( m => m.ResponseDate.HasValue )
                        .ThenByDescending( m => m.ResponseDate )
                        .ThenByDescending( m => m.RequestDate )
                        .FirstOrDefault();

                    // Is it older than the most recent completed one?
                    var mostRecentBackgroundCheck = personBackgroundChecks.FirstOrDefault();
                    if ( mostRecentBackgroundCheck != null &&
                        orderDate != null &&
                        ( backgroundCheck == null || backgroundCheck != mostRecentBackgroundCheck )
                        )
                    {
                        SharedHelper.LogMessageToDebuggingInteraction( interactionId, String.Format( "Using Background Check Id {0} with RequestDate {1} as Latest Background Check", mostRecentBackgroundCheck.Id, mostRecentBackgroundCheck.RequestDate ) );

                        var existingDateTime = DateTime.Parse( mostRecentBackgroundCheck.RequestDate.ToShortDateTimeString() );
                        var importedDateTime = DateTime.Parse( orderDate.Value.ToShortDateTimeString() );
                        int dateCompareResult = DateTime.Compare( existingDateTime, importedDateTime );
                        string relationship = string.Empty;
                        if ( dateCompareResult < 0 )
                            relationship = "is earlier than";
                        else if ( dateCompareResult == 0 )
                            relationship = "is the same time as";
                        else
                            relationship = "is later than";

                        SharedHelper.LogMessageToDebuggingInteraction( interactionId, String.Format(
                            "Existing OrderDate of {0} {1} Imported OrderDate of {2}"
                            , existingDateTime
                            , relationship
                            , importedDateTime ) );

                        SharedHelper.LogMessageToDebuggingInteraction( interactionId, String.Format(
                            "Existing OrderDate.Ticks of {0} {1} Imported OrderDate.Ticks of {2}"
                            , existingDateTime.Ticks
                            , relationship
                            , importedDateTime.Ticks ) );

                        if ( dateCompareResult >= 0 )
                        {
                            SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Existing background check is up to date. Skipping import." );
                            return true;
                        }
                        else
                        {
                            SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Imported Background Check is newer. Proceeding with import." );
                        }
                    }

                    if ( backgroundCheck != null )
                    {
                        SharedHelper.LogMessageToDebuggingInteraction( interactionId, String.Format( "Matched on BackgroundCheck Id {0}", backgroundCheck.Id ) );
                    }

                    if ( backgroundCheck == null )
                    {
                        SharedHelper.LogMessageToDebuggingInteraction( interactionId, "No Matching Background Check. Creating New Record." );

                        backgroundCheck = new BackgroundCheck();
                        backgroundCheckService.Add( backgroundCheck );

                        backgroundCheck.PersonAliasId = personAliasId.Value;
                        backgroundCheck.ForeignId = 4;
                        backgroundCheck.PackageName = "";
                        backgroundCheck.RequestDate = orderDate ?? RockDateTime.Now;

                        backgroundCheck.RequestId = requestIdString;
                        rockContext.SaveChanges();
                        SharedHelper.LogMessageToDebuggingInteraction( interactionId, String.Format( "New Record Created: Id {0}", backgroundCheck.Id ) );

                    }

                    SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Setting Status." );

                    backgroundCheck.Status = status;
                    if ( backgroundCheck.Status.IsNullOrWhiteSpace() )
                    {
                        backgroundCheck.Status = "ready";
                    }

                    if ( BackgroundCheckStatuses.COMPLETED_NEEDS_REVIEW.Contains( backgroundCheck.Status ) && tazworkFlagged != null && tazworkFlagged != true )
                    {
                        backgroundCheck.Status = "clear";
                    }

                    SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Status Set. Updating Response Info" );


                    backgroundCheck.ResponseId = requestIdString;
                    backgroundCheck.ResponseDate = completionDate ?? ( orderDate ?? RockDateTime.Now );
                    if ( resultsUrl.IsNotNullOrWhiteSpace() )
                    {
                        backgroundCheck.ResponseData = resultsUrl;
                    }

                    SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Response Info Updated. Saving Changes." );

                    rockContext.SaveChanges();

                    SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Changes Saved. Setting Recommendation and Report Status." );

                    string recommendation = null;
                    string reportStatus = null; //Pass,Fail,Review
                    if ( BackgroundCheckStatuses.AWAITING_APPLICANT.Contains( backgroundCheck.Status ) )
                    {
                        recommendation = "Awaiting Applicant";
                    }
                    else if ( BackgroundCheckStatuses.CANCELLED.Contains( backgroundCheck.Status ) )
                    {
                        recommendation = "Cancelled";
                        reportStatus = "Cancelled";
                    }
                    else if ( BackgroundCheckStatuses.COMPLETED_NEEDS_REVIEW.Contains( backgroundCheck.Status ) )
                    {
                        recommendation = "Candidate Review";
                        reportStatus = "Review";
                    }
                    else if ( BackgroundCheckStatuses.COMPLETED_CLEARED.Contains( backgroundCheck.Status ) )
                    {
                        recommendation = "Candidate Pass";
                        reportStatus = "Pass";
                    }
                    else if ( BackgroundCheckStatuses.DISPUTED.Contains( backgroundCheck.Status ) )
                    {
                        recommendation = "Report Disputed";
                    }
                    else if ( BackgroundCheckStatuses.ERROR.Contains( backgroundCheck.Status ) )
                    {
                        recommendation = "Error";
                    }
                    else if ( BackgroundCheckStatuses.PROCESSING.Contains( backgroundCheck.Status ) )
                    {
                        recommendation = "Report Processing";
                    }
                    else if ( BackgroundCheckStatuses.SUBMITTED.Contains( backgroundCheck.Status ) )
                    {
                        recommendation = "Submitted";
                    }

                    SharedHelper.LogMessageToDebuggingInteraction( interactionId,
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
                        SharedHelper.LogMessageToDebuggingInteraction( interactionId, String.Format( "Found Matching Workflow Id: {0}.", workflow.Id ) );

                        // Check if the workflow is completed and we should relaunch
                        if ( relaunchCompletedWorkflows && workflow.CompletedDateTime.HasValue && workflowTypeCache != null )
                        {
                            SharedHelper.LogMessageToDebuggingInteraction( interactionId, String.Format( "Existing Workflow Id: {0} is completed (CompletedDateTime: {1}). Relaunching new workflow.", workflow.Id, workflow.CompletedDateTime ) );
                            workflow = null; // Clear so a new one will be created below
                        }
                    }

                    if ( workflow == null && workflowTypeCache != null )
                    {
                        // Add Workflow
                        var personAlias = new PersonAliasService( rockContext ).Get( personAliasId.Value );
                        workflow = Rock.Model.Workflow.Activate( workflowTypeCache, personAlias.Person.FullName );
                        workflowService.Add( workflow );
                        rockContext.SaveChanges();
                        backgroundCheck.WorkflowId = workflow.Id;
                        SharedHelper.LogMessageToDebuggingInteraction( interactionId, String.Format( "Created New Workflow Id: {0}.", workflow.Id ) );

                    }

                    rockContext.SaveChanges();

                    SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Background Check Update Complete." );


                    if ( backgroundCheck.WorkflowId.HasValue && backgroundCheck.WorkflowId > 0 )
                    {
                        SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Launching UpdateBackgroundCheckWorkflow method." );

                        UpdateBackgroundCheckWorkflow( backgroundCheck.WorkflowId.Value, recommendation, backgroundCheck.ResponseId, reportStatus, rockContext, backgroundCheck.PersonAliasId, resultsUrl, interactionId );
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
                SharedHelper.LogMessageToDebuggingInteraction( interactionId, "An Error has occurred. See the exception log for more details." );
                return false;
            }
        }

        #endregion

        #region Workflow Methods

        /// <summary>
        /// Updates the workflow, closing it if the reportStatus is blank and the recommendation is "Invitation Expired".
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="recommendation">The recommendation.</param>
        /// <param name="documentId">The document identifier.</param>
        /// <param name="reportStatus">The report status.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="personAliasId">The person alias identifier.</param>
        internal static void UpdateBackgroundCheckWorkflow( int id, string recommendation, string documentId, string reportStatus, RockContext rockContext, int? personAliasId = null, string resultsUrl = null, int? interactionId = null )//, string customPackageCode = null, int? level = null, string userType = null )
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
                    SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Updating Workflow." );

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
                                if ( SharedHelper.SaveAttributeValue( workflow, "Person", personAlias.Guid.ToString(),
                                FieldTypeCache.Get( Rock.SystemGuid.FieldType.PERSON.AsGuid() ), rockContext ) )
                                {
                                }
                            }
                        }
                    }

                    // Save the recommendation
                    if ( !string.IsNullOrWhiteSpace( recommendation ) )
                    {
                        if ( SharedHelper.SaveAttributeValue( workflow, "ReportRecommendation", recommendation,
                            FieldTypeCache.Get( Rock.SystemGuid.FieldType.TEXT.AsGuid() ), rockContext,
                            new Dictionary<string, string> { { "ispassword", "false" } } ) )
                        {
                        }
                    }

                    // Save the report link
                    if ( documentId.IsNotNullOrWhiteSpace() )
                    {
                        int entityTypeId = EntityTypeCache.Get( typeof( MinistrySafe ) ).Id;
                        if ( SharedHelper.SaveAttributeValue( workflow, "Report", $"{entityTypeId},{documentId}",
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
                        if ( SharedHelper.SaveAttributeValue( workflow, "ReportStatus", reportStatus,
                        FieldTypeCache.Get( Rock.SystemGuid.FieldType.SINGLE_SELECT.AsGuid() ), rockContext,
                        new Dictionary<string, string> { { "fieldtype", "ddl" }, { "values", "Pass,Fail,Review" } } ) )
                        {
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

                SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Workflow Updated. Processing now." );


                List<string> workflowErrors;
                workflowService.Process( workflow, out workflowErrors );
                _lockObjects.TryRemove( id, out _ ); // we no longer need that lock for this workflow
                SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Workflow Processing Complete." );

            }
        }

        internal static Guid? SaveFile( AttributeCache binaryFileAttribute, string url, string fileName )
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
        /// Sets the workflow RequestStatus attribute.
        /// </summary>
        /// <param name="workflow">The workflow.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="requestStatus">The request status.</param>
        internal static void UpdateWorkflowApplicantInterfaceUrl( Rock.Model.Workflow workflow, RockContext rockContext, string applicantInterfaceUrl )
        {
            if ( SharedHelper.SaveAttributeValue( workflow, "ApplicantInterfaceUrl", applicantInterfaceUrl,
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
        internal static void UpdateWorkflowRequestMessage( Rock.Model.Workflow workflow, RockContext rockContext, string requestMessage )
        {
            if ( SharedHelper.SaveAttributeValue( workflow, "RequestMessage", requestMessage,
                FieldTypeCache.Get( Rock.SystemGuid.FieldType.TEXT.AsGuid() ), rockContext, null ) )
            {
                rockContext.SaveChanges();
            }
        }

        #endregion

        #region Background Check Levels Methods

        /// <summary>
        /// Get the MinistrySafe packages and update the list on the server.
        /// </summary>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        public static bool UpdateAvailableLevels( List<string> errorMessages )
        {
            List<BackgroundCheckLevelV3> availableLevels;

            if ( !ApiHelper.GetAvailableLevels( out availableLevels, errorMessages ) )
            {
                return false;
            }

            if ( availableLevels == null )
            {
                availableLevels = new List<BackgroundCheckLevelV3>();
            }

            var defaultPackageResponseList = new List<PackageResponse>();
            var packageResponseList = new List<PackageResponse>();

            Dictionary<string, List<DefinedValue>> groupedPackageKeys;
            using ( var rockContext = new RockContext() )
            {
                var definedType = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.BACKGROUND_CHECK_TYPES.AsGuid() );

                DefinedValueService definedValueService = new DefinedValueService( rockContext );
                groupedPackageKeys = definedValueService
                    .GetByDefinedTypeGuid( Rock.SystemGuid.DefinedType.BACKGROUND_CHECK_TYPES.AsGuid() )
                    .Where( v => v.ForeignId == 4 )
                    .ToList()
                    .Select( v => { v.LoadAttributes( rockContext ); return v; } ) // v => v.Value.Substring( MinistrySafeConstants.TYPENAME_PREFIX.Length ) )
                    .GroupBy( v => v.GetAttributeValue( "MinistrySafePackageName" ).ToString() )
                    .ToDictionary( v => v.Key, v => v.ToList() );

                var userTypes = definedValueService
                     .GetByDefinedTypeGuid( "559E79C6-2EAB-4A0D-A16F-59D9B63F002F".AsGuid() )
                     .ToList();

                foreach ( var packageResponse in customPackageResponseList )
                {
                    string packageName = packageResponse.Name;
                    if ( !groupedPackageKeys.ContainsKey( packageName ) )
                    {
                        AddPackage( rockContext, definedType, definedValueService, packageResponse, null );
                    }

                    packageResponseList.Add( packageResponse );
                }

                foreach ( var packageResponse in defaultPackageResponseList )
                {
                    string packageName = packageResponse.Name;
                    if ( !groupedPackageKeys.ContainsKey( packageName ) )
                    {
                        foreach ( var userType in userTypes )
                        {
                            AddPackage( rockContext, definedType, definedValueService, packageResponse, userType );
                        }
                    }

                    packageResponseList.Add( packageResponse );
                }

                var packageRestResponseNames = packageResponseList.Select( pr => pr.Name );
                foreach ( var groupedPackageKey in groupedPackageKeys )
                {
                    var isPackageActive = packageRestResponseNames.Contains( groupedPackageKey.Key );
                    foreach ( var package in groupedPackageKey.Value )
                    {
                        package.IsActive = isPackageActive;
                    }
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
        internal static void AddPackage( RockContext rockContext, DefinedTypeCache definedType, DefinedValueService definedValueService, PackageResponse packageResponse, DefinedValue userType = null )
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
        internal static bool GetPackageName( RockContext rockContext, Rock.Model.Workflow workflow, AttributeCache requestTypeAttribute, out int? level, out string packageCode, out string userType, out string packageName, List<string> errorMessages )
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

            level = pkgTypeDefinedValue.GetAttributeValue( "MinistrySafePackageLevel" ).AsIntegerOrNull();
            packageCode = pkgTypeDefinedValue.GetAttributeValue( "MinistrySafePackageCode" );
            packageName = pkgTypeDefinedValue.Value;
            return true;
        }


        #endregion
    }
}

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
    internal class TrainingHelper
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

        /// <summary>
        /// Updates the survey types.
        /// </summary>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool UpdateTrainingTypes( List<string> errorMessages )
        {
            List<TrainingTypeV3> trainingTypeList;

            if ( !ApiHelper.GetTrainingTypes( 1, out trainingTypeList, errorMessages ) )
            {
                //return false;
            }

            if ( trainingTypeList == null )
            {
                trainingTypeList = new List<TrainingTypeV3>();
            }


            using ( var rockContext = new RockContext() )
            {
                // Update Step Types

                var stepTypeMapping = new Dictionary<int, Guid>();
                var stepProgram = StepProgramCache.Get( MinistrySafeSystemGuid.MINISTRYSAFE_TRAINING_PROGRAM.AsGuid() );

                if ( stepProgram == null )
                {
                    errorMessages.Add( "MinistrySafe Training Step Program not found. Please ensure the MinistrySafe migrations have run." );
                    return false;
                }

                var stepTypeService = new StepTypeService( rockContext );
                var stepProgramGuid = stepProgram.Guid;
                var stepTypeList = stepTypeService.Queryable().Where( st => st.StepProgram.Guid == stepProgramGuid ).ToList();

                // First, mark all existing codes as inactive
                foreach ( var stepType in stepTypeList )
                {
                    stepType.IsActive = false;
                }

                foreach ( var trainingType in trainingTypeList )
                {
                    foreach ( var trainingTypeLanguage in trainingType.Languages )
                    {
                        var stepType = stepTypeList
                            .Where( st => st.Name == trainingTypeLanguage.Description )
                            .FirstOrDefault();
                        if ( stepType == null )
                        {
                            stepType = new StepType()
                            {
                                IsActive = true,
                                StepProgramId = stepProgram.Id,
                                ForeignId = trainingTypeLanguage.Id,
                                Name = trainingTypeLanguage.Description
                            };

                            stepTypeService.Add( stepType );
                            rockContext.SaveChanges();
                        }

                        stepType.IsActive = true;
                        stepType.ForeignId = trainingTypeLanguage.Id;
                        stepType.Description = trainingTypeLanguage.Description;

                        stepType.LoadAttributes( rockContext );

                        stepType.SetAttributeValue( "Code", trainingType.ShortName );
                        stepType.SetAttributeValue( "Price", trainingTypeLanguage.Price );

                        stepType.SaveAttributeValues( rockContext );

                        // Add Score attribute to steps of this step type if it doesn't exist
                        var stepEntityTypeId = EntityTypeCache.Get( typeof( Step ) ).Id;
                        var attributeService = new AttributeService( rockContext );
                        var scoreAttribute = attributeService
                            .GetByEntityTypeQualifier( stepEntityTypeId, "StepTypeId", stepType.Id.ToString(), true )
                            .FirstOrDefault( a => a.Key == "Score" );

                        if ( scoreAttribute == null )
                        {
                            scoreAttribute = new Rock.Model.Attribute
                            {
                                EntityTypeId = stepEntityTypeId,
                                EntityTypeQualifierColumn = "StepTypeId",
                                EntityTypeQualifierValue = stepType.Id.ToString(),
                                Key = "Score",
                                Name = "Score",
                                FieldTypeId = FieldTypeCache.Get( Rock.SystemGuid.FieldType.INTEGER.AsGuid() ).Id,
                                IsRequired = false,
                                Order = 0
                            };
                            attributeService.Add( scoreAttribute );
                            rockContext.SaveChanges();
                        }

                        stepTypeMapping.Add( trainingTypeLanguage.Id, stepType.Guid );
                    }
                }

                // Update Defined Values

                var definedType = DefinedTypeCache.Get( MinistrySafeSystemGuid.MINISTRYSAFE_SURVEY_TYPES.AsGuid() );
                DefinedValueService definedValueService = new DefinedValueService( rockContext );
                var definedValueList = definedValueService
                    .GetByDefinedTypeGuid( definedType.Guid )
                    .ToList();

                // First, mark all existing codes as inactive
                foreach ( var definedValue in definedValueList )
                {
                    definedValue.IsActive = false;
                }

                foreach ( var trainingType in trainingTypeList )
                {
                    foreach ( var trainingTypeLanguage in trainingType.Languages )
                    {
                        var definedValue = definedValueList
                        .Where( dv => dv.ForeignId == trainingTypeLanguage.Id )
                        .FirstOrDefault();
                        if ( definedValue == null )
                        {
                            definedValue = new DefinedValue()
                            {
                                IsActive = true,
                                DefinedTypeId = definedType.Id,
                                ForeignId = trainingTypeLanguage.Id,
                                Value = trainingType.ShortName
                            };

                            definedValueService.Add( definedValue );
                            rockContext.SaveChanges();
                        }

                        definedValue.IsActive = true;
                        definedValue.ForeignId = trainingTypeLanguage.Id;
                        definedValue.Description = trainingTypeLanguage.Description;

                        definedValue.LoadAttributes( rockContext );

                        definedValue.SetAttributeValue( "Code", trainingType.ShortName );
                        definedValue.SetAttributeValue( "Price", trainingTypeLanguage.Price );

                        var stepTypeGuid = stepTypeMapping.GetValueOrNull( trainingTypeLanguage.Id );
                        if ( stepTypeGuid.HasValue )
                        {
                            definedValue.SetAttributeValue( "StepType", stepTypeGuid.ToString() );
                        }

                        definedValue.SaveAttributeValues( rockContext );
                    }
                }

                rockContext.SaveChanges();
            }

            DefinedValueCache.Clear();
            return true;
        }

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
                    if ( !UserHelper.GetPerson( rockContext, workflow, personAttribute, out person, out personAliasId, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to get Person." );
                        UpdateWorkflowTrainingStatus( workflow, rockContext, "FAIL" );
                        return true;
                    }

                    string surveyTypeCode;
                    if ( !GetSurveyTypeCode( rockContext, workflow, surveyTypeAttribute, out surveyTypeCode, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to get Survey Type." );
                        UpdateWorkflowTrainingStatus( workflow, rockContext, "FAIL" );
                        return true;
                    }

                    string userTypeName;
                    if ( !UserHelper.GetUserTypeName( rockContext, workflow, userTypeAttribute, out userTypeName, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to get User Type." );
                        UpdateWorkflowTrainingStatus( workflow, rockContext, "FAIL" );
                        return true;
                    }

                    string tagList = null;
                    if ( !UserHelper.GetTags( rockContext, workflow, out tagList, errorMessages ) )
                    {
                        workflow.AddLogEntry( "Unable to get Tags." );
                    }

                    int? userId;
                    if ( !UserHelper.GetOrCreateUser(
                        person, 
                        personAliasId.Value, 
                        userTypeName, 
                        tagList, 
                        out userId, 
                        errorMessages ) )
                    {
                        errorMessages.Add( "Unable to create user." );
                        UpdateWorkflowTrainingStatus( workflow, rockContext, "FAIL" );
                        return true;
                    }

                    string directLoginUrl;
                    if ( !AssignTraining( userId, surveyTypeCode, out directLoginUrl, errorMessages ) )
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
                        ministrySafeUser.SurveyCode = surveyTypeCode;
                        ministrySafeUser.UserType = userTypeName;
                        ministrySafeUser.RequestDate = RockDateTime.Now;
                        ministrySafeUser.DirectLoginUrl = directLoginUrl;
                        ministrySafeUser.UserId = userId.Value;
                        newRockContext.SaveChanges();
                    }

                    UpdateWorkflowTrainingStatus( workflow, rockContext, "SUCCESS" );

                    if ( SharedHelper.SaveAttributeValue( workflow, directLoginUrlAttribute.Key, directLoginUrl,
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

        public bool RefreshTraining( RockContext rockContext, Rock.Model.Workflow workflow,
                 AttributeCache personAttribute, AttributeCache directLoginUrlAttribute,
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
                    if ( !UserHelper.GetPerson( rockContext, workflow, personAttribute, out person, out personAliasId, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to get Person." );
                        UpdateWorkflowTrainingStatus( workflow, rockContext, "FAIL" );
                        return true;
                    }

                    UserResponse userResponse;
                    if ( !MinistrySafeApiUtility.GetUser( workflow, person, personAliasId.Value, out userResponse, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to get User." );
                        UpdateWorkflowTrainingStatus( workflow, rockContext, "FAIL" );
                        return true;
                    }

                    string trainingLink;
                    if ( !RefreshTraining( userResponse.Id, out trainingLink, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to refresh training link." );
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

                        if ( ministrySafeUser != null )
                        {
                            ministrySafeUser.DirectLoginUrl = trainingLink;
                            newRockContext.SaveChanges();
                        }
                    }

                    UpdateWorkflowTrainingStatus( workflow, rockContext, "SUCCESS" );

                    if ( SharedHelper.SaveAttributeValue( workflow, directLoginUrlAttribute.Key, trainingLink,
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
        internal static void UpdateTrainingWorkflow( int id, int? score, DateTime completedDateTime, RockContext rockContext, string surveyCode = null, int? personAliasId = null, int? interactionId = null )
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
                        if ( SharedHelper.SaveAttributeValue( workflow, "TrainingScore", score.ToString(),
                            FieldTypeCache.Get( Rock.SystemGuid.FieldType.INTEGER.AsGuid() ), rockContext ) )
                        {
                        }
                    }

                    if ( completedDateTime != null )
                    {
                        // Save the training date
                        if ( SharedHelper.SaveAttributeValue( workflow, "TrainingDate", completedDateTime.ToString(),
                        FieldTypeCache.Get( Rock.SystemGuid.FieldType.SINGLE_SELECT.AsGuid() ), rockContext ) )
                        {
                        }
                    }

                    // Set the Step Type if blank
                    StepType stepType = null;
                    if ( workflow.GetAttributeValue( "StepType" ).IsNullOrWhiteSpace() && surveyCode.IsNotNullOrWhiteSpace() )
                    {
                        var stepProgram = StepProgramCache.Get( MinistrySafeSystemGuid.MINISTRYSAFE_TRAINING_PROGRAM.AsGuid() );
                        if ( stepProgram != null )
                        {
                            var stepTypeService = new StepTypeService( rockContext );
                            stepType = stepTypeService.Queryable()
                                .Where( st => st.StepProgram.Guid == stepProgram.Guid )
                                .WhereAttributeValue( rockContext, "Code", surveyCode )
                                .FirstOrDefault();
                            if ( stepType != null )
                            {
                                var rawValue = string.Format( "{0}|{1}", stepProgram.Guid, stepType.Guid );
                                SharedHelper.SaveAttributeValue( workflow, "StepType", rawValue,
                                        FieldTypeCache.Get( Rock.SystemGuid.FieldType.STEP_PROGRAM_STEP_TYPE.AsGuid() ), rockContext );
                            }
                        }
                    }

                    // Set the legacy training type attribute if blank
                    if ( workflow.GetAttributeValue( "SurveyType" ).IsNullOrWhiteSpace() && surveyCode.IsNotNullOrWhiteSpace() )
                    {
                        var definedType = DefinedTypeCache.Get( MinistrySafeSystemGuid.MINISTRYSAFE_SURVEY_TYPES.AsGuid() );
                        DefinedValueCache matchingValue = null;
                        if ( definedType != null )
                        {
                            foreach ( var definedValue in definedType.DefinedValues )
                            {
                                // First, check for a matching Step Type attribute. This section will overwrite
                                // anything set by the following section, and will additionally exit out of the loop.
                                var stepTypeGuid = definedValue.GetAttributeValue( "StepType" ).AsGuidOrNull();
                                if ( stepTypeGuid != null &&
                                    stepType != null &&
                                    stepType.Guid == stepTypeGuid )
                                {
                                    matchingValue = definedValue;
                                    break;
                                }

                                // If there's no matching step type, check the legacy Code attribute on the defined value 
                                var definedValueCode = definedValue.GetAttributeValue( "Code" );
                                if ( definedValueCode == surveyCode )
                                {
                                    matchingValue = definedValue;
                                }
                            }
                        }

                        if ( matchingValue != null )
                        {
                            SharedHelper.SaveAttributeValue( workflow, "SurveyType", matchingValue.Guid.ToString(),
                                        FieldTypeCache.Get( Rock.SystemGuid.FieldType.DEFINED_VALUE.AsGuid() ), rockContext );
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

        /// <summary>
        /// Imports the trainings.
        /// </summary>
        /// <param name="dateRange">The date range.</param>
        /// <param name="workflowType">Type of the workflow.</param>
        /// <param name="relaunchCompletedWorkflows">if set to <c>true</c>, launch a new workflow if the existing workflow has already completed.</param>
        /// <param name="trainingsProcessed">The trainings processed.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal bool ImportTrainings( DateRange dateRange, WorkflowTypeCache workflowType, bool relaunchCompletedWorkflows, out int trainingsProcessed, out List<string> errorMessages )
        {
            var startDate = dateRange.Start;
            var endDate = dateRange.End;
            trainingsProcessed = 0;
            int pageNumber = 1;
            errorMessages = new List<string>();
            List<TrainingAttemptV3> getAllTrainingResponses;

            // Save Interaction storing information
            var errorMessage = string.Empty;
            int? interactionId = SharedHelper.CreateDebuggingInteraction( "Training Import", out errorMessage );
            if ( errorMessage.IsNotNullOrWhiteSpace() )
            {
                errorMessages.Add( errorMessage );
                return false;
            }

            if ( MinistrySafeApiUtility.GetAllTrainings( pageNumber, startDate, endDate, out getAllTrainingResponses, errorMessages ) )
            {
                SharedHelper.LogMessageToDebuggingInteraction(
                    interactionId,
                    String.Format(
                        "Pulled Page {0} of trainings from {1} to {2}",
                        pageNumber,
                        startDate,
                        endDate
                        )
                    );

                SharedHelper.LogMessageToDebuggingInteraction(
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
                        SharedHelper.LogMessageToDebuggingInteraction(
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
                            if ( UpdateTraining( externalId, userId, score, surveyCode, completedDateTime.Value, createdDateTime, workflowType, relaunchCompletedWorkflows, interactionId ) )
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
        internal static bool UpdateTrainingFromWebhook( TrainingAssignmentV3_Webhook trainingWebhook )
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
        /// <param name="relaunchCompletedWorkflows">if set to <c>true</c>, launch a new workflow if the existing workflow has already completed.</param>
        /// <param name="interactionId">The interaction identifier.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool UpdateTraining( string externalId, string userId, int? score, string surveyCode, DateTime completedDateTime, DateTime? createdDateTime, WorkflowTypeCache workflowTypeCache = null, bool relaunchCompletedWorkflows = false, int? interactionId = null )
        {
            var rockContext = new RockContext();
            var errorMessages = new List<string>();

            SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Searching for PersonAliasId." );
            int? personAliasId = null;
            int? workflowId = null;
            if ( externalId.IsNotNullOrWhiteSpace() )
            {
                var numericExternalId = externalId.RemoveAllNonNumericCharacters().AsIntegerOrNull();

                if ( numericExternalId != null )
                {
                    if ( externalId.Contains( "pa" ) )
                    {
                        var personAlias = new PersonAliasService( rockContext ).Get( numericExternalId.Value );
                        if ( personAlias != null )
                        {
                            personAliasId = personAlias.Id;
                        }
                    }
                    else
                    {
                        workflowId = numericExternalId;
                    }
                }
            }

            if ( personAliasId == null )
            {
                personAliasId = UserHelper.FindRockPerson( userId, rockContext, errorMessages );
            }

            if ( personAliasId == null )
            {
                SharedHelper.LogMessageToDebuggingInteraction( interactionId, string.Format( "Failed to find PersonAliasId for {0}. Skipping import.", externalId ) );
                return false;
            }

            SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Found PersonAliasId. Searching for Training." );

            var ministrySafeUserService = new MinistrySafeUserService( rockContext );
            var ministrySafeUsers = ministrySafeUserService
                .Queryable( "PersonAlias.Person" )
                .Where( m =>
                        (
                            ( workflowId != null && m.WorkflowId == workflowId && m.ForeignId == 3 ) ||
                            ( personAliasId != null && m.PersonAliasId == personAliasId && ( m.ForeignId == 2 || m.ForeignId == 4 ) )
                        )
                    )
                .OrderBy( m => m.CompletedDateTime.HasValue )
                .ThenByDescending( m => m.CompletedDateTime )
                .ThenByDescending( m => m.ResponseDate )
                .ToList();

            if ( ministrySafeUsers != null && ministrySafeUsers.Count > 0 )
            {
                SharedHelper.LogMessageToDebuggingInteraction( interactionId, String.Format( "Found {0} Matches: {1}",
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
                SharedHelper.LogMessageToDebuggingInteraction( interactionId, "No Matching Open Trainings. Creating New Record." );

                // Is it older than the most recent completed one?
                if ( latestUser != null )
                {
                    SharedHelper.LogMessageToDebuggingInteraction( interactionId, String.Format( "Using User Id {0} with CompletedDateTime {1} as Latest User", latestUser.Id, latestUser.CompletedDateTime ) );

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

                    SharedHelper.LogMessageToDebuggingInteraction( interactionId, String.Format(
                        "Existing CompletionDate of {0} {1} Imported CompletionDate of {2}"
                        , existingDateTime
                        , relationship
                        , importedDateTime ) );

                    SharedHelper.LogMessageToDebuggingInteraction( interactionId, String.Format(
                        "Existing CompletionDate.Ticks of {0} {1} Imported CompletionDate.Ticks of {2}"
                        , existingDateTime.Ticks
                        , relationship
                        , importedDateTime.Ticks ) );

                    if ( dateCompareResult <= 0 )
                    {
                        SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Existing user is up to date. Skipping import." );
                        return true;
                    }
                    else
                    {
                        SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Imported Training is newer. Proceeding with import." );
                    }
                }
                else
                {
                    SharedHelper.LogMessageToDebuggingInteraction( interactionId, "No previous user to compare to." );
                }

                var ministrySafeUser = new MinistrySafeUser();
                ministrySafeUserService.Add( ministrySafeUser );
                ministrySafeUser.PersonAliasId = personAliasId.Value;
                ministrySafeUser.ForeignId = 4;
                ministrySafeUser.SurveyCode = surveyCode;
                ministrySafeUser.RequestDate = createdDateTime ?? RockDateTime.Now;
                ministrySafeUser.UserId = userId.AsInteger();
                rockContext.SaveChanges();
                ministrySafeUser = ministrySafeUserService.Get( ministrySafeUser.Guid );
                ministrySafeUsers.Add( ministrySafeUser );

                SharedHelper.LogMessageToDebuggingInteraction( interactionId, String.Format( "New Record Created: Id {0}", ministrySafeUser.Id ) );
            }

            foreach ( var ministrySafeUser in ministrySafeUsers )
            {
                SharedHelper.LogMessageToDebuggingInteraction( interactionId, String.Format( "Updating Record: Id {0}", ministrySafeUser.Id ) );

                ministrySafeUser.Score = score;
                ministrySafeUser.CompletedDateTime = completedDateTime;
                ministrySafeUser.ResponseDate = RockDateTime.Now;
                ministrySafeUser.SurveyCode = surveyCode ?? ministrySafeUser.SurveyCode;

                SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Record Updated. Grabbing Workflow." );

                var workflowService = new WorkflowService( rockContext );
                Rock.Model.Workflow workflow = null;
                if ( ministrySafeUser.WorkflowId.HasValue )
                {
                    workflow = workflowService.Get( ministrySafeUser.WorkflowId.Value );
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
                    workflow = Rock.Model.Workflow.Activate( workflowTypeCache, ministrySafeUser?.PersonAlias?.Person?.FullName );
                    workflowService.Add( workflow );
                    rockContext.SaveChanges();
                    ministrySafeUser.WorkflowId = workflow.Id;
                    SharedHelper.LogMessageToDebuggingInteraction( interactionId, String.Format( "Created New Workflow Id: {0}.", workflow.Id ) );
                }

                rockContext.SaveChanges();

                SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Training Update Complete." );

                if ( ministrySafeUser.WorkflowId.HasValue && ministrySafeUser.WorkflowId > 0 )
                {
                    SharedHelper.LogMessageToDebuggingInteraction( interactionId, "Launching UpdateTrainingWorkflow method." );
                    UpdateTrainingWorkflow( ministrySafeUser.WorkflowId.Value, score, completedDateTime, rockContext, ministrySafeUser.SurveyCode, ministrySafeUser.PersonAliasId, interactionId );
                }
            }

            return true;
        }

        /// <summary>
        /// Sets the workflow RequestStatus attribute.
        /// </summary>
        /// <param name="workflow">The workflow.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="requestStatus">The request status.</param>
        internal void UpdateWorkflowTrainingStatus( Rock.Model.Workflow workflow, RockContext rockContext, string requestStatus )
        {
            if ( SharedHelper.SaveAttributeValue( workflow, "RequestStatus", requestStatus,
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
        internal bool GetSurveyTypeCode( RockContext rockContext, Rock.Model.Workflow workflow, AttributeCache surveyTypeAttribute, out string surveyCode, List<string> errorMessages )
        {
            surveyCode = null;
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

            surveyTypeDefinedValue.LoadAttributes();
            surveyCode = surveyTypeDefinedValue.GetAttributeValue( "Code" );

            if ( surveyCode.IsNullOrWhiteSpace() )
            {
                surveyCode = surveyTypeDefinedValue.Value;
            }

            return true;
        }

        /// <summary>
        /// Creates the invitation.
        /// </summary>
        /// <param name="candidateId">The candidate identifier.</param>
        /// <param name="surveyCode">The survey code.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        public static bool AssignTraining( int userId, int trainingId, out string directLoginUrl, List<string> errorMessages )
        {
            directLoginUrl = null;
            TrainingAttemptV3 assignTrainingResponse;
            if ( ApiHelper.AssignTraining( userId, trainingId, out assignTrainingResponse, errorMessages ) )
            {
                directLoginUrl = assignTrainingResponse.DirectLoginUrl;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates the invitation.
        /// </summary>
        /// <param name="candidateId">The candidate identifier.</param>
        /// <param name="surveyCode">The survey code.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        public static bool RefreshTraining( string candidateId, out string trainingLink, List<string> errorMessages )
        {
            trainingLink = null;
            RefreshTrainingResponse refreshTrainingResponse;
            if ( MinistrySafeApiUtility.RefreshTraining( candidateId, out refreshTrainingResponse, errorMessages ) )
            {
                trainingLink = refreshTrainingResponse.TrainingLink;
                return true;
            }

            return false;
        }

    }
}

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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using com.bemaservices.MinistrySafe.Constants;
using com.bemaservices.MinistrySafe.MinistrySafeApi.V2;
using com.bemaservices.MinistrySafe.MinistrySafeApi.V3;
using com.bemaservices.MinistrySafe.MinistrySafeApi.V3.BackgroundChecks;
using com.bemaservices.MinistrySafe.MinistrySafeApi.V3.Response;
using com.bemaservices.MinistrySafe.MinistrySafeApi.V3.Trainings;
using com.bemaservices.MinistrySafe.MinistrySafeApi.V3.Users;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi
{
    /// <summary>
    /// Class MinistrySafeApiUtility.
    /// </summary>
    internal static class ApiHelper
    {
        #region Utilities   

        /// <summary>
        /// Return a rest client.
        /// </summary>
        /// <returns>The rest client.</returns>
        private static RestClient RestClient( int apiVersion = 3 )
        {
            string apiKey = null;
            string serverUrl = null;
            using ( RockContext rockContext = new RockContext() )
            {
                var settings = MinistrySafe.GetSettings( rockContext );
                if ( settings != null )
                {
                    apiKey = MinistrySafe.GetSettingValue( settings, MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_ACCESS_TOKEN, true );
                    serverUrl = MinistrySafe.GetSettingValue( settings, MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_SERVER_URL, false );
                }
            }

            if ( apiKey.IsNullOrWhiteSpace() )
            {
                apiKey = GlobalAttributesCache.Value( "MinistrySafeAPIToken" );
            }

            var apiToken = string.Empty;
            if ( apiVersion >= 3 )
            {
                apiToken = string.Format( "Bearer {0}", apiKey );
            }
            else
            {
                apiToken = string.Format( "Token token={0}", apiKey );
            }

            var serverLink = serverUrl.IsNullOrWhiteSpace() ? MinistrySafeConstants.MINISTRYSAFE_APISERVER : serverUrl;
            var restClient = new RestClient( serverLink );

            restClient.AddDefaultHeader( "Authorization", apiToken );
            return restClient;
        }

        /// <summary>
        /// RestClient request to string for debugging purposes.
        /// </summary>
        /// <param name="restClient">The rest client.</param>
        /// <param name="restRequest">The rest request.</param>
        /// <returns>The RestClient Request in string format.</returns>
        // https://stackoverflow.com/questions/15683858/restsharp-print-raw-request-and-response-headers
        private static string RequestToString( RestClient restClient, RestRequest restRequest )
        {
            var requestToLog = new
            {
                resource = restRequest.Resource,
                // Parameters are custom anonymous objects in order to have the parameter type as a nice string
                // otherwise it will just show the enum value
                parameters = restRequest.Parameters.Select( parameter => new
                {
                    name = parameter.Name,
                    value = parameter.Value,
                    type = parameter.Type.ToString()
                } ),
                // ToString() here to have the method as a nice string otherwise it will just show the enum value
                method = restRequest.Method.ToString(),
                // This will generate the actual Uri used in the request
                uri = restClient.BuildUri( restRequest ),
            };
            return JsonConvert.SerializeObject( requestToLog );
        }

        /// <summary>
        /// RestClient response to string for debugging purposes.
        /// </summary>
        /// <param name="restResponse">The rest response.</param>
        /// <returns>The RestClient response in string format.</returns>
        // https://stackoverflow.com/questions/15683858/restsharp-print-raw-request-and-response-headers
        private static string ResponseToString( IRestResponse restResponse )
        {
            var responseToLog = new
            {
                statusCode = restResponse.StatusCode,
                content = restResponse.Content,
                headers = restResponse.Headers,
                // The Uri that actually responded (could be different from the requestUri if a redirection occurred)
                responseUri = restResponse.ResponseUri,
                errorMessage = restResponse.ErrorMessage,
            };

            return JsonConvert.SerializeObject( responseToLog );
        }

        #endregion

        #region V2 Methods

        /// <summary>
        /// Gets the tags.
        /// </summary>
        /// <param name="getTagsResponse">The get tags response.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool GetTags( out List<TagV2> getTagsResponse, List<string> errorMessages )
        {
            getTagsResponse = null;
            RestClient restClient = RestClient( apiVersion: 2 );
            RestRequest restRequest = new RestRequest( MinistrySafeConstants.MINISTRYSAFE_TAGS_URL );
            IRestResponse restResponse = restClient.Execute( restRequest );

            if ( restResponse.StatusCode == HttpStatusCode.Unauthorized )
            {
                errorMessages.Add( "Failed to authorize MinistrySafe. Please confirm your access token." );
                return false;
            }

            if ( restResponse.StatusCode != HttpStatusCode.OK )
            {
                errorMessages.Add( "Failed to get MinistrySafe Tags: " + restResponse.Content );
                return false;
            }

            getTagsResponse = JsonConvert.DeserializeObject<List<TagV2>>( restResponse.Content );
            if ( getTagsResponse == null )
            {
                errorMessages.Add( "Get Tags is not valid: " + restResponse.Content );
                return false;
            }

            return true;
        }

        #endregion

        #region V3 Methods

        #region Shared Methods
        #endregion

        #region Background Check Methods

        /// <summary>
        /// Gets the packages.
        /// </summary>
        /// <param name="getPackagesResponse">The get packages response.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        internal static bool GetAvailableLevels( out List<BackgroundCheckLevelV3> availableLevels, List<string> errorMessages )
        {
            availableLevels = null;
            RestClient restClient = RestClient();
            RestRequest restRequest = new RestRequest( MinistrySafeConstants.MINISTRYSAFE_PACKAGES_URL );
            IRestResponse restResponse = restClient.Execute( restRequest );

            if ( restResponse.StatusCode == HttpStatusCode.Unauthorized )
            {
                errorMessages.Add( "Failed to authorize MinistrySafe. Please confirm your access token." );
                return false;
            }

            if ( restResponse.StatusCode != HttpStatusCode.OK )
            {
                errorMessages.Add( "Failed to get MinistrySafe Packages: " + restResponse.Content );
                return false;
            }

            AvailableLevelsV3 levelList = JsonConvert.DeserializeObject<AvailableLevelsV3>( restResponse.Content );
            if ( levelList == null )
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponseV3>( restResponse.Content );
                if ( errorResponse != null )
                {
                    errorMessages.Add( errorResponse.Error + ": " + errorResponse.Message );
                }
                else
                {
                    errorMessages.Add( "Get Packages is not valid: " + restResponse.Content );
                }
                return false;
            }

            availableLevels = levelList.Levels;

            return true;
        }

        /// <summary>
        /// Gets the background check.
        /// </summary>
        /// <param name="backgroundCheckId">The background check identifier.</param>
        /// <param name="getBackgroundCheckV3">The get background check response.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool GetBackgroundCheck( int backgroundCheckId, out BackgroundCheckV3 getBackgroundCheckV3, List<string> errorMessages )
        {
            getBackgroundCheckV3 = null;
            RestClient restClient = RestClient();
            RestRequest restRequest = new RestRequest( String.Format( "{0}/{1}", MinistrySafeConstants.MINISTRYSAFE_BACKGROUNDCHECK_URL, backgroundCheckId ) );
            IRestResponse restResponse = restClient.Execute( restRequest );

            if ( restResponse.StatusCode == HttpStatusCode.Unauthorized )
            {
                errorMessages.Add( "Invalid MinistrySafe access token. To Re-authenticate go to Admin Tools > System Settings > MinistrySafe. Click edit to change your access token." );
                return false;
            }

            if ( restResponse.StatusCode != HttpStatusCode.OK )
            {
                errorMessages.Add( "Failed to get MinistrySafe Background Check: " + restResponse.Content );
                return false;
            }

            getBackgroundCheckV3 = JsonConvert.DeserializeObject<BackgroundCheckV3>( restResponse.Content );
            if ( getBackgroundCheckV3 == null )
            {
                errorMessages.Add( "Get Background Check is not valid: " + restResponse.Content );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Archives the background check.
        /// </summary>
        /// <param name="backgroundCheckId">The background check identifier.</param>
        /// <param name="archiveBackgroundCheckV3">The archive background check response.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool ArchiveBackgroundCheck( string backgroundCheckId, out BackgroundCheckV3 archiveBackgroundCheckV3, List<string> errorMessages )
        {
            archiveBackgroundCheckV3 = null;
            RestClient restClient = RestClient();
            RestRequest restRequest = new RestRequest( String.Format( "{0}/{1}/archive", MinistrySafeConstants.MINISTRYSAFE_BACKGROUNDCHECK_URL, backgroundCheckId ), Method.PUT );
            IRestResponse restResponse = restClient.Execute( restRequest );

            if ( restResponse.StatusCode == HttpStatusCode.Unauthorized )
            {
                errorMessages.Add( "Invalid MinistrySafe access token. To Re-authenticate go to Admin Tools > System Settings > MinistrySafe. Click edit to change your access token." );
                return false;
            }

            if ( restResponse.StatusCode != HttpStatusCode.OK )
            {
                errorMessages.Add( "Failed to archive MinistrySafe Background Check: " + restResponse.Content );
                return false;
            }

            archiveBackgroundCheckV3 = JsonConvert.DeserializeObject<BackgroundCheckV3>( restResponse.Content );
            if ( archiveBackgroundCheckV3 == null )
            {
                errorMessages.Add( "Archive Background Check is not valid: " + restResponse.Content );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets all background checks.
        /// </summary>
        /// <param name="pageNumber">The page number.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="getAllBackgroundCheckV3s">The get all background check responses.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool GetAllBackgroundChecks( int pageNumber,
            DateTime? startDate,
            DateTime? endDate,
            out List<BackgroundCheckV3> backgroundCheckList,
            List<string> errorMessages )
        {
            backgroundCheckList = new List<BackgroundCheckV3>();
            RestClient restClient = RestClient();
            RestRequest restRequest = new RestRequest( MinistrySafeConstants.MINISTRYSAFE_BACKGROUNDCHECK_URL, Method.GET );
            restRequest.AddParameter( "page", pageNumber );

            if ( startDate.HasValue )
            {
                restRequest.AddParameter( "filter[start_date]", startDate.ToShortDateString() );
            }

            if ( endDate.HasValue )
            {
                restRequest.AddParameter( "filter[end_date]", endDate.ToShortDateString() );
            }

            IRestResponse restResponse = restClient.Execute( restRequest );

            if ( restResponse.StatusCode == HttpStatusCode.Unauthorized )
            {
                errorMessages.Add( "Invalid MinistrySafe access token. To Re-authenticate go to Admin Tools > System Settings > MinistrySafe. Click edit to change your access token." );
                return false;
            }

            if ( restResponse.StatusCode != HttpStatusCode.OK )
            {
                errorMessages.Add( "Failed to get MinistrySafe Background Checks: " + restResponse.Content );
                return false;
            }

            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponseV3<BackgroundCheckV3>>( restResponse.Content );
            if ( paginatedResponse == null )
            {
                errorMessages.Add( "Get All Background Checks Response is not valid: " + restResponse.Content );
                return false;
            }

            backgroundCheckList.AddRange( paginatedResponse.Data );

            if ( paginatedResponse.Page < paginatedResponse.TotalPages )
            {
                List<BackgroundCheckV3> nextPageBackgroundChecks;
                if ( !GetAllBackgroundChecks( pageNumber + 1, startDate, endDate, out nextPageBackgroundChecks, errorMessages ) )
                {
                    return false;
                }
                backgroundCheckList.AddRange( nextPageBackgroundChecks );
            }

            return true;
        }

        /// <summary>
        /// Creates the background check.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="level">The level.</param>
        /// <param name="packageCode">The package code.</param>
        /// <param name="userType">Type of the user.</param>
        /// <param name="childServing">if set to <c>true</c> [child serving].</param>
        /// <param name="over13">if set to <c>true</c> [over13].</param>
        /// <param name="salaryRange">The salary range.</param>
        /// <param name="backgroundCheckResponse">The background check response.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool CreateQuickApp(
            int userId,
            int? level,
            string packageCode,
            string userType,
            bool? childServing,
            bool? over13,
            string salaryRange,
            out BackgroundCheckV3 backgroundCheckResponse,
            List<string> errorMessages )
        {
            backgroundCheckResponse = null;
            RestClient restClient = RestClient();
            RestRequest restRequest = new RestRequest( MinistrySafeConstants.MINISTRYSAFE_BACKGROUNDCHECK_URL, Method.POST );

            var backgroundCheckAssignment = new QuickAppAssignmentV3();
            backgroundCheckAssignment.UserId = userId;
            backgroundCheckAssignment.Level = level ?? 0;
            backgroundCheckAssignment.CustomBackgroundCheckPackageCode = packageCode.IsNotNullOrWhiteSpace() ? packageCode : null;
            backgroundCheckAssignment.UserType = userType.IsNotNullOrWhiteSpace() ? userType : null;
            backgroundCheckAssignment.SalaryRange = salaryRange.IsNotNullOrWhiteSpace() ? salaryRange : null;
            backgroundCheckAssignment.ChildServing = childServing ?? false;
            backgroundCheckAssignment.AgeOver13 = over13 ?? false;

            var requestWrapper = new
            {
                background_check = backgroundCheckAssignment
            };

            restRequest.AddJsonBody( requestWrapper );

            IRestResponse restResponse = restClient.Execute( restRequest );

            if ( restResponse.StatusCode == HttpStatusCode.Unauthorized )
            {
                errorMessages.Add( "Invalid MinistrySafe access token. To Re-authenticate go to Admin Tools > System Settings > MinistrySafe. Click edit to change your access token." );
                return false;
            }

            if ( restResponse.StatusCode != HttpStatusCode.Created )
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append( "Failed to create MinistrySafe Background Check for request." );
                using ( var rockContext = new RockContext() )
                {
                    var settings = MinistrySafe.GetSettings( rockContext );
                    if ( settings != null )
                    {
                        var enableDebugging = MinistrySafe.GetSettingValue( settings, MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_ENABLE_DEBUGGING, false ).AsBoolean();
                        if ( enableDebugging )
                        {
                            stringBuilder.AppendFormat( " Request:{0}"
                                , restRequest.Parameters
                                .Where( p => !p.Name.Contains( "Authorization" ) )
                                .Select( p => p.Name + ": " + p.Value )
                                .ToList()
                                .AsDelimited( ", " ) );
                        }
                    }
                }

                stringBuilder.AppendFormat( " Response:{0}", restResponse.Content );

                errorMessages.Add( stringBuilder.ToString() );
                return false;
            }

            backgroundCheckResponse = JsonConvert.DeserializeObject<BackgroundCheckV3>( restResponse.Content );
            if ( backgroundCheckResponse == null )
            {
                errorMessages.Add( "Create Background Check is not valid: " + restResponse.Content );
                return false;
            }

            return true;
        }

        #endregion

        #region Training Methods

        /// <summary>
        /// Gets the tags.
        /// </summary>
        /// <param name="getTagsResponse">The get tags response.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool GetTrainingTypes( int pageNumber,
            out List<TrainingTypeV3> trainingTypeList,
            List<string> errorMessages )
        {
            trainingTypeList = new List<TrainingTypeV3>();
            RestClient restClient = RestClient();
            RestRequest restRequest = new RestRequest( MinistrySafeConstants.MINISTRYSAFE_TRAININGS_URL );
            IRestResponse restResponse = restClient.Execute( restRequest );

            if ( restResponse.StatusCode == HttpStatusCode.Unauthorized )
            {
                errorMessages.Add( "Failed to authorize MinistrySafe. Please confirm your access token." );
                return false;
            }

            if ( restResponse.StatusCode != HttpStatusCode.OK )
            {
                errorMessages.Add( "Failed to get MinistrySafe Training Types: " + restResponse.Content );
                return false;
            }

            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponseV3<TrainingTypeV3>>( restResponse.Content );
            if ( paginatedResponse == null )
            {
                errorMessages.Add( "Get All Background Checks Response is not valid: " + restResponse.Content );
                return false;
            }

            trainingTypeList.AddRange( paginatedResponse.Data );

            if ( paginatedResponse.Page < paginatedResponse.TotalPages )
            {
                List<TrainingTypeV3> nextPageTrainingTypes;
                if ( !GetTrainingTypes( pageNumber + 1, out nextPageTrainingTypes, errorMessages ) )
                {
                    return false;
                }
                trainingTypeList.AddRange( nextPageTrainingTypes );
            }

            return true;
        }

        /// <summary>
        /// Creates the invitation.
        /// </summary>
        /// <param name="candidateId">The candidate identifier.</param>
        /// <param name="surveyCode">The survey code.</param>
        /// <param name="assignTrainingResponse">The assign training response.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        internal static bool AssignTraining(
            int userId,
            string trainingId,
            List<string> errorMessages )
        {
            RestClient restClient = RestClient();
            RestRequest restRequest = new RestRequest( String.Format( "{0}/{1}/assign", MinistrySafeConstants.MINISTRYSAFE_TRAININGS_URL, trainingId ), Method.POST );

            var trainingAssignment = new TrainingAssignmentV3();
            trainingAssignment.UserId = userId;
            trainingAssignment.SendEmail = false;
            trainingAssignment.UpfrontPayment = true;

            restRequest.AddJsonBody( trainingAssignment );

            IRestResponse restResponse = restClient.Execute( restRequest );

            if ( restResponse.StatusCode == HttpStatusCode.Unauthorized )
            {
                errorMessages.Add( "Invalid MinistrySafe access token. To Re-authenticate go to Admin Tools > System Settings > MinistrySafe. Click edit to change your access token." );
                return false;
            }

            if ( restResponse.StatusCode != HttpStatusCode.Created )
            {
                errorMessages.Add( "Failed to assign MinistrySafe Training: " + restResponse.Content );
                return false;
            }

            var assignTrainingResponse = JsonConvert.DeserializeObject<MessageResponseV3>( restResponse.Content );
            if ( assignTrainingResponse == null )
            {
                errorMessages.Add( "Assign Training Response is not valid: " + restResponse.Content );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets all trainings.
        /// </summary>
        /// <param name="pageNumber">The page number.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="getAllTrainingResponses">The get all training responses.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool GetAllTrainings(
            int pageNumber,
            DateTime? startDate,
            DateTime? endDate,
            int? userId,
            string trainingCode,
            out List<TrainingAttemptV3> trainingAttemptList,
            List<string> errorMessages )
        {
            trainingAttemptList = new List<TrainingAttemptV3>();
            RestClient restClient = RestClient();
            RestRequest restRequest = new RestRequest( MinistrySafeConstants.MINISTRYSAFE_TRAINING_ATTEMPTS_URL, Method.GET );
            restRequest.AddParameter( "page", pageNumber );

            if ( startDate.HasValue )
            {
                restRequest.AddParameter( "filter[start_date]", startDate.ToISO8601DateString() );
            }

            if ( endDate.HasValue )
            {
                restRequest.AddParameter( "filter[end_date]", endDate.ToISO8601DateString() );
            }

            if ( userId.HasValue )
            {
                restRequest.AddParameter( "filter[user_id]", userId.Value );
            }

            if ( trainingCode.IsNotNullOrWhiteSpace() )
            {
                restRequest.AddParameter( "filter[training_code]", trainingCode );
            }

            IRestResponse restResponse = restClient.Execute( restRequest );

            if ( restResponse.StatusCode == HttpStatusCode.Unauthorized )
            {
                errorMessages.Add( "Invalid MinistrySafe access token. To Re-authenticate go to Admin Tools > System Settings > MinistrySafe. Click edit to change your access token." );
                return false;
            }

            if ( restResponse.StatusCode != HttpStatusCode.OK )
            {
                errorMessages.Add( "Failed to get MinistrySafe Trainings: " + restResponse.Content );
                return false;
            }

            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponseV3<TrainingAttemptV3>>( restResponse.Content );
            if ( paginatedResponse == null )
            {
                errorMessages.Add( "Get All Trainings Response is not valid: " + restResponse.Content );
                return false;
            }

            trainingAttemptList.AddRange( paginatedResponse.Data );

            if ( paginatedResponse.Page < paginatedResponse.TotalPages )
            {
                List<TrainingAttemptV3> nextPageTrainingAttempts;
                if ( !GetAllTrainings( pageNumber + 1, startDate, endDate, userId, trainingCode, out nextPageTrainingAttempts, errorMessages ) )
                {
                    return false;
                }
                trainingAttemptList.AddRange( nextPageTrainingAttempts );
            }

            return true;
        }

        /// <summary>
        /// Resends the training.
        /// </summary>
        /// <param name="candidateId">The candidate identifier.</param>
        /// <param name="surveyCode">The survey code.</param>
        /// <param name="resendTrainingResponse">The resend training response.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool ResendTraining(
            int userId,
            string trainingId,
            List<string> errorMessages )
        {
            RestClient restClient = RestClient();
            RestRequest restRequest = new RestRequest( String.Format( "{0}/assign", MinistrySafeConstants.MINISTRYSAFE_TRAININGS_URL, trainingId ), Method.POST );

            var trainingAssignment = new TrainingAssignmentV3();
            trainingAssignment.UserId = userId;
            trainingAssignment.SendEmail = false;
            trainingAssignment.UpfrontPayment = true;

            restRequest.AddJsonBody( trainingAssignment );

            IRestResponse restResponse = restClient.Execute( restRequest );

            if ( restResponse.StatusCode == HttpStatusCode.Unauthorized )
            {
                errorMessages.Add( "Invalid MinistrySafe access token. To Re-authenticate go to Admin Tools > System Settings > MinistrySafe. Click edit to change your access token." );
                return false;
            }

            if ( restResponse.StatusCode != HttpStatusCode.Created )
            {
                errorMessages.Add( "Failed to Resend MinistrySafe Training: " + restResponse.Content );
                return false;
            }

            var resendTrainingResponse = JsonConvert.DeserializeObject<MessageResponseV3>( restResponse.Content );
            if ( resendTrainingResponse == null )
            {
                errorMessages.Add( "Resend Training Response is not valid: " + restResponse.Content );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the training for user.
        /// </summary>
        /// <param name="candidateId">The candidate identifier.</param>
        /// <param name="getReportResponse">The get report response.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool GetTrainingAttempt(
            string trainingAttemptId,
            out TrainingAttemptV3 trainingAttempt,
            List<string> errorMessages )
        {
            trainingAttempt = null;
            RestClient restClient = RestClient();
            RestRequest restRequest = new RestRequest( String.Format( "{0}/{1}", MinistrySafeConstants.MINISTRYSAFE_TRAINING_ATTEMPTS_URL, trainingAttemptId ) );
            IRestResponse restResponse = restClient.Execute( restRequest );

            if ( restResponse.StatusCode == HttpStatusCode.Unauthorized )
            {
                errorMessages.Add( "Invalid MinistrySafe access token. To Re-authenticate go to Admin Tools > System Settings > MinistrySafe. Click edit to change your access token." );
                return false;
            }

            if ( restResponse.StatusCode != HttpStatusCode.OK )
            {
                errorMessages.Add( "Failed to get MinistrySafe Training: " + restResponse.Content );
                return false;
            }

            trainingAttempt = JsonConvert.DeserializeObject<TrainingAttemptV3>( restResponse.Content );
            if ( trainingAttempt == null )
            {
                errorMessages.Add( "Get Training is not valid: " + restResponse.Content );
                return false;
            }

            return true;
        }

        #endregion

        #region User Methods

        /// <summary>
        /// Gets the packages.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="getUserResponse">The get user response.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        internal static bool GetUser( string userId,
            out UserV3 user,
            List<string> errorMessages )
        {
            user = null;
            RestClient restClient = RestClient();
            RestRequest restRequest = new RestRequest( String.Format( "{0}/{1}?include=trainings,background_checks", MinistrySafeConstants.MINISTRYSAFE_USERS_URL, userId ) );
            IRestResponse restResponse = restClient.Execute( restRequest );

            if ( restResponse.StatusCode == HttpStatusCode.Unauthorized )
            {
                errorMessages.Add( "Failed to authorize MinistrySafe. Please confirm your access token." );
                return false;
            }

            if ( restResponse.StatusCode != HttpStatusCode.OK )
            {
                errorMessages.Add( "Failed to get MinistrySafe User: " + restResponse.Content );
                return false;
            }

            user = JsonConvert.DeserializeObject<UserV3>( restResponse.Content );
            if ( user == null )
            {
                errorMessages.Add( "Get User is not valid: " + restResponse.Content );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the packages.
        /// </summary>
        /// <param name="getUsersResponse">The get users response.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        internal static bool GetUsers( int pageNumber,
            string externalId,
            out List<UserV3> userList,
            List<string> errorMessages )
        {
            userList = null;
            RestClient restClient = RestClient();
            RestRequest restRequest = new RestRequest( MinistrySafeConstants.MINISTRYSAFE_USERS_URL );

            if ( externalId.IsNotNullOrWhiteSpace() )
            {
                restRequest.AddParameter( "filter[external_id]", externalId );
            }

            restRequest.AddParameter( "include", "trainings,background_checks" );

            IRestResponse restResponse = restClient.Execute( restRequest );

            if ( restResponse.StatusCode == HttpStatusCode.Unauthorized )
            {
                errorMessages.Add( "Failed to authorize MinistrySafe. Please confirm your access token." );
                return false;
            }

            if ( restResponse.StatusCode != HttpStatusCode.OK )
            {
                errorMessages.Add( "Failed to get MinistrySafe Users: " + restResponse.Content );
                return false;
            }

            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponseV3<UserV3>>( restResponse.Content );
            if ( paginatedResponse == null )
            {
                errorMessages.Add( "Get All Trainings Response is not valid: " + restResponse.Content );
                return false;
            }

            userList.AddRange( paginatedResponse.Data );

            if ( paginatedResponse.Page < paginatedResponse.TotalPages )
            {
                List<UserV3> nextPageUsers;
                if ( !GetUsers( pageNumber + 1, externalId, out nextPageUsers, errorMessages ) )
                {
                    return false;
                }
                userList.AddRange( nextPageUsers );
            }

            return true;
        }

        /// <summary>
        /// Creates the candidate.
        /// </summary>
        /// <param name="workflow">The workflow.</param>
        /// <param name="person">The person.</param>
        /// <param name="personAliasId">The person alias identifier.</param>
        /// <param name="userType">Type of the user.</param>
        /// <param name="tagList">The tag list.</param>
        /// <param name="createUserResponse">The create user response.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>True/False value of whether the request was successfully sent or not.</returns>
        internal static bool CreateUser(
            Person person,
            int personAliasId,
            string userType,
            string tagList,
            out UserV3 createUserResponse,
            List<string> errorMessages )
        {
            var firstName = person.FirstName;
            var lastName = person.LastName;
            var email = person.Email;
            var externalId = "pa" + personAliasId.ToString();

            return CreateUser( firstName, lastName, email, externalId, null, userType, tagList, out createUserResponse, errorMessages );
        }

        internal static bool CreateUser(
            string firstName,
            string lastName,
            string email,
            string externalId,
            string role,
            string userType,
            string tagList,
            out UserV3 createUserResponse,
            List<string> errorMessages )
        {
            createUserResponse = null;
            RestClient restClient = RestClient();
            RestRequest restRequest = new RestRequest( MinistrySafeConstants.MINISTRYSAFE_USERS_URL, Method.POST );

            var createUserRequest = new UserV3()
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                ExternalId = externalId
            };

            if ( role.IsNotNullOrWhiteSpace() )
            {
                createUserRequest.Role = role;
            }

            if ( userType.IsNotNullOrWhiteSpace() )
            {
                createUserRequest.UserType = userType;
            }

            if ( tagList.IsNotNullOrWhiteSpace() )
            {
                createUserRequest.Tags = tagList.SplitDelimitedValues();
            }

            restRequest.AddJsonBody( new
            {
                user = createUserRequest
            } );

            IRestResponse restResponse = restClient.Execute( restRequest );

            if ( restResponse.StatusCode == HttpStatusCode.Unauthorized )
            {
                errorMessages.Add( "Invalid MinistrySafe access token. To Re-authenticate go to Admin Tools > System Settings > MinistrySafe. Click edit to change your access token." );
                return false;
            }

            if ( restResponse.StatusCode != HttpStatusCode.Created )
            {
                errorMessages.Add( "Failed to create MinistrySafe User: " + restResponse.Content );
                return false;
            }

            createUserResponse = JsonConvert.DeserializeObject<UserV3>( restResponse.Content );
            if ( createUserResponse == null )
            {
                errorMessages.Add( "Create User Response is not valid: " + restResponse.Content );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the user.
        /// </summary>
        /// <param name="workflow">The workflow.</param>
        /// <param name="person">The person.</param>
        /// <param name="personAliasId">The person alias identifier.</param>
        /// <param name="userResponse">The user response.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool GetUserByExternalId(
            string externalId,
            out UserV3 user,
            List<string> errorMessages )
        {
            user = null;
            List<UserV3> userList = new List<UserV3>();
            if ( !GetUsers( 1, externalId, out userList, errorMessages ) )
            {
                return false;
            }

            user = userList.FirstOrDefault();

            return user != null;
        }

        /// <summary>
        /// Updates the user.
        /// </summary>
        /// <param name="candidateId">The candidate identifier.</param>
        /// <param name="email">The email.</param>
        /// <param name="tagList">The tag list.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool UpdateUser(
            int userId,
            string firstName,
            string lastName,
            string email,
            string externalId,
            string role,
            string userType,
            string tagList,
            out UserV3 userResponse,
            out List<string> errorMessages )
        {
            userResponse = null;
            errorMessages = new List<string>();
            RestClient restClient = RestClient();
            RestRequest restRequest = new RestRequest( String.Format( "{0}/{1}", MinistrySafeConstants.MINISTRYSAFE_USERS_URL, userId ), Method.PATCH );

            var userRequest = new UserV3();

            if ( firstName.IsNotNullOrWhiteSpace() )
            {
                userRequest.FirstName = firstName;
            }

            if ( lastName.IsNotNullOrWhiteSpace() )
            {
                userRequest.LastName = lastName;
            }

            if ( email.IsNotNullOrWhiteSpace() )
            {
                userRequest.Email = email;
            }

            if ( externalId.IsNotNullOrWhiteSpace() )
            {
                userRequest.ExternalId = externalId;
            }

            if ( role.IsNotNullOrWhiteSpace() )
            {
                userRequest.Role = role;
            }

            if ( userType.IsNotNullOrWhiteSpace() )
            {
                userRequest.UserType = userType;
            }

            if ( tagList.IsNotNullOrWhiteSpace() )
            {
                userRequest.Tags = tagList.SplitDelimitedValues();
            }

            restRequest.AddJsonBody( new
            {
                user = userRequest
            } );

            IRestResponse restResponse = restClient.Execute( restRequest );

            if ( restResponse.StatusCode == HttpStatusCode.Unauthorized )
            {
                errorMessages.Add( "Invalid MinistrySafe access token. To Re-authenticate go to Admin Tools > System Settings > MinistrySafe. Click edit to change your access token." );
                return false;
            }

            if ( restResponse.StatusCode != HttpStatusCode.OK )
            {
                errorMessages.Add( "Failed to update MinistrySafe User: " + restResponse.Content );
                return false;
            }

            userResponse = JsonConvert.DeserializeObject<UserV3>( restResponse.Content );
            if ( userResponse == null )
            {
                errorMessages.Add( "Update User Response is not valid: " + restResponse.Content );
                return false;
            }

            return true;
        }
        #endregion

        #endregion   
    }
}

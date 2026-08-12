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
    internal class UserHelper
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
        /// Finds the rock person.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>System.String.</returns>
        internal static int? FindRockPerson( string userId, RockContext rockContext, List<string> errorMessages )
        {
            var externalId = string.Empty;
            UserV3 user = null;
            if ( ApiHelper.GetUser( userId, out user, errorMessages ) )
            {
                // Find Existing Match
                if ( user.ExternalId.IsNotNullOrWhiteSpace() )
                {
                    var numericExternalId = externalId.RemoveAllNonNumericCharacters().AsIntegerOrNull();

                    if ( numericExternalId != null )
                    {
                        if ( externalId.Contains( "pa" ) )
                        {
                            var personAlias = new PersonAliasService( rockContext ).Get( numericExternalId.Value );
                            if ( personAlias != null )
                            {
                                return personAlias.Id;
                            }
                        }
                    }
                }

                // Find Rock Match
                var personService = new PersonService( rockContext );
                var personQuery = new PersonService.PersonMatchQuery( user.FirstName, user.LastName, user.Email, null );
                var person = personService.FindPerson( personQuery, false );

                if ( person == null )
                {
                    // Add New Person
                    person = new Person();
                    person.FirstName = user.FirstName.FixCase();
                    person.LastName = user.LastName.FixCase();
                    person.IsEmailActive = true;
                    person.Email = user.Email;
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
                    return person.PrimaryAliasId;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the type of the user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>System.String.</returns>
        internal static string FindUserType( string userId, RockContext rockContext, List<string> errorMessages )
        {
            var userType = string.Empty;
            UserV3 user = null;
            if ( !ApiHelper.GetUser( userId, out user, errorMessages ) )
            {
                userType = user.UserType;
            }

            return userType;
        }
        /// <summary>
        /// Gets the child serving.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="workflow">The workflow.</param>
        /// <param name="childServing">if set to <c>true</c> [child serving].</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool GetChildServing( RockContext rockContext, Rock.Model.Workflow workflow, out bool? childServing, List<string> errorMessages )
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
        internal static bool GetOverThirteen( RockContext rockContext, Rock.Model.Workflow workflow, out bool? over13, List<string> errorMessages )
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
        internal static bool GetSalaryRange( RockContext rockContext, Rock.Model.Workflow workflow, out string salaryRange, List<string> errorMessages )
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
        internal static bool GetEmployeeType( RockContext rockContext, Rock.Model.Workflow workflow, out string employeeType, List<string> errorMessages )
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
        /// Gets the name of the user type.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="workflow">The workflow.</param>
        /// <param name="userTypeAttribute">The user type attribute.</param>
        /// <param name="userTypeName">Name of the user type.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool GetUserTypeName( RockContext rockContext, Rock.Model.Workflow workflow, AttributeCache userTypeAttribute, out string userTypeName, List<string> errorMessages )
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
        public static bool GetOrCreateUser(
            Person person,
            int personAliasId,
            string userTypeName,
            string tagList,
            out int? candidateId,
            List<string> errorMessages )
        {
            UserV3 userResponse;
            candidateId = null;
            var externalId = "pa" + personAliasId.ToString();
            if ( ApiHelper.GetUserByExternalId( externalId, out userResponse, errorMessages ) )
            {
                candidateId = userResponse.Id;

                if ( tagList.IsNotNullOrWhiteSpace() )
                {
                    if ( ApiHelper.UpdateUser( candidateId.Value,
                        null, // person.FirstName
                        null, // person.LastName
                        person.Email,
                        null, // externalId
                        null, // role
                        null, // userType
                        tagList,
                        out userResponse,
                        out errorMessages ) )
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
                if ( ApiHelper.CreateUser( person, personAliasId, userTypeName, tagList, out userResponse, errorMessages ) )
                {
                    candidateId = userResponse.Id;
                    return true;
                }
            }

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
        internal static bool GetUserTags( RockContext rockContext, Rock.Model.Workflow workflow, AttributeCache personAttribute, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            try
            {
                // Check to make sure workflow is not null
                if ( workflow == null )
                {
                    errorMessages.Add( "The 'MinistrySafe' provider requires a valid workflow." );
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
                        SharedHelper.UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                        return true;
                    }

                    string tagList = null;
                    if ( !GetTags( rockContext, workflow, out tagList, errorMessages ) )
                    {
                        errorMessages.Add( "Unable to get Tags." );
                        SharedHelper.UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
                        return true;
                    }

                    UserV3 userResponse;
                    if ( ApiHelper.GetUserByExternalId( "pa" + person.PrimaryAliasId, out userResponse, errorMessages ) )
                    {
                        var rockTagList = tagList.SplitDelimitedValues().ToList();
                        var ministrySafeTagList = userResponse.Tags;
                        rockTagList.AddRange( ministrySafeTagList );
                        var newTagDefinedValueGuids = DefinedTypeCache.Get( com.bemaservices.MinistrySafe.Constants.MinistrySafeSystemGuid.MINISTRYSAFE_TAGS ).DefinedValues
                            .Where( dv => rockTagList.Contains( dv.Value ) )
                            .Select( dv => dv.Guid )
                            .ToList()
                            .AsDelimited( "," );

                        if ( SharedHelper.SaveAttributeValue( workflow, "UserTags", newTagDefinedValueGuids,
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
                SharedHelper.UpdateWorkflowRequestStatus( workflow, rockContext, "FAIL" );
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
        internal static bool GetTags( RockContext rockContext, Rock.Model.Workflow workflow, out string tagList, List<string> errorMessages )
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
        /// Updates the tags.
        /// </summary>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool UpdateTags( List<string> errorMessages )
        {
            List<TagV2> tagResponseList;

            if ( !ApiHelper.GetTags( out tagResponseList, errorMessages ) )
            {
                //return false;
            }

            if ( tagResponseList == null )
            {
                tagResponseList = new List<TagV2>();
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
        /// Gets the person that is currently logged in.
        /// </summary>
        /// <returns>Person.</returns>
        internal static Person GetCurrentPerson()
        {
            using ( var rockContext = new RockContext() )
            {
                var currentUser = new UserLoginService( rockContext ).GetByUserName( UserLogin.GetCurrentUserName() );
                return currentUser != null ? currentUser.Person : null;
            }
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
        internal static bool GetPerson( RockContext rockContext, Rock.Model.Workflow workflow, AttributeCache personAttribute, out Person person, out int? personAliasId, List<string> errorMessages )
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


    }
}

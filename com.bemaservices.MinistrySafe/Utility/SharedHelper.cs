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
    public static class SharedHelper
    {
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
            TrainingAttemptV3 trainingWebhook = JsonConvert.DeserializeObject<TrainingAttemptV3>( postedData, new JsonSerializerSettings()
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
                return TrainingHelper.UpdateTrainingFromWebhook( trainingWebhook );
            }

            // Try casting as Background Check
            BackgroundCheckV3_Webhook backgroundCheckWebhook = JsonConvert.DeserializeObject<BackgroundCheckV3_Webhook>( postedData, new JsonSerializerSettings()
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
                return BackgroundCheckHelper.UpdateBackgroundCheckFromWebhook( backgroundCheckWebhook, interactionId );
            }

            // Return Invalid Data
            responseMessage = "Webhook data is not valid: " + postedData;
            Rock.Model.ExceptionLogService.LogException( new Exception( responseMessage ), null );
            return false;
        }

        #region Settings

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
        internal static bool SaveAttributeValue( Rock.Model.Workflow workflow, string key, string value,
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

        #endregion

        #region Debugging Methods

        /// <summary>
        /// Logs the errors.
        /// </summary>
        /// <param name="errorMessages">The error messages.</param>
        internal static void LogErrors( List<string> errorMessages )
        {
            if ( errorMessages.Any() )
            {
                foreach ( string errorMsg in errorMessages )
                {
                    ExceptionLogService.LogException( new Exception( "MinistrySafe Error: " + errorMsg ), null );
                }
            }
        }

        internal static int? CreateDebuggingInteraction( string componentName, out string errorMessage )
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

        internal static void LogMessageToDebuggingInteraction( int? interactionId, string logMessage )
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

        #endregion

        #region Workflow

        /// <summary>
        /// Sets the workflow RequestStatus attribute.
        /// </summary>
        /// <param name="workflow">The workflow.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="requestStatus">The request status.</param>
        internal static void UpdateWorkflowRequestStatus( Rock.Model.Workflow workflow, RockContext rockContext, string requestStatus )
        {
            if ( SharedHelper.SaveAttributeValue( workflow, "RequestStatus", requestStatus,
                FieldTypeCache.Get( Rock.SystemGuid.FieldType.TEXT.AsGuid() ), rockContext, null ) )
            {
                rockContext.SaveChanges();
            }
        }

        #endregion
    }
}

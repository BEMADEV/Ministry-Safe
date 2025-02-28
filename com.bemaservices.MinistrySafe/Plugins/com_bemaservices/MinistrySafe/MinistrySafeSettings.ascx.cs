// <copyright>
// Copyright by the Spark Development Network
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
using System.ComponentModel;
using System.Linq;
using System.Web.UI;

using Rock;
using com.bemaservices.MinistrySafe;
using com.bemaservices.MinistrySafe.Constants;
using Rock.Data;
using Rock.Migrations;
using Rock.Model;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Security;
using Rock.SystemKey;
using Rock.Checkr.Constants;

namespace RockWeb.Plugins.com_bemaservices.MinistrySafe
{
    [DisplayName( "MinistrySafe Settings" )]
    [Category( "BEMA Services > MinistrySafe" )]
    [Description( "Block for updating the settings used by the MinistrySafe plugin." )]

    public partial class MinistrySafeSettings : Rock.Web.UI.RockBlock
    {
        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                ShowDetail();
            }
        }

        #endregion

        #region Events
        /// <summary>
        /// Handles the Click event of the btnEdit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnEdit_Click( object sender, EventArgs e )
        {
            nbNotification.Visible = false;
            pnlToken.Visible = true;
            pnlPackages.Visible = false;
            HideSecondaryBlocks( true );
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var settings = com.bemaservices.MinistrySafe.MinistrySafe.GetSettings( rockContext );
                com.bemaservices.MinistrySafe.MinistrySafe.SetSettingValue( rockContext, settings, MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_ACCESS_TOKEN, tbAccessToken.Text, true );
                com.bemaservices.MinistrySafe.MinistrySafe.SetSettingValue( rockContext, settings, MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_SERVER_URL, tbServerUrl.Text, false );
                com.bemaservices.MinistrySafe.MinistrySafe.SetSettingValue( rockContext, settings, MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_ENABLE_DEBUGGING, cbEnableDebugging.Checked.ToString(), false );

                rockContext.SaveChanges();

                BackgroundCheckContainer.Instance.Refresh();
            }

            if ( IsDefaultProvider() )
            {
                btnUpdate_Click( null, null );
            }

            btnUpdateTags_Click( null, null );

            btnUpdateSurveyTypes_Click( null, null );

            pnlToken.Visible = false;
            pnlPackages.Visible = true;
            HideSecondaryBlocks( false );
            ShowDetail();
        }

        /// <summary>
        /// Handles the Click event of the btnUpdate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnUpdate_Click( object sender, EventArgs e )
        {
            nbNotification.Visible = false;

            List<string> errorMessages = new List<string>();
            if ( !com.bemaservices.MinistrySafe.MinistrySafe.UpdatePackages( errorMessages ) )
            {
                nbNotification.Text = "<p>" + errorMessages.AsDelimited( "</p><p>" ) + "</p>";
                nbNotification.Visible = true;
                foreach ( string errorMessage in errorMessages )
                {
                    ExceptionLogService.LogException( new Exception( errorMessage ), null );
                }
            }

            DisplayPackages();
            DisplayTags();
            DisplaySurveyTypes();
            if ( sender != null )
            {
                maUpdated.Show( "Update Packages Complete.", ModalAlertType.Information );
            }
        }
        protected void btnUpdateTags_Click( object sender, EventArgs e )
        {
            nbNotification.Visible = false;

            List<string> errorMessages = new List<string>();
            if ( !com.bemaservices.MinistrySafe.MinistrySafe.UpdateTags( errorMessages ) )
            {
                nbNotification.Text = "<p>" + errorMessages.AsDelimited( "</p><p>" ) + "</p>";
                nbNotification.Visible = true;
                foreach ( string errorMessage in errorMessages )
                {
                    ExceptionLogService.LogException( new Exception( errorMessage ), null );
                }
            }

            DisplayTags();
            if ( sender != null )
            {
                maUpdated.Show( "Update Tags Complete.", ModalAlertType.Information );
            }
        }
        protected void btnUpdateSurveyTypes_Click( object sender, EventArgs e )
        {
            nbNotification.Visible = false;

            List<string> errorMessages = new List<string>();
            if ( !com.bemaservices.MinistrySafe.MinistrySafe.UpdateSurveyTypes( errorMessages ) )
            {
                nbNotification.Text = "<p>" + errorMessages.AsDelimited( "</p><p>" ) + "</p>";
                nbNotification.Visible = true;
                foreach ( string errorMessage in errorMessages )
                {
                    ExceptionLogService.LogException( new Exception( errorMessage ), null );
                }
            }

            DisplaySurveyTypes();
            if ( sender != null )
            {
                maUpdated.Show( "Update Tags Complete.", ModalAlertType.Information );
            }
        }

        /// <summary>
        /// Handles the Click event of the btnDefault control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnDefault_Click( object sender, EventArgs e )
        {
            var bioBlock = BlockCache.Get( Rock.SystemGuid.Block.BIO.AsGuid() );
            // Record an exception if the stock Bio block has been deleted but continue processing
            // the remaining settings.
            if ( bioBlock == null )
            {
                var errorMessage = string.Format( "Stock Bio block ({0}) is missing.", Rock.SystemGuid.Block.BIO );
                ExceptionLogService.LogException( new Exception( errorMessage ) );
            }
            else
            {
                List<Guid> workflowActionGuidList = bioBlock.GetAttributeValues( "WorkflowActions" ).AsGuidList();
                if ( workflowActionGuidList == null || workflowActionGuidList.Count == 0 )
                {
                    // Add MinistrySafe to Bio Workflow Actions
                    bioBlock.SetAttributeValue( "WorkflowActions", MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_WORKFLOW_TYPE );
                }
                else
                {
                    Guid guid = MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_WORKFLOW_TYPE.AsGuid();
                    if ( !workflowActionGuidList.Any( w => w == guid ) )
                    {
                        // Add MinistrySafe to Bio Workflow Actions
                        workflowActionGuidList.Add( guid );
                    }

                    // Remove PMM and Checkr from Bio Workflow Actions
                    guid = Rock.SystemGuid.WorkflowType.PROTECTMYMINISTRY.AsGuid();
                    workflowActionGuidList.RemoveAll( w => w == guid );

                    guid = CheckrSystemGuid.CHECKR_WORKFLOW_TYPE.AsGuid();
                    workflowActionGuidList.RemoveAll( w => w == guid );

                    bioBlock.SetAttributeValue( "WorkflowActions", workflowActionGuidList.AsDelimited( "," ) );
                }

                bioBlock.SaveAttributeValue( "WorkflowActions" );
            }

            string ministrySafeTypeName = ( typeof( com.bemaservices.MinistrySafe.MinistrySafe ) ).FullName;
            var ministrySafeComponent = BackgroundCheckContainer.Instance.Components.Values.FirstOrDefault( c => c.Value.TypeName == ministrySafeTypeName );
            ministrySafeComponent.Value.SetAttributeValue( "Active", "True" );
            ministrySafeComponent.Value.SaveAttributeValue( "Active" );
            // Set as the default provider in the system setting
            SystemSettings.SetValue( Rock.SystemKey.SystemSetting.DEFAULT_BACKGROUND_CHECK_PROVIDER, ministrySafeTypeName );

            using ( var rockContext = new RockContext() )
            {
                WorkflowTypeService workflowTypeService = new WorkflowTypeService( rockContext );
                // Rename PMM Workflow
                var pmmWorkflowAction = workflowTypeService.Get( Rock.SystemGuid.WorkflowType.PROTECTMYMINISTRY.AsGuid() );
                pmmWorkflowAction.Name = Checkr_CreatePages.NEW_PMM_WORKFLOW_TYPE_NAME;

                // Rename Checkr Workflow
                var checkrWorkflowAction = workflowTypeService.Get( CheckrSystemGuid.CHECKR_WORKFLOW_TYPE.AsGuid() );
                checkrWorkflowAction.Name = "Background Check (Checkr)";

                var ministrySafeWorkflowAction = workflowTypeService.Get( MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_WORKFLOW_TYPE.AsGuid() );
                // Rename MinistrySafe Workflow
                ministrySafeWorkflowAction.Name = "Background Check";

                rockContext.SaveChanges();

                // Enable MinistrySafe packages and disable PMM and Checkr packages
                DefinedValueService definedValueService = new DefinedValueService( rockContext );
                var packages = definedValueService
                    .GetByDefinedTypeGuid( Rock.SystemGuid.DefinedType.BACKGROUND_CHECK_TYPES.AsGuid() )
                    .ToList();

                foreach ( var package in packages )
                {
                    package.IsActive = false;
                }

                rockContext.SaveChanges();
            }

            btnUpdate_Click( null, null );

            ShowDetail();
        }
        #endregion

        #region Internal Methods

        /// <summary>
        /// Display the packages.
        /// </summary>
        private void DisplayPackages()
        {
            using ( var rockContext = new RockContext() )
            {
                var packages = new DefinedValueService( rockContext )
                    .GetByDefinedTypeGuid( Rock.SystemGuid.DefinedType.BACKGROUND_CHECK_TYPES.AsGuid() )
                    .Where( v => v.ForeignId == 4 && v.IsActive )
                    .Select( v => v.Value.Substring( MinistrySafeConstants.MINISTRYSAFE_TYPENAME_PREFIX.Length ) )
                    .ToList();

                lPackages.Text = packages.AsDelimited( "<br/>" );
            }
        }

        private void DisplayTags()
        {
            using ( var rockContext = new RockContext() )
            {
                var tags = new DefinedValueService( rockContext )
                    .GetByDefinedTypeGuid( MinistrySafeSystemGuid.MINISTRYSAFE_TAGS.AsGuid() )
                    .Where( v => v.ForeignId == 4 && v.IsActive )
                    .Select( v => v.Value )
                    .ToList();

                lTags.Text = tags.AsDelimited( "<br/>" );
            }
        }

        private void DisplaySurveyTypes()
        {
            using ( var rockContext = new RockContext() )
            {
                var surveyTypes = new DefinedValueService( rockContext )
                    .GetByDefinedTypeGuid( MinistrySafeSystemGuid.MINISTRYSAFE_SURVEY_TYPES.AsGuid() )
                    .Where( v => v.IsActive )
                    .Select( v => v.Value )
                    .ToList();

                lSurveyTypes.Text = surveyTypes.AsDelimited( "<br/>" );
            }
        }

        /// <summary>
        /// Determines whether Checkr is the default provider.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if Checkr is the default provider; otherwise, <c>false</c>.
        /// </returns>
        private bool IsDefaultProvider()
        {
            string providerTypeName = ( typeof( com.bemaservices.MinistrySafe.MinistrySafe ) ).FullName;
            string defaultProvider = Rock.Web.SystemSettings.GetValue( SystemSetting.DEFAULT_BACKGROUND_CHECK_PROVIDER ) ?? string.Empty;
            return providerTypeName == defaultProvider;
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        private void ShowDetail()
        {
            string accessToken = null;
            string serverUrl = null;
            bool enableDebugging = false;
            using ( RockContext rockContext = new RockContext() )
            {
                var settings = com.bemaservices.MinistrySafe.MinistrySafe.GetSettings( rockContext );
                if ( settings != null )
                {
                    accessToken = com.bemaservices.MinistrySafe.MinistrySafe.GetSettingValue( settings, MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_ACCESS_TOKEN, true );
                    serverUrl = com.bemaservices.MinistrySafe.MinistrySafe.GetSettingValue( settings, MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_SERVER_URL, false );
                    enableDebugging = com.bemaservices.MinistrySafe.MinistrySafe.GetSettingValue( settings, MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_ENABLE_DEBUGGING, false ).AsBoolean();

                    if ( accessToken.IsNullOrWhiteSpace() )
                    {
                        string token = GlobalAttributesCache.Value( "MinistrySafeAPIToken" );
                        if ( token.IsNotNullOrWhiteSpace() )
                        {
                            com.bemaservices.MinistrySafe.MinistrySafe.SetSettingValue( rockContext, settings, MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_ACCESS_TOKEN, token, true );
                            rockContext.SaveChanges();
                            BackgroundCheckContainer.Instance.Refresh();
                            accessToken = com.bemaservices.MinistrySafe.MinistrySafe.GetSettingValue( settings, MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_ACCESS_TOKEN, true );
                        }
                    }

                    if ( serverUrl.IsNullOrWhiteSpace() )
                    {
                        serverUrl = MinistrySafeConstants.MINISTRYSAFE_APISERVER;
                    }
                }
            }

            tbAccessToken.Text = accessToken;
            tbServerUrl.Text = serverUrl;
            cbEnableDebugging.Checked = enableDebugging;

            if ( accessToken.IsNullOrWhiteSpace() || serverUrl.IsNullOrWhiteSpace() )
            {
                btnDefault.Visible = false;
                pnlToken.Visible = true;
                pnlPackages.Visible = false;
                HideSecondaryBlocks( true );
            }
            else
            {
                if ( IsDefaultProvider() )
                {
                    btnDefault.Visible = false;
                }
                else
                {
                    btnDefault.Visible = true;
                }

                pnlPackages.Enabled = true;
                lViewColumnLeft.Text = new DescriptionList()
                    .Add( "Access Token", accessToken )
                    .Add( "Server Url", serverUrl )
                    .Add( "Enable Debugging?", enableDebugging )
                    .Html;
                DisplayPackages();
                DisplayTags();
                DisplaySurveyTypes();
            }
        }

        #endregion
    }
}
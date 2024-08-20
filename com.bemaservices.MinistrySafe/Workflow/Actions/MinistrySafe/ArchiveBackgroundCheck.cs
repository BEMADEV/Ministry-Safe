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
using System.ComponentModel;
using System.ComponentModel.Composition;

using Rock;
using Rock.Workflow;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Security;
using com.bemaservices.MinistrySafe;
namespace com.bemaservices.MinistrySafe.Workflow.Action
{
    /// <summary>
    /// Sends a MinistrySafe Awareness Training.
    /// </summary>
    [ActionCategory( "BEMA Services > MinistrySafe" )]
    [Description( "Archives Background Checks linked to the current Workflow." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "MinistrySafe Archive Background Check" )]

    public class ArchiveBackgroundCheck : ActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            var provider = new MinistrySafe();

            var ministrySafe = new MinistrySafe();
            return ministrySafe.ArchiveLinkedBackgroundChecks( rockContext, action.Activity.Workflow, out errorMessages );
        }
    }
}
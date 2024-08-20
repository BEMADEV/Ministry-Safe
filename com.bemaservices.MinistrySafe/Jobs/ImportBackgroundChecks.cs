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
using System.Data.Entity;
using System.Linq;

using Quartz;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace com.bemaservices.MinistrySafe.Jobs
{

    /// <summary>
    /// Class ImportBackgroundChecks.
    /// Implements the <see cref="IJob" />
    /// </summary>
    /// <seealso cref="IJob" />
    [SlidingDateRangeField( "Date Range", "The date range of trainings to import.", required: true )]
    [WorkflowTypeField( "Workflow Type", "An optional workflow type to fire for background checks without an existing workflow.", required: false )]
    [DisallowConcurrentExecution]
    public class ImportBackgroundChecks : IJob
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImportTrainings" /> class.
        /// </summary>
        public ImportBackgroundChecks()
        {
        }

        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public virtual void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var dateRange = SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( dataMap.Get( "DateRange" ) != null ? dataMap.Get( "DateRange" ).ToString() : "-1||" );

            int backgroundChecksProcessed = 0;
            List<string> errorMessages = new List<string>();

            var rockContext = new RockContext();
            WorkflowTypeCache workflowType = null;
            Guid? workflowTypeGuid = dataMap.Get( "WorkflowType" ).ToStringSafe().AsGuidOrNull();
            if ( workflowTypeGuid.HasValue )
            {
                var workflowTypeService = new WorkflowTypeService( rockContext );
                workflowType = WorkflowTypeCache.Get( workflowTypeGuid.Value );
            }

            var ministrySafe = new MinistrySafe();
            ministrySafe.ImportBackgroundChecks( dateRange, workflowType, out backgroundChecksProcessed, out errorMessages );

            context.Result += string.Format( "{0} background checks processed{1}", backgroundChecksProcessed, ( errorMessages.Count > 0 ) ? ", but " + errorMessages.Count + " errors were reported" : string.Empty );
        }
    }
}
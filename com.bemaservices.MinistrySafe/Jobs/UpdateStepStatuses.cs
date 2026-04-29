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
using Rock.Jobs;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace com.bemaservices.MinistrySafe.Jobs
{

    /// <summary>
    /// Job to update MinistrySafe step statuses based on completion dates and expiration thresholds.
    /// </summary>
    /// <seealso cref="IJob" />
    [IntegerField( "Days Until Expiring",
        Description = "The number of days after completion when a step should be marked as Expiring.",
        IsRequired = true,
        DefaultIntegerValue = 700,
        Key = "DaysUntilExpiring",
        Order = 0 )]
    [IntegerField( "Days Until Expired",
        Description = "The number of days after completion when a step should be marked as Expired.",
        IsRequired = true,
        DefaultIntegerValue = 730,
        Key = "DaysUntilExpired",
        Order = 1 )]
    [DisallowConcurrentExecution]
    public class UpdateStepStatuses : RockJob
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateStepStatuses" /> class.
        /// </summary>
        public UpdateStepStatuses()
        {
        }

        /// <summary>
        /// Executes the specified context.
        /// </summary>
        public override void Execute()
        {
            var daysUntilExpiring = GetAttributeValue( "DaysUntilExpiring" ).AsIntegerOrNull() ?? 700;
            var daysUntilExpired = GetAttributeValue( "DaysUntilExpired" ).AsIntegerOrNull() ?? 730;

            int stepsMarkedExpiring = 0;
            int stepsMarkedExpired = 0;

            var rockContext = new RockContext();
            var stepService = new StepService( rockContext );
            var stepProgramService = new StepProgramService( rockContext );
            var stepStatusService = new StepStatusService( rockContext );
            var stepProgramGuid = Constants.MinistrySafeSystemGuid.MINISTRYSAFE_TRAINING_PROGRAM.AsGuid();

            // Get MinistrySafe Step Program
            var ministrySafeProgram = stepProgramService
                .Queryable()
                .AsNoTracking()
                .FirstOrDefault( sp => sp.Guid == stepProgramGuid );

            if ( ministrySafeProgram == null )
            {
                this.Result = "MinistrySafe Step Program not found.";
                return;
            }

            // Get Step Statuses
            var passedStatus = stepStatusService
                .Queryable()
                .AsNoTracking()
                .FirstOrDefault( ss => ss.Name == "Passed" );

            var expiringStatus = stepStatusService
                .Queryable()
                .AsNoTracking()
                .FirstOrDefault( ss => ss.Name == "Expiring" );

            var expiredStatus = stepStatusService
                .Queryable()
                .AsNoTracking()
                .FirstOrDefault( ss => ss.Name == "Expired" );

            if ( passedStatus == null || expiringStatus == null || expiredStatus == null )
            {
                this.Result = "Required step statuses (Passed, Expiring, Expired) not found.";
                return;
            }

            var today = RockDateTime.Today;
            var expiringThresholdDate = today.AddDays( -daysUntilExpiring );
            var expiredThresholdDate = today.AddDays( -daysUntilExpired );

            // Update Passed steps to Expiring
            var passedSteps = stepService
                .Queryable()
                .Where( s => s.StepType.StepProgramId == ministrySafeProgram.Id )
                .Where( s => s.StepStatusId == passedStatus.Id )
                .Where( s => s.CompletedDateTime.HasValue )
                .Where( s => s.CompletedDateTime.Value < expiringThresholdDate )
                .ToList();

            foreach ( var step in passedSteps )
            {
                step.StepStatusId = expiringStatus.Id;
                stepsMarkedExpiring++;
            }

            rockContext.SaveChanges();

            // Update Expiring steps to Expired
            var expiringSteps = stepService
                .Queryable()
                .Where( s => s.StepType.StepProgramId == ministrySafeProgram.Id )
                .Where( s => s.StepStatusId == expiringStatus.Id )
                .Where( s => s.CompletedDateTime.HasValue )
                .Where( s => s.CompletedDateTime.Value < expiredThresholdDate )
                .ToList();

            foreach ( var step in expiringSteps )
            {
                step.StepStatusId = expiredStatus.Id;
                stepsMarkedExpired++;
            }

            rockContext.SaveChanges();

            this.Result = string.Format( "{0} step{1} marked as Expiring, {2} step{3} marked as Expired.",
                stepsMarkedExpiring,
                stepsMarkedExpiring == 1 ? "" : "s",
                stepsMarkedExpired,
                stepsMarkedExpired == 1 ? "" : "s" );
        }
    }
}

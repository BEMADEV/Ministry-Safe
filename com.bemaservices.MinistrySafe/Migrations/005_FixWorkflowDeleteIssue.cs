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
using System.Text;
using System.Threading.Tasks;
using Rock.Plugin;

namespace com.bemaservices.MinistrySafe.Migrations
{
    /// <summary>
    /// Class FixWorkflowDeleteIssue.
    /// Implements the <see cref="Migration" />
    /// </summary>
    /// <seealso cref="Migration" />
    [MigrationNumber( 5, "1.8.5" )]
    public class FixWorkflowDeleteIssue : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            Sql( @"
                ALTER TABLE [dbo].[_com_bemaservices_MinistrySafe_MinistrySafeUser] DROP CONSTRAINT [FK__com_bemaservices_MinistrySafe_MinistrySafeUser_WorkflowId]

                ALTER TABLE [dbo].[_com_bemaservices_MinistrySafe_MinistrySafeUser]  WITH CHECK ADD  CONSTRAINT [FK__com_bemaservices_MinistrySafe_MinistrySafeUser_WorkflowId] FOREIGN KEY([WorkflowId])
                REFERENCES [dbo].[Workflow] ([Id])
                ON DELETE CASCADE
                

                ALTER TABLE [dbo].[_com_bemaservices_MinistrySafe_MinistrySafeUser] CHECK CONSTRAINT [FK__com_bemaservices_MinistrySafe_MinistrySafeUser_WorkflowId]
" );          
            
        }
        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
        }
    }
}

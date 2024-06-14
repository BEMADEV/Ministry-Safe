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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
namespace com.bemaservices.MinistrySafe.Model
{
    /// <summary>
    /// Class MinistrySafeUser.
    /// Implements the <see cref="Rock.Data.Model{com.bemaservices.MinistrySafe.Model.MinistrySafeUser}" />
    /// Implements the <see cref="IRockEntity" />
    /// </summary>
    /// <seealso cref="Rock.Data.Model{com.bemaservices.MinistrySafe.Model.MinistrySafeUser}" />
    /// <seealso cref="IRockEntity" />
    [Table( "_com_bemaservices_MinistrySafe_MinistrySafeUser" )]
    [DataContract]
    public class MinistrySafeUser : Rock.Data.Model<MinistrySafeUser>, Rock.Data.IRockEntity
    {

        #region Entity Properties

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        [DataMember]
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        /// <value>The first name.</value>
        [DataMember]
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        /// <value>The last name.</value>
        [DataMember]
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the person alias identifier.
        /// </summary>
        /// <value>The person alias identifier.</value>
        [DataMember]
        public int PersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets the score.
        /// </summary>
        /// <value>The score.</value>
        [DataMember]
        public int? Score { get; set; }

        /// <summary>
        /// Gets or sets the type of the user.
        /// </summary>
        /// <value>The type of the user.</value>
        [DataMember]
        public string UserType { get; set; }

        /// <summary>
        /// Gets or sets the survey code.
        /// </summary>
        /// <value>The survey code.</value>
        [DataMember]
        public string SurveyCode { get; set; }

        /// <summary>
        /// Gets or sets the direct login URL.
        /// </summary>
        /// <value>The direct login URL.</value>
        [DataMember]
        public string DirectLoginUrl { get; set; }

        /// <summary>
        /// Gets or sets the completed date time.
        /// </summary>
        /// <value>The completed date time.</value>
        [DataMember]
        public DateTime? CompletedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the workflow identifier.
        /// </summary>
        /// <value>The workflow identifier.</value>
        [DataMember]
        public int? WorkflowId { get; set; }

        /// <summary>
        /// Gets or sets the request date.
        /// </summary>
        /// <value>The request date.</value>
        [DataMember]
        public DateTime RequestDate { get; set; }

        /// <summary>
        /// Gets or sets the response date.
        /// </summary>
        /// <value>The response date.</value>
        [DataMember]
        public DateTime? ResponseDate { get; set; }

        #endregion

        #region Virtual Properties
        /// <summary>
        /// Gets or sets the person alias.
        /// </summary>
        /// <value>The person alias.</value>
        [LavaVisibleAttribute]
        public virtual Rock.Model.PersonAlias PersonAlias { get; set; }

        /// <summary>
        /// Gets or sets the workflow.
        /// </summary>
        /// <value>The workflow.</value>
        [LavaVisibleAttribute]
        public virtual Rock.Model.Workflow Workflow { get; set; }

        #endregion

    }

    #region Entity Configuration


    /// <summary>
    /// Class MinistrySafeUserConfiguration.
    /// Implements the <see cref="System.Data.Entity.ModelConfiguration.EntityTypeConfiguration{com.bemaservices.MinistrySafe.Model.MinistrySafeUser}" />
    /// </summary>
    /// <seealso cref="System.Data.Entity.ModelConfiguration.EntityTypeConfiguration{com.bemaservices.MinistrySafe.Model.MinistrySafeUser}" />
    public partial class MinistrySafeUserConfiguration : EntityTypeConfiguration<MinistrySafeUser>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MinistrySafeUserConfiguration" /> class.
        /// </summary>
        public MinistrySafeUserConfiguration()
        {
            this.HasRequired( p => p.PersonAlias ).WithMany().HasForeignKey( p => p.PersonAliasId ).WillCascadeOnDelete( true );
            this.HasOptional( p => p.Workflow ).WithMany().HasForeignKey( p => p.WorkflowId ).WillCascadeOnDelete( true );

            // IMPORTANT!!
            this.HasEntitySetName( "MinistrySafeUser" );
        }
    }

    #endregion

}
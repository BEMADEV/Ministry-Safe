using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Plugin;

namespace com.bemaservices.MinistrySafe.Migrations
{
    [MigrationNumber( 10, "1.12.7" )]
    public partial class AdditionalTrainingTypes : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.UpdateDefinedValue( "95EF81D2-C192-4B9E-A7A3-5E1E90BDA3CE", "california", "California-Specific Awareness Training", "BD3D604D-13D7-400C-AB21-92313A57E41C", false );
            RockMigrationHelper.UpdateDefinedValue( "95EF81D2-C192-4B9E-A7A3-5E1E90BDA3CE", "daycare", "Daycare-Focused Sexual Abuse Awareness Training", "E23A6BEE-EC40-4081-907C-F33A5CB482B1", false );
            RockMigrationHelper.UpdateDefinedValue( "95EF81D2-C192-4B9E-A7A3-5E1E90BDA3CE", "education", "Education-Focused Sexual Abuse Awareness Training", "68806ED1-9DC4-4BB7-9995-8C1E7D4A2D1E", false );
            RockMigrationHelper.UpdateDefinedValue( "95EF81D2-C192-4B9E-A7A3-5E1E90BDA3CE", "parent_training", "Parent Training", "C0EFA286-26AD-4F67-95A4-990DFDB5F4B5", false );
            RockMigrationHelper.UpdateDefinedValue( "95EF81D2-C192-4B9E-A7A3-5E1E90BDA3CE", "peer_to_peer_training", "Peer-to-Peer Sexual Abuse Training", "B3BCC294-3C66-4EC5-9EF1-61F14B74A4A2", false );
            RockMigrationHelper.UpdateDefinedValue( "95EF81D2-C192-4B9E-A7A3-5E1E90BDA3CE", "skillful_screening", "Skillful Screening Training", "5FC2AB06-080B-4435-9893-24B1436C3643", false );
            RockMigrationHelper.UpdateDefinedValue( "95EF81D2-C192-4B9E-A7A3-5E1E90BDA3CE", "youth_ministry", "Youth Ministry Sexual Abuse Awareness Training", "51484E20-744F-4FC8-97D4-8763BFEE1BA8", false );
        }
        public override void Down()
        {
        }
    }
}

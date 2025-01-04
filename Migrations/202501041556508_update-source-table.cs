namespace LocalFileWebService.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class updatesourcetable : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Sources", "SourceLength", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Sources", "SourceLength");
        }
    }
}

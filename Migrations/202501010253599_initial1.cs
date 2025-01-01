namespace LocalFileWebService.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class initial1 : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.SourceInfoes", newName: "Sources");
            AddColumn("dbo.Sources", "SourceType", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Sources", "SourceType");
            RenameTable(name: "dbo.Sources", newName: "SourceInfoes");
        }
    }
}

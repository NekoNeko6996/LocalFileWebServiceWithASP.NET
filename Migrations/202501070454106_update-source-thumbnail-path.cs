namespace LocalFileWebService.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class updatesourcethumbnailpath : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Sources", "SourceThumbnailPath", c => c.String());
            DropColumn("dbo.Sources", "SourceThumbnailName");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Sources", "SourceThumbnailName", c => c.String());
            DropColumn("dbo.Sources", "SourceThumbnailPath");
        }
    }
}

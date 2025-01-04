namespace LocalFileWebService.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class update_source_thumbnail : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Sources", "SourceThumbnailName", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Sources", "SourceThumbnailName");
        }
    }
}

namespace LocalFileWebService.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SourceInfoes",
                c => new
                    {
                        SourceId = c.Int(nullable: false, identity: true),
                        SourceName = c.String(nullable: false, maxLength: 255),
                        SourceDescription = c.String(maxLength: 1000),
                        UploadTime = c.DateTime(nullable: false),
                        SourceUrl = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.SourceId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.SourceInfoes");
        }
    }
}

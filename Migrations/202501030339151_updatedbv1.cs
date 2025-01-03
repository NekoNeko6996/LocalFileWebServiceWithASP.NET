namespace LocalFileWebService.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class updatedbv1 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Artists",
                c => new
                    {
                        ArtistId = c.Int(nullable: false, identity: true),
                        ArtistName = c.String(nullable: false),
                        ArtisPageLink = c.String(),
                        ArtisAvatarUrl = c.String(),
                        ArtistCreateAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ArtistId);
            
            CreateTable(
                "dbo.FolderLinks",
                c => new
                    {
                        SourceId = c.Int(nullable: false),
                        FolderId = c.Int(nullable: false),
                        FolderLinkCreateAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => new { t.SourceId, t.FolderId })
                .ForeignKey("dbo.Folders", t => t.FolderId, cascadeDelete: true)
                .ForeignKey("dbo.Sources", t => t.SourceId, cascadeDelete: true)
                .Index(t => t.SourceId)
                .Index(t => t.FolderId);
            
            CreateTable(
                "dbo.Folders",
                c => new
                    {
                        FolderId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        FolderName = c.String(nullable: false),
                        FolderIconUrl = c.String(),
                        FolderParentId = c.Int(nullable: false),
                        FolderCreateAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.FolderId)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        UserId = c.Int(nullable: false, identity: true),
                        UserEmail = c.String(nullable: false, maxLength: 255),
                        UserHash = c.String(nullable: false, maxLength: 64),
                        UserCreateAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.UserId)
                .Index(t => t.UserEmail, unique: true);
            
            CreateTable(
                "dbo.Sources",
                c => new
                    {
                        SourceId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        SourceName = c.String(nullable: false, maxLength: 255),
                        SourceDescription = c.String(maxLength: 1000),
                        SourceType = c.String(nullable: false),
                        SourceTag = c.String(),
                        ArtistId = c.Int(nullable: false),
                        UploadTime = c.DateTime(nullable: false),
                        SourceUrl = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.SourceId)
                .ForeignKey("dbo.Artists", t => t.ArtistId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId)
                .Index(t => t.ArtistId);
            
            CreateTable(
                "dbo.Tags",
                c => new
                    {
                        TagId = c.Int(nullable: false, identity: true),
                        TagName = c.String(nullable: false),
                        TagCreateAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.TagId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.FolderLinks", "SourceId", "dbo.Sources");
            DropForeignKey("dbo.FolderLinks", "FolderId", "dbo.Folders");
            DropForeignKey("dbo.Folders", "UserId", "dbo.Users");
            DropForeignKey("dbo.Sources", "UserId", "dbo.Users");
            DropForeignKey("dbo.Sources", "ArtistId", "dbo.Artists");
            DropIndex("dbo.Sources", new[] { "ArtistId" });
            DropIndex("dbo.Sources", new[] { "UserId" });
            DropIndex("dbo.Users", new[] { "UserEmail" });
            DropIndex("dbo.Folders", new[] { "UserId" });
            DropIndex("dbo.FolderLinks", new[] { "FolderId" });
            DropIndex("dbo.FolderLinks", new[] { "SourceId" });
            DropTable("dbo.Tags");
            DropTable("dbo.Sources");
            DropTable("dbo.Users");
            DropTable("dbo.Folders");
            DropTable("dbo.FolderLinks");
            DropTable("dbo.Artists");
        }
    }
}

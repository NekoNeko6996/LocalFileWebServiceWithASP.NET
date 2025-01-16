using System.Data.Entity;

namespace LocalFileWebService.Models
{
    public class MVCDBContext : DbContext
    {
        public MVCDBContext() : base("MyConnect") { }

        public DbSet<User> Users { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Source> Sources { get; set; }
        public DbSet<Folder> Folders { get; set; }
        public DbSet<FolderLink> FolderLinks { get; set; }
        

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Thêm cấu hình khóa chính composite
            modelBuilder.Entity<FolderLink>()
                .HasKey(fl => new { fl.SourceId, fl.FolderId }); // 2 PK columns

            modelBuilder.Entity<User>()
                .HasIndex(u => u.UserEmail) // Tạo chỉ mục trên cột UserEmail
                .IsUnique();                // Đảm bảo duy nhất

            modelBuilder.Entity<Source>()
                .HasRequired(s => s.User)      // Một Source phải liên kết với một User
                .WithMany(u => u.Sources)     // Một User có thể có nhiều Source
                .HasForeignKey(s => s.UserId) // Khóa ngoại UserId
                .WillCascadeOnDelete(false);  // Không cho phép Cascade Delete


            base.OnModelCreating(modelBuilder);
        }
    }
}   
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace LocalFileWebService.Models
{
    public class MVCDBContext : DbContext
    {
        public MVCDBContext() : base("MyConnect") { }

        public DbSet<Sources> Sources { get; set; }
    }
}   
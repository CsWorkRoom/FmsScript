
namespace Easyman.Service
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Configuration;
    using System.Data.Common;

    public partial class DBEntities : DbContext
    {
        public DBEntities()
            :  base("name=DBEntities")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //var schema = ConfigurationManager.AppSettings["Database.Schema"];
            var schema = ConfigurationSettings.AppSettings["Database.Schema"];
            modelBuilder.HasDefaultSchema(schema);
            base.OnModelCreating(modelBuilder);
        }


        public DbSet<EM_ANALYSIS> EM_ANALYSIS { get; set; }
        public DbSet<EM_CONNECT_LINE> EM_CONNECT_LINE { get; set; }
        public DbSet<EM_DB_SERVER> EM_DB_SERVER { get; set; }
        public DbSet<EM_DB_TAG> EM_DB_TAG { get; set; }
        public DbSet<EM_HAND_RECORD> EM_HAND_RECORD { get; set; }
        public DbSet<EM_ICON> EM_ICON { get; set; }
        public DbSet<EM_MODULE> EM_MODULE { get; set; }
        public DbSet<EM_MODULE_EVENT> EM_MODULE_EVENT { get; set; }
        public DbSet<EM_NODE_POSITION> EM_NODE_POSITION { get; set; }
        public DbSet<EM_NODE_POSITION_FORCASE> EM_NODE_POSITION_FORCASE { get; set; }
        public DbSet<EM_ROLE_MODULE> EM_ROLE_MODULE { get; set; }
        public DbSet<EM_ROLE_MODULE_EVENT> EM_ROLE_MODULE_EVENT { get; set; }
        public DbSet<EM_SCRIPT> EM_SCRIPT { get; set; }
        public DbSet<EM_SCRIPT_CASE> EM_SCRIPT_CASE { get; set; }
        public DbSet<EM_SCRIPT_CASE_LOG> EM_SCRIPT_CASE_LOG { get; set; }
        public DbSet<EM_SCRIPT_NODE> EM_SCRIPT_NODE { get; set; }
        public DbSet<EM_SCRIPT_NODE_CASE> EM_SCRIPT_NODE_CASE { get; set; }
        public DbSet<EM_SCRIPT_NODE_CASE_LOG> EM_SCRIPT_NODE_CASE_LOG { get; set; }
        public DbSet<EM_SCRIPT_NODE_FORCASE> EM_SCRIPT_NODE_FORCASE { get; set; }
        public DbSet<EM_SCRIPT_NODE_LOG> EM_SCRIPT_NODE_LOG { get; set; }
        public DbSet<EM_SCRIPT_NODE_TYPE> EM_SCRIPT_NODE_TYPE { get; set; }
        public DbSet<EM_SCRIPT_REF_NODE> EM_SCRIPT_REF_NODE { get; set; }
        public DbSet<EM_SCRIPT_REF_NODE_FORCASE> EM_SCRIPT_REF_NODE_FORCASE { get; set; }
        public DbSet<EM_SCRIPT_TYPE> EM_SCRIPT_TYPE { get; set; }
        public DbSet<EM_SCRIPT_FUNCTION> EM_SCRIPT_FUNCTION { get; set; }
        public DbSet<EM_CONNECT_LINE_FORCASE> EM_CONNECT_LINE_FORCASE { get; set; }
    }
}

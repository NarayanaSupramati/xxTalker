using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using xxTalker.Shared.Models;

namespace xxTalker.Shared.DataAccess
{
    public class DomainModelPostgreSqlContext : DbContext
    {
        public const string DatabaseSchema = "public";

        public DomainModelPostgreSqlContext(DbContextOptions<DomainModelPostgreSqlContext> options) : base(options)
        {
        }

        public DbSet<Talker> Talkers { get; set; }
        public DbSet<TalkerMessage> Messages { get; set; }

        private static string TranslateTypeName<T>() => NameTranslator.TranslateTypeName(typeof(T).Name);
        private static readonly INpgsqlNameTranslator NameTranslator = NpgsqlConnection.GlobalTypeMapper.DefaultNameTranslator;

        static DomainModelPostgreSqlContext()
        {
            NpgsqlConnection.GlobalTypeMapper.MapEnum<MessageType>($"{DatabaseSchema}.{TranslateTypeName<MessageType>()}", NameTranslator);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.HasDefaultSchema(DatabaseSchema);

            builder.HasPostgresEnum<MessageType>(DatabaseSchema, TranslateTypeName<MessageType>(), NameTranslator);

            builder.Entity<TalkerMessage>().HasIndex(tm => tm.ReceiverAccount);

            builder.Entity<TalkerMessage>()
                .Property(tm => tm.MessageId)
                .ValueGeneratedOnAdd();

            builder.Entity<Talker>()
                .ToView("talker")
                .HasMany(t => t.Messages)
                .WithOne()
                .HasForeignKey(tm => tm.ReceiverAccount)
                .HasPrincipalKey(t => t.AccountId);

        }

        public override int SaveChanges()
        {
            ChangeTracker.DetectChanges();
            return base.SaveChanges();
        }

    }
}

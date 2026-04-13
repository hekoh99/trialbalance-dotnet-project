using Microsoft.EntityFrameworkCore;
using TriBalance.Domain.Engagement;
using TriBalance.Domain.Validation;

namespace TriBalance.Infrastructure.Persistence.PostgreSQL;

public class TriBalanceDbContext : DbContext
{
    public DbSet<Engagement> Engagements => Set<Engagement>();
    public DbSet<TrialBalance> TrialBalances => Set<TrialBalance>();
    public DbSet<GLEntry> GlEntries => Set<GLEntry>();
    public DbSet<ValidationJob> ValidationJobs => Set<ValidationJob>();

    public TriBalanceDbContext(DbContextOptions<TriBalanceDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Engagement>(entity =>
        {
            entity.ToTable("engagements");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ClientName).HasColumnName("client_name").IsRequired();
            entity.Property(e => e.FiscalYearEnd).HasColumnName("fiscal_year_end").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

            entity.HasMany(e => e.TrialBalances)
                .WithOne()
                .HasForeignKey(tb => tb.EngagementId);
        });

        modelBuilder.Entity<TrialBalance>(entity =>
        {
            entity.ToTable("trial_balances");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.EngagementId).HasColumnName("engagement_id").IsRequired();
            entity.Property(e => e.FileName).HasColumnName("file_name").IsRequired();
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at").HasDefaultValueSql("now()");
            entity.Property(e => e.TotalDebits).HasColumnName("total_debits").HasPrecision(18, 2);
            entity.Property(e => e.TotalCredits).HasColumnName("total_credits").HasPrecision(18, 2);
            entity.Ignore(e => e.IsBalanced);

            entity.HasMany(e => e.GlEntries)
                .WithOne()
                .HasForeignKey(gl => gl.TrialBalanceId);
        });

        modelBuilder.Entity<GLEntry>(entity =>
        {
            entity.ToTable("gl_entries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.TrialBalanceId).HasColumnName("trial_balance_id").IsRequired();
            entity.Property(e => e.EngagementId).HasColumnName("engagement_id").IsRequired();
            entity.Property(e => e.AccountCode).HasColumnName("account_code").IsRequired();
            entity.Property(e => e.AccountName).HasColumnName("account_name").IsRequired();
            entity.Property(e => e.Debit).HasColumnName("debit").HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(e => e.Credit).HasColumnName("credit").HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(e => e.Balance).HasColumnName("balance").HasPrecision(18, 2);

            entity.HasIndex(e => e.TrialBalanceId).HasDatabaseName("idx_gl_entries_trial_balance");
            entity.HasIndex(e => e.EngagementId).HasDatabaseName("idx_gl_entries_engagement");
        });

        modelBuilder.Entity<ValidationJob>(entity =>
        {
            entity.ToTable("validation_jobs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.EngagementId).HasColumnName("engagement_id").IsRequired();
            entity.Property(e => e.TrialBalanceId).HasColumnName("trial_balance_id").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status")
                .HasConversion<string>()
                .HasDefaultValue(ValidationJobStatus.Queued);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");

            entity.HasIndex(e => e.EngagementId).HasDatabaseName("idx_validation_jobs_engagement");
            entity.HasIndex(e => e.Status).HasDatabaseName("idx_validation_jobs_status");
        });
    }
}

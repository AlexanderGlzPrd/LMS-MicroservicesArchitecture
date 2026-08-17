using Certification.Domain.Certificates;
using Certification.Infrastructure.Issuance;
using Certification.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
namespace Certification.Infrastructure.Persistence;

public sealed class CertificationDbContext(DbContextOptions<CertificationDbContext> options)
    : DbContext(options)
{
    public DbSet<Certificate> Certificates => Set<Certificate>();

    internal DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    internal DbSet<PendingCertificateIssuance> PendingCertificateIssuances =>
        Set<PendingCertificateIssuance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CertificationDbContext).Assembly);
    }
}
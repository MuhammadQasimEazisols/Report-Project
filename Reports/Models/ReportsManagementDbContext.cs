using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Reports.Models;

public partial class ReportsManagementDbContext : DbContext
{
    public ReportsManagementDbContext()
    {
    }

    public ReportsManagementDbContext(DbContextOptions<ReportsManagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TblApplicationUser> TblApplicationUsers { get; set; }

    public virtual DbSet<TblReport> TblReports { get; set; }

    public virtual DbSet<TblReportDetail> TblReportDetails { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblApplicationUser>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.ToTable("tblApplicationUser");

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.Email).HasMaxLength(1000);
            entity.Property(e => e.Password).HasMaxLength(1000);
            entity.Property(e => e.UserName).HasMaxLength(1000);
        });

        modelBuilder.Entity<TblReport>(entity =>
        {
            entity.HasKey(e => e.ReportId);

            entity.ToTable("tblReport");

            entity.Property(e => e.ReportId).ValueGeneratedNever();
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(1000);

            entity.HasOne(d => d.User).WithMany(p => p.TblReports)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Report_ApplicationUser");
        });

        modelBuilder.Entity<TblReportDetail>(entity =>
        {
            entity.HasKey(e => e.ReportDetailId).HasName("PK_ReportDetailId");

            entity.ToTable("tblReportDetail");

            entity.Property(e => e.ReportDetailId).ValueGeneratedNever();

            entity.HasOne(d => d.Report).WithMany(p => p.TblReportDetails)
                .HasForeignKey(d => d.ReportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReportDetail_Report");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

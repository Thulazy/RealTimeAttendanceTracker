using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Services.Extensions.Logger.Service;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;
using RealTimeAttendanceTracker.lib.Utility;

namespace RealTimeAttendanceTracker.lib.Entity;

public partial class AttendanceContext : DbContext
{
    public AttendanceContext()
    {
    }

    public AttendanceContext(DbContextOptions<AttendanceContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Staff> Staffs { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Timetable> Timetables { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            string conn = ConfigHelper.GetSetting(AppConstants.AppSettingKey.MySqlConn);
            var serverVersion = ServerVersion.AutoDetect(conn);
            optionsBuilder.UseMySql(conn, serverVersion);
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("staffs");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Department).HasMaxLength(200);
            entity.Property(e => e.HandlingSubjects).HasMaxLength(1000);
            entity.Property(e => e.IsDeleted).HasColumnType("bit(1)");
            entity.Property(e => e.ModifiedAt).HasColumnType("datetime");
            entity.Property(e => e.StaffName).HasMaxLength(200);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("students");

            entity.Property(e => e.Address).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.DateOfBirth).HasColumnType("datetime");
            entity.Property(e => e.Degree).HasMaxLength(200);
            entity.Property(e => e.IsDeleted).HasColumnType("bit(1)");
            entity.Property(e => e.Mobile).HasMaxLength(200);
            entity.Property(e => e.ModifiedAt).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(500);
            entity.Property(e => e.RegNo).HasMaxLength(500);
            entity.Property(e => e.Year).HasMaxLength(200);
        });

        modelBuilder.Entity<Timetable>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("timetable");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.IsDeleted).HasColumnType("bit(1)");
            entity.Property(e => e.ModifiedAt).HasColumnType("datetime");
            entity.Property(e => e.Subject).HasMaxLength(200);
            entity.Property(e => e.Time).HasMaxLength(200);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

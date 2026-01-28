using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace GersoPersonalizados.Api.Data.Models;

public partial class GersoDbContext : DbContext
{
    public GersoDbContext(DbContextOptions<GersoDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Customers> Customers { get; set; }

    public virtual DbSet<OrderItems> OrderItems { get; set; }

    public virtual DbSet<Orders> Orders { get; set; }

    public virtual DbSet<Payments> Payments { get; set; }

    public virtual DbSet<Products> Products { get; set; }

    public virtual DbSet<Users> Users { get; set; }

    public virtual DbSet<vw_OrderSummary> vw_OrderSummary { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customers>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64D8CF306BFA");

            entity.HasIndex(e => e.Phone, "IX_Customers_Phone");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.FullName).HasMaxLength(120);
            entity.Property(e => e.Notes).HasMaxLength(400);
            entity.Property(e => e.Phone).HasMaxLength(30);
        });

        modelBuilder.Entity<OrderItems>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__57ED06818362E20E");

            entity.HasIndex(e => e.OrderId, "IX_OrderItems_OrderId");

            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.LineTotal)
                .HasComputedColumnSql("(CONVERT([decimal](18,2),[Qty]*[UnitPrice]))", true)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_OrderItems_Products");
        });

        modelBuilder.Entity<Orders>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCF1EF80B24");

            entity.HasIndex(e => new { e.CustomerId, e.CreatedAt }, "IX_Orders_CustomerId_CreatedAt").IsDescending(false, true);

            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "IX_Orders_Status_CreatedAt").IsDescending(false, true);

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DeliveryType).HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_Orders_Users");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Customers");
        });

        modelBuilder.Entity<Payments>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A38DA6F8081");

            entity.HasIndex(e => new { e.OrderId, e.PaidAt }, "IX_Payments_OrderId_PaidAt").IsDescending(false, true);

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Method).HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.PaidAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Reference).HasMaxLength(80);

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Orders");
        });

        modelBuilder.Entity<Products>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6CD9A973B3C");

            entity.Property(e => e.BasePrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(120);
        });

        modelBuilder.Entity<Users>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C5A13EA91");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(160);
            entity.Property(e => e.FullName).HasMaxLength(120);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Role).HasMaxLength(30);
        });

        modelBuilder.Entity<vw_OrderSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_OrderSummary");

            entity.Property(e => e.Balance).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.CreatedAt).HasPrecision(0);
            entity.Property(e => e.DeliveryType).HasMaxLength(20);
            entity.Property(e => e.FullName).HasMaxLength(120);
            entity.Property(e => e.PaidAmount).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

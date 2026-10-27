using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DePaso.Entidades;

public partial class DePasoAppContext : DbContext
{
    public DePasoAppContext()
    {
    }

    public DePasoAppContext(DbContextOptions<DePasoAppContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Asignaciones> Asignaciones { get; set; }

    public virtual DbSet<EnvioEstadoHistoria> EnvioEstadoHistoria { get; set; }

    public virtual DbSet<Envios> Envios { get; set; }

    public virtual DbSet<Paradas> Paradas { get; set; }

    public virtual DbSet<Usuarios> Usuarios { get; set; }

    public virtual DbSet<ViajeDia> ViajeDia { get; set; }

    public virtual DbSet<Viajes> Viajes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:Conexion");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Asignaciones>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Asignaci__3214EC07C705ED66");

            entity.HasIndex(e => e.EnvioId, "UQ__Asignaci__D024E23E16902D18").IsUnique();

            entity.Property(e => e.AsignadoEn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DesvioKm).HasColumnType("decimal(4, 2)");
            entity.Property(e => e.Ganancia).HasColumnType("decimal(8, 2)");

            entity.HasOne(d => d.Envio).WithOne(p => p.Asignaciones)
                .HasForeignKey<Asignaciones>(d => d.EnvioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Asignacion_Envio");

            entity.HasOne(d => d.Viaje).WithMany(p => p.Asignaciones)
                .HasForeignKey(d => d.ViajeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Asignacion_Viaje");
        });

        modelBuilder.Entity<EnvioEstadoHistoria>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EnvioEst__3214EC07F52EABDD");

            entity.HasIndex(e => e.EnvioId, "UQ__EnvioEst__D024E23E3313BBA8");

            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Envio).WithMany(p => p.EnvioEstadoHistoria)
                .HasForeignKey<EnvioEstadoHistoria>(d => d.EnvioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EnvioEstadoHistoria_Envio");
        });

        modelBuilder.Entity<Envios>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Envios__3214EC07F23EEC5D");

            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DestinoDir)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DestinoLat).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.DestinoLng).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Monto).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.OrigenDir)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.OrigenLat).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.OrigenLng).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.PesoKg).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Envios)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Envio_Usuario");
        });

        modelBuilder.Entity<Paradas>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Paradas__3214EC077DADEF59");

            entity.Property(e => e.Direccion)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Lat).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Lng).HasColumnType("decimal(9, 6)");

            entity.HasOne(d => d.Viaje).WithMany(p => p.Paradas)
                .HasForeignKey(d => d.ViajeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Parada_Viaje");
        });

        modelBuilder.Entity<Usuarios>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuarios__3214EC07E7F20049");

            entity.HasIndex(e => e.Email, "UQ__Usuarios__A9D10534338279AE").IsUnique();

            entity.Property(e => e.Apellido).HasMaxLength(100);
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
        });

        modelBuilder.Entity<ViajeDia>(entity =>
        {
            entity.HasKey(e => new { e.ViajeId, e.DiaSemana });

            entity.HasOne(d => d.Viaje).WithMany(p => p.ViajeDia)
                .HasForeignKey(d => d.ViajeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ViajeDia_Viaje");
        });

        modelBuilder.Entity<Viajes>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Viajes__3214EC07838E3B0F");

            entity.Property(e => e.Activo).HasDefaultValue(false);
            entity.Property(e => e.DestinoDir)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DestinoLat).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.DestinoLng).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.DesvioMaxKm).HasColumnType("decimal(4, 2)");
            entity.Property(e => e.DistanciaKm).HasColumnType("decimal(4, 2)");
            entity.Property(e => e.OrigenDir)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.OrigenLat).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.OrigenLng).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.PesoMaxKg).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Viajes)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Viaje_Usuario");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

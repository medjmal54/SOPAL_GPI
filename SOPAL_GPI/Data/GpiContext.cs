using Microsoft.EntityFrameworkCore;
using SOPAL_GPI.Models;

namespace SOPAL_GPI.Data;

public partial class GpiContext : DbContext
{
    public GpiContext(DbContextOptions<GpiContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Employe> Employes { get; set; }

    public virtual DbSet<Etatmat> Etatmats { get; set; }

    public virtual DbSet<Intervention> Interventions { get; set; }

    public virtual DbSet<Localistation> Localistations { get; set; }

    public virtual DbSet<Material> Materials { get; set; }

    public virtual DbSet<Matype> Matypes { get; set; }

    public virtual DbSet<Mouvement> Mouvements { get; set; }

    public virtual DbSet<TypMov> TypMovs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employe>(entity =>
        {
            entity.HasKey(e => e.Matricule);

            entity.ToTable("Employe");

            entity.Property(e => e.Matricule).ValueGeneratedNever();
            entity.Property(e => e.Role).HasMaxLength(7);
        });

        modelBuilder.Entity<Etatmat>(entity =>
        {
            entity.ToTable("Etatmat");

            entity.Property(e => e.Libelle).HasMaxLength(10);
        });

        modelBuilder.Entity<Intervention>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Interven__3214EC270CF1B0C3");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Commentaires).HasMaxLength(200);

            entity.HasOne(d => d.Intervenant).WithMany(p => p.Interventions)
                .HasForeignKey(d => d.IntervenantId)
                .HasConstraintName("FK_Interventions_Employe");

            entity.HasOne(d => d.Maint).WithMany(p => p.Interventions)
                .HasForeignKey(d => d.MaintId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Interventions_Mouvement");

            entity.HasOne(d => d.Mat).WithMany(p => p.Interventions)
                .HasForeignKey(d => d.MatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Interventions_Material");
        });

        modelBuilder.Entity<Localistation>(entity =>
        {
            entity.ToTable("Localistation");
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.ToTable("Material");

            entity.HasIndex(e => e.NumSerie, "IX_Material_NumSerie").IsUnique();

            entity.Property(e => e.Commentaires).HasColumnName("commentaires");
            entity.Property(e => e.DateModif)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EtatId).HasDefaultValue(1);
            entity.Property(e => e.Libelle).HasMaxLength(100);
            entity.Property(e => e.LocalisationId).HasDefaultValue(6);
            entity.Property(e => e.Marque).HasMaxLength(50);
            entity.Property(e => e.Modele).HasMaxLength(50);
            entity.Property(e => e.NumSerie).HasMaxLength(100);

            entity.HasOne(d => d.EmployeMatNavigation).WithMany(p => p.Materials)
                .HasForeignKey(d => d.EmployeMat)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Etat).WithMany(p => p.Materials)
                .HasForeignKey(d => d.EtatId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Localisation).WithMany(p => p.Materials)
                .HasForeignKey(d => d.LocalisationId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TypeMateriel).WithMany(p => p.Materials)
                .HasForeignKey(d => d.TypeMaterielId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Matype>(entity =>
        {
            entity.ToTable("Matype");
        });

        modelBuilder.Entity<Mouvement>(entity =>
        {
            entity.ToTable("Mouvement");

            entity.Property(e => e.Commentaires).HasColumnName("commentaires");
            entity.Property(e => e.DateMouvement)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MaterielId).HasColumnName("materielId");
            entity.Property(e => e.Statut).HasMaxLength(30);
            entity.Property(e => e.Typmov).HasColumnName("typmov");

            entity.HasOne(d => d.EmployeMatNavigation).WithMany(p => p.Mouvements).HasForeignKey(d => d.EmployeMat);

            entity.HasOne(d => d.Localisation).WithMany(p => p.Mouvements)
                .HasForeignKey(d => d.LocalisationId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Materiel).WithMany(p => p.Mouvements).HasForeignKey(d => d.MaterielId);

            entity.HasOne(d => d.NouvEtatmat).WithMany(p => p.Mouvements)
                .HasForeignKey(d => d.NouvEtatmatId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TypmovNavigation).WithMany(p => p.Mouvements)
                .HasForeignKey(d => d.Typmov)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Mouvement_Typmov_TypmovId");
        });

        modelBuilder.Entity<TypMov>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TypMov__3214EC27C7552B1A");

            entity.ToTable("TypMov");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Nom).HasMaxLength(12);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

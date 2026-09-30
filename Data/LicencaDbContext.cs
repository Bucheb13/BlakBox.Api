using Microsoft.EntityFrameworkCore;
using BlakBox.Api.Models;

namespace BlakBox.Api.Data;

public class LicencaDbContext : DbContext, Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.IDataProtectionKeyContext
{
    public LicencaDbContext(
        DbContextOptions<LicencaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Sistema> Sistemas =>
        Set<Sistema>();

    public DbSet<Cliente> Clientes =>
        Set<Cliente>();

    public DbSet<Instalacao> Instalacoes =>
        Set<Instalacao>();

    public DbSet<Licenca> Licencas =>
        Set<Licenca>();

    public DbSet<LicencaHistorico> LicencaHistoricos =>
        Set<LicencaHistorico>();

        public DbSet<Plano> Planos =>
    Set<Plano>();

public DbSet<CredencialInstalacao> CredenciaisInstalacao =>
    Set<CredencialInstalacao>();
    public DbSet<ApiRequisicao> ApiRequisicoes =>
    Set<ApiRequisicao>();

    public DbSet<ConfiguracaoR2Instalacao> ConfiguracoesR2Instalacoes =>
        Set<ConfiguracaoR2Instalacao>();

    public DbSet<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey> DataProtectionKeys =>
        Set<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        /*
         * =====================================================
         * SISTEMA
         * =====================================================
         */

        modelBuilder.Entity<Sistema>(entity =>
        {
            entity.ToTable("Sistema");

            entity.HasKey(x =>
                x.SistemaId);

            entity.Property(x =>
                x.Codigo)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x =>
                x.Nome)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x =>
                x.Ativo)
                .IsRequired();

            entity.Property(x =>
                x.CriadoEm)
                .IsRequired();

            entity.HasIndex(x =>
                x.Codigo)
                .IsUnique();

                entity.Property(x =>
    x.DiasTolerancia)
    .IsRequired();

        });

        /*
         * =====================================================
         * CLIENTE
         * =====================================================
         */

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Cliente");

            entity.HasKey(x =>
                x.ClienteId);

            entity.Property(x =>
                x.Nome)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x =>
                x.Documento)
                .HasMaxLength(30);

            entity.Property(x =>
                x.Email)
                .HasMaxLength(150);

            entity.Property(x =>
                x.Telefone)
                .HasMaxLength(30);

            entity.Property(x =>
                x.Endereco)
                .HasMaxLength(300);

            entity.Property(x =>
                x.Numero)
                .HasMaxLength(50);

            entity.Property(x =>
                x.Ativo)
                .IsRequired();

            entity.Property(x =>
                x.CriadoEm)
                .IsRequired();

            entity.HasIndex(x =>
                x.Nome);

            entity.HasIndex(x =>
                x.Documento);
        });

        /*
         * =====================================================
         * INSTALAÇÃO
         * =====================================================
         */

        modelBuilder.Entity<Instalacao>(entity =>
        {
            entity.ToTable("Instalacao");

            entity.HasKey(x =>
                x.InstalacaoId);

            entity.Property(x =>
                x.InstalacaoKey)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x =>
                x.NomeInstalacao)
                .HasMaxLength(150);

            entity.Property(x =>
                x.Ativa)
                .IsRequired();

            entity.Property(x =>
                x.CriadoEm)
                .IsRequired();

            entity.HasIndex(x =>
                    new
                    {
                        x.SistemaId,
                        x.InstalacaoKey
                    })
                .IsUnique();

            entity.HasIndex(x =>
                x.ClienteId);

            entity.HasIndex(x =>
                x.SistemaId);

            entity.HasIndex(x =>
                x.UltimaComunicacao);

            entity.HasOne(x =>
                x.Cliente)
                .WithMany(x =>
                    x.Instalacoes)
                .HasForeignKey(x =>
                    x.ClienteId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            entity.HasOne(x =>
                x.Sistema)
                .WithMany(x =>
                    x.Instalacoes)
                .HasForeignKey(x =>
                    x.SistemaId)
                .OnDelete(
                    DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CredencialInstalacao>(entity =>
{
    entity.ToTable("CredencialInstalacao");

    entity.HasKey(x =>
        x.CredencialInstalacaoId);

    entity.Property(x =>
        x.ChaveHash)
        .HasMaxLength(128)
        .IsRequired();

    entity.Property(x =>
        x.UltimosCaracteres)
        .HasMaxLength(8);

    entity.Property(x =>
        x.Ativa)
        .IsRequired();

    entity.Property(x =>
        x.CriadoEm)
        .IsRequired();

    entity.Property(x => x.ChaveRecuperacaoProtegida);
    entity.Property(x => x.RecuperacaoExpiraEm);

    entity.HasIndex(x =>
        x.ChaveHash)
        .IsUnique();

    entity.HasIndex(x =>
        x.InstalacaoId);

    entity.HasIndex(x =>
        x.Ativa);

    entity.HasOne(x =>
        x.Instalacao)
        .WithMany(x =>
            x.Credenciais)
        .HasForeignKey(x =>
            x.InstalacaoId)
        .OnDelete(
            DeleteBehavior.Cascade);
});

        /*
         * =====================================================
         * LICENÇA
         * =====================================================
         */

modelBuilder.Entity<Plano>(entity =>
{
    entity.ToTable("Plano");

    entity.HasKey(x =>
        x.PlanoId);

    entity.Property(x =>
        x.Codigo)
        .HasMaxLength(50)
        .IsRequired();

    entity.Property(x =>
        x.Nome)
        .HasMaxLength(100)
        .IsRequired();

    entity.Property(x =>
        x.DuracaoDias);

    entity.Property(x =>
        x.DuracaoMeses);

    entity.Property(x =>
        x.Ativo)
        .IsRequired();

    entity.Property(x =>
        x.Valor)
        .HasColumnType("decimal(18,2)");

    entity.Property(x =>
        x.CriadoEm)
        .IsRequired();

    entity.HasIndex(x =>
        new
        {
            x.SistemaId,
            x.Codigo
        })
        .IsUnique();

    entity.HasIndex(x =>
        x.SistemaId);

    entity.HasIndex(x =>
        x.Ativo);

    entity.HasOne(x =>
        x.Sistema)
        .WithMany(x =>
            x.Planos)
        .HasForeignKey(x =>
            x.SistemaId)
        .OnDelete(
            DeleteBehavior.Restrict);
});

modelBuilder.Entity<Licenca>(entity =>
{
    entity.ToTable("Licenca");

    entity.HasKey(x =>
        x.LicencaId);

    entity.Property(x =>
        x.OficinaId)
        .HasMaxLength(50)
        .IsRequired();

    entity.Property(x =>
        x.NomeOficina)
        .HasMaxLength(150);

    entity.Property(x =>
        x.Plano)
        .HasMaxLength(50);

    entity.Property(x =>
        x.SituacaoComercial)
        .HasMaxLength(30)
        .IsRequired();

    entity.Property(x =>
        x.Mensagem)
        .HasMaxLength(500);

    entity.HasIndex(x =>
        x.OficinaId);

    entity.HasIndex(x =>
        x.InstalacaoId);

    entity.HasIndex(x =>
        x.PlanoId);

    entity.HasOne(x =>
        x.Instalacao)
        .WithMany(x =>
            x.Licencas)
        .HasForeignKey(x =>
            x.InstalacaoId)
        .OnDelete(
            DeleteBehavior.SetNull);

    entity.HasOne(x =>
        x.PlanoRelacionamento)
        .WithMany(x =>
            x.Licencas)
        .HasForeignKey(x =>
            x.PlanoId)
        .OnDelete(
            DeleteBehavior.SetNull);
});
        /*
         * =====================================================
         * HISTÓRICO DA LICENÇA
         * =====================================================
         */

        modelBuilder.Entity<LicencaHistorico>(entity =>
        {
            entity.ToTable("LicencaHistorico");

            entity.HasKey(x =>
                x.LicencaHistoricoId);

            entity.Property(x =>
                x.Acao)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x =>
                x.Plano)
                .HasMaxLength(50);

            entity.Property(x =>
                x.Origem)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x =>
                x.Observacao)
                .HasMaxLength(500);

            entity.Property(x =>
                x.CriadoEm)
                .IsRequired();

            entity.HasIndex(x =>
                x.LicencaId);

            entity.HasIndex(x =>
                x.CriadoEm);

            entity.HasOne(x =>
                x.Licenca)
                .WithMany(x =>
                    x.Historico)
                .HasForeignKey(x =>
                    x.LicencaId)
                .OnDelete(
                    DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApiRequisicao>(entity =>
        {
            entity.ToTable("ApiRequisicao");
            entity.HasKey(x => x.ApiRequisicaoId);
            // DataHora registra instantes UTC; os demais DateTime do sistema são horários locais de negócio.
            entity.Property(x => x.DataHora)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.Property(x => x.Metodo).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Rota).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Aplicacao).HasMaxLength(100);
            entity.Property(x => x.Operacao).HasMaxLength(100);
            entity.Property(x => x.QueryString).HasMaxLength(1000);
            entity.Property(x => x.StatusCode);
            entity.Property(x => x.DuracaoMs);
            entity.Property(x => x.OficinaId).HasMaxLength(50);
            entity.Property(x => x.Ip).HasMaxLength(100);
            entity.Property(x => x.UserAgent).HasMaxLength(1000);
            entity.Property(x => x.RequestContentType).HasMaxLength(200);
            entity.Property(x => x.ResponseContentType).HasMaxLength(200);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.Sucesso).IsRequired();
            entity.Property(x => x.Erro).HasMaxLength(2000);
            entity.HasIndex(x => x.DataHora);
            entity.HasIndex(x => x.StatusCode);
            entity.HasIndex(x => x.OficinaId);
            entity.HasIndex(x => x.InstalacaoId);
            entity.HasIndex(x => x.ClienteId);
            entity.HasIndex(x => x.SistemaId);
            entity.HasIndex(x => x.PlanoId);
            entity.HasIndex(x => x.Rota);
            entity.HasIndex(x => x.Operacao);
            entity.HasIndex(x => x.Aplicacao);
            entity.HasIndex(x => x.CorrelationId);
        });

        modelBuilder.Entity<ConfiguracaoR2Instalacao>(entity =>
        {
            entity.ToTable("ConfiguracaoR2Instalacao");
            entity.HasKey(x => x.InstalacaoId);
            entity.Property(x => x.Endpoint).HasMaxLength(500);
            entity.Property(x => x.Bucket).HasMaxLength(150);
            entity.Property(x => x.AccessKeyProtegida).HasMaxLength(2000);
            entity.Property(x => x.SecretKeyProtegida).HasMaxLength(4000);
            entity.Property(x => x.PublicBaseUrl).HasMaxLength(500);
            entity.Property(x => x.OficinaSlug).HasMaxLength(63);
            entity.Property(x => x.AtualizadoEm)
                .HasColumnType("timestamp without time zone")
                .IsRequired();
            entity.HasOne(x => x.Instalacao)
                .WithMany()
                .HasForeignKey(x => x.InstalacaoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey>(entity =>
        {
            entity.ToTable("DataProtectionKeys");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FriendlyName).HasMaxLength(450);
            entity.Property(x => x.Xml).HasColumnType("text");
        });


        // ============================================================
        // DATAS - PostgreSQL
        // ============================================================
        // Mantém o equivalente ao datetime2 do SQL Server:
        // timestamp without time zone.
        // ============================================================

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) ||
                    property.ClrType == typeof(DateTime?))
                {
                    if (entityType.ClrType == typeof(ApiRequisicao) &&
                        property.Name == nameof(ApiRequisicao.DataHora))
                    {
                        continue;
                    }

                    property.SetColumnType("timestamp without time zone");
                }
            }
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DataStorm.Web.Models;
using Npgsql;

namespace DataStorm.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<Utente>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            
            base.OnModelCreating(builder);
            builder.Entity<Segnalazione>().ToTable("Segnalazioni").HasKey(s => s.Id);
            builder.Entity<Utente>().ToTable("Utenti").HasKey(user => user.Id);
            builder.Entity<Utente>().HasMany(ue => ue.AppartamentiUtente);
            builder.Entity<Immobile>().ToTable("Immobili").HasKey(app => app.Id);
            builder.Entity<Immobile>().HasOne(app => app.PuntoMappa);
            builder.Entity<Immobile>().HasOne(app => app.UtenteAppartenenza);
            builder.Entity<LinkAvviso>().ToTable("LinkAvvisi").HasKey(lav => lav.Id);
            builder.Entity<ImmagineAvviso>().ToTable("ImmaginiAvvisi").HasKey(img => img.Id);
            builder.Entity<AvvisoTopic>().ToTable("AvvisiTopics").HasKey(av => av.Id);
            builder.Entity<Avviso>().ToTable("Avvisi").HasKey(av => av.Id);
            builder.Entity<Avviso>().HasMany(av => av.ImmaginiAvviso);
            builder.Entity<Avviso>().HasMany(av => av.Links);
            builder.Entity<Avviso>().HasMany(av => av.AvvisiTopics);
            builder.Entity<PuntoMappa>().ToTable("PuntiMappa").HasKey(pm => pm.Id);
            builder.Entity<AreaMappa>().ToTable("AreeMappa").HasKey(am=>am.Id);
            builder.Entity<AreaMappa>().HasMany(am => am.PuntiMappa);
            builder.Entity<Azienda>().ToTable("Aziende").HasKey(tl => tl.Id);
            builder.Entity<Azienda>().HasOne(az => az.Posizione);
            builder.Entity<TipologiaLavoro>().ToTable("TipologieLavoro").HasKey(tl=>tl.Id);
            builder.Entity<AziendeTipoLavoro>().ToTable("AziendeTipiLavoro").HasKey(tl => tl.Id);
            builder.Entity<AziendeTipoLavoro>().HasOne(az => az.AziendaLavoro);
            builder.Entity<AziendeTipoLavoro>().HasOne(tl => tl.TipoLavoro);
            builder.Entity<Catasto>().ToTable("Catasto").HasKey(ca => ca.IdCatasto);
            builder.Entity<Azienda>().HasMany(az => az.AziendaTipoLavoro);
            builder.Entity<Topic>().ToTable("Topics").HasKey(t=>t.Id);
            builder.Entity<Topic>().HasMany(t => t.AvvisiTopics);

            builder.Entity<Regione>().ToTable("Regioni").HasKey(r => r.ID);
            builder.Entity<Provincia>().ToTable("Province").HasKey(p => p.Sigla);
            builder.Entity<Provincia>().HasOne(p => p.Regione).WithMany().HasForeignKey(p => p.RegioneID);
            builder.Entity<Comune>().ToTable("Comuni").HasKey(c => c.ID);
            builder.Entity<Comune>().HasOne(c => c.Provincia).WithMany().HasForeignKey(c => c.SiglaProvincia);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }

        public DbSet<TipologiaLavoro> TipologieLavoro { get; set; }
        public DbSet<Azienda> Aziende { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Avviso> Avvisi { get; set; }
        public DbSet<Immobile> Immobili { get; set; }
        public DbSet<Utente> Utenti { get; set; }
        public DbSet<AreaMappa> AreeMappa { get; set; }
        public DbSet<Segnalazione> Segnalazioni { get; set; }

        public DbSet<Regione> Regioni { get; set; }
        public DbSet<Provincia> Province { get; set; }
        public DbSet<Comune> Comuni { get; set; }
    }
}

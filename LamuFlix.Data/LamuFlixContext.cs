using LamuFlix.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace LamuFlix.Data
{
    public class LamuFlixContext : DbContext
    {
        public DbSet<Movie> Movies { get; set; } = null!;
        public DbSet<Actor> Actors { get; set; } = null!;
        public DbSet<Director> Directors { get; set; } = null!;
        public DbSet<Genre> Genres { get; set; } = null!;
        public DbSet<Collection> Collections { get; set; } = null!;
        public DbSet<MovieActors> MovieActors { get; set; } = null!;
        public DbSet<MovieDirectors> MovieDirectors { get; set; } = null!;
        public DbSet<MovieGenre> MovieGenres { get; set; } = null!;

        public DbSet<Player> Players { get; set; } = null!;

        public LamuFlixContext()
        {

        }


        public LamuFlixContext(DbContextOptions<LamuFlixContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = Environment.GetEnvironmentVariable("LAMUFLIX_CONNECTION")
                    ?? "server=localhost;user id=test;password=test;port=3306;database=lamuflix;";
                optionsBuilder.UseMySql(connectionString,
                    new MySqlServerVersion(new Version(8, 0, 31)),
                    x => x.MigrationsHistoryTable("__efmigrationshistory"));
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Movie>(i =>
            {
                i.ToTable("movie");
                i.HasKey(x => x.Id);

                i.HasIndex(x => x.Title);
                i.HasIndex(x => x.Year);
                i.Property(x => x.Status).HasDefaultValue(MovieEnrichmentStatus.Pending);
            });

            builder.Entity<Actor>(i =>
            {
                i.ToTable("actor");
                i.HasKey(x => x.Id);

                i.HasIndex(x => x.Name);
            });

            builder.Entity<Director>(i =>
            {
                i.ToTable("director");
                i.HasKey(x => x.Id);

                i.HasIndex(x => x.Name);
            });

            builder.Entity<Genre>(i =>
            {
                i.ToTable("genre");
                i.HasKey(x => x.Id);

                i.HasIndex(x => x.Name);
            });

            builder.Entity<Collection>(i =>
            {
                i.ToTable("collection");
                i.HasKey(x => x.Id);

                i.HasIndex(x => x.Name);

                i.HasMany(x => x.Movies)
                .WithOne(x => x.Collection)
                .HasForeignKey(x => x.CollectionId);
            });

            builder.Entity<MovieActors>(i =>
            {
                i.ToTable("movieactors");
                i.HasKey(x => new { x.MovieId, x.ActorId });

                i.HasOne(x => x.Movie)
                .WithMany(x => x.Actors)
                .HasForeignKey(x => x.MovieId);

                i.HasOne(x => x.Actor)
                .WithMany(x => x.Movies)
                .HasForeignKey(x => x.ActorId);
            });

            builder.Entity<MovieDirectors>(i =>
            {
                i.ToTable("moviedirectors");
                i.HasKey(x => new { x.MovieId, x.DirectorId });

                i.HasOne(x => x.Movie)
                .WithMany(x => x.Directors)
                .HasForeignKey(x => x.MovieId);

                i.HasOne(x => x.Director)
                .WithMany(x => x.Movies)
                .HasForeignKey(x => x.DirectorId);
            });

            builder.Entity<MovieGenre>(i =>
            {
                i.ToTable("moviegenre");
                i.HasKey(x => new { x.MovieId, x.GenreId });

                i.HasOne(x => x.Movie)
                .WithMany(x => x.Genres)
                .HasForeignKey(x => x.MovieId);

                i.HasOne(x => x.Genre)
                .WithMany(x => x.Movies)
                .HasForeignKey(x => x.GenreId);
            });

            builder.Entity<Player>(i =>
            {
                i.ToTable("player");
                i.HasKey(x => x.Id);

                i.HasIndex(x => x.Name);

                i.HasData(
                    new Player { Id = 1, Name = "bsplayer", Path = "D:\\Program Files (x86)\\Webteh\\BSPlayer\\bsplayer.exe", Formats = ".webm, .xvid, .avi, .mpg, .mp4, .3ivx, .mov, .ogm, .mkv, .asf, .wmv, .dv, .mp4, .flv" },
                    new Player { Id = 2, Name = "RealPlayer", Path = "D:\\Program Files\\RealPlayer\\realplay.exe", Formats = ".rm, .rmvb" }
                        );
            });
        }
    }
}

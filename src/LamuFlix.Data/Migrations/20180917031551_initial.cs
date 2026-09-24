using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LamuFlix.Data.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "actor",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_actor", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "collection",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collection", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "director",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_director", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "genre",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_genre", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "player",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true),
                    Path = table.Column<string>(nullable: true),
                    Formats = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "movie",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(nullable: true),
                    Synopsis = table.Column<string>(nullable: true),
                    Year = table.Column<int>(nullable: false),
                    Duration = table.Column<string>(nullable: true),
                    Poster = table.Column<string>(nullable: true),
                    ImdbRating = table.Column<string>(nullable: true),
                    Location = table.Column<string>(nullable: true),
                    Format = table.Column<string>(nullable: true),
                    CollectionId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movie", x => x.Id);
                    table.ForeignKey(
                        name: "FK_movie_collection_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "collection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movieactors",
                columns: table => new
                {
                    MovieId = table.Column<int>(nullable: false),
                    ActorId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movieactors", x => new { x.MovieId, x.ActorId });
                    table.ForeignKey(
                        name: "FK_movieactors_actor_ActorId",
                        column: x => x.ActorId,
                        principalTable: "actor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_movieactors_movie_MovieId",
                        column: x => x.MovieId,
                        principalTable: "movie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "moviedirectors",
                columns: table => new
                {
                    MovieId = table.Column<int>(nullable: false),
                    DirectorId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moviedirectors", x => new { x.MovieId, x.DirectorId });
                    table.ForeignKey(
                        name: "FK_moviedirectors_director_DirectorId",
                        column: x => x.DirectorId,
                        principalTable: "director",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_moviedirectors_movie_MovieId",
                        column: x => x.MovieId,
                        principalTable: "movie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "moviegenre",
                columns: table => new
                {
                    MovieId = table.Column<int>(nullable: false),
                    GenreId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moviegenre", x => new { x.MovieId, x.GenreId });
                    table.ForeignKey(
                        name: "FK_moviegenre_genre_GenreId",
                        column: x => x.GenreId,
                        principalTable: "genre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_moviegenre_movie_MovieId",
                        column: x => x.MovieId,
                        principalTable: "movie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "player",
                columns: new[] { "Id", "Formats", "Name", "Path" },
                values: new object[] { 1, ".webm, .xvid, .avi, .mpg, .mp4, .3ivx, .mov, .ogm, .mkv, .asf, .wmv, .dv, .mp4, .flv", "bsplayer", "D:\\Program Files (x86)\\Webteh\\BSPlayer\\bsplayer.exe" });

            migrationBuilder.InsertData(
                table: "player",
                columns: new[] { "Id", "Formats", "Name", "Path" },
                values: new object[] { 2, ".rm, .rmvb", "RealPlayer", "D:\\Program Files\\RealPlayer\\realplay.exe" });

            migrationBuilder.CreateIndex(
                name: "IX_actor_Name",
                table: "actor",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_collection_Name",
                table: "collection",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_director_Name",
                table: "director",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_genre_Name",
                table: "genre",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_movie_CollectionId",
                table: "movie",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_movie_Title",
                table: "movie",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_movie_Year",
                table: "movie",
                column: "Year");

            migrationBuilder.CreateIndex(
                name: "IX_movieactors_ActorId",
                table: "movieactors",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_moviedirectors_DirectorId",
                table: "moviedirectors",
                column: "DirectorId");

            migrationBuilder.CreateIndex(
                name: "IX_moviegenre_GenreId",
                table: "moviegenre",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_player_Name",
                table: "player",
                column: "Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movieactors");

            migrationBuilder.DropTable(
                name: "moviedirectors");

            migrationBuilder.DropTable(
                name: "moviegenre");

            migrationBuilder.DropTable(
                name: "player");

            migrationBuilder.DropTable(
                name: "actor");

            migrationBuilder.DropTable(
                name: "director");

            migrationBuilder.DropTable(
                name: "genre");

            migrationBuilder.DropTable(
                name: "movie");

            migrationBuilder.DropTable(
                name: "collection");
        }
    }
}

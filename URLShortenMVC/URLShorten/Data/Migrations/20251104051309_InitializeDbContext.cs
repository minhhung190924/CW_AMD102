using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace URLShorten.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitializeDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.CreateTable(
            //    name: "Users",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            //        DisplayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
            //        PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
            //        CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            //        IsActive = table.Column<bool>(type: "bit", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_Users", x => x.Id);
            //    });

            migrationBuilder.CreateTable(
                name: "UrlLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    ShortenedUrl = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomAlias = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClickCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                    //UserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrlLinks", x => x.Id);
                    //table.ForeignKey(
                    //    name: "FK_UrlLinks_Users_UserId",
                    //    column: x => x.UserId,
                    //    principalTable: "Users",
                    //    principalColumn: "Id",
                    //    onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UrlLinks_ShortenedUrl",
                table: "UrlLinks",
                column: "ShortenedUrl",
                unique: true);

            //migrationBuilder.CreateIndex(
            //    name: "IX_UrlLinks_UserId",
            //    table: "UrlLinks",
            //    column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UrlLinks");

            //migrationBuilder.DropTable(
            //    name: "Users");
        }
    }
}

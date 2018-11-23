using Microsoft.EntityFrameworkCore.Migrations;

namespace Ironclad.ExternalIdentityProvider.Migrations
{
    public partial class AddExternalProviders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalIdentityProviders",
                columns: table => new
                {
                    Name = table.Column<string>(nullable: false),
                    DisplayName = table.Column<string>(nullable: true),
                    Authority = table.Column<string>(nullable: true),
                    ClientId = table.Column<string>(nullable: true),
                    CallbackPath = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalIdentityProviders", x => x.Name);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalIdentityProviders");
        }
    }
}

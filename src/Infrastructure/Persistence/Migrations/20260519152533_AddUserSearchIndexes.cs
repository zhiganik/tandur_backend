using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_users_email_trgm
                ON ""AspNetUsers"" USING GIN (""Email"" gin_trgm_ops);");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_users_phone_trgm
                ON ""AspNetUsers"" USING GIN (""PhoneNumber"" gin_trgm_ops);");

            migrationBuilder.CreateIndex(
                name: "ix_users_createdat",
                table: "AspNetUsers",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_users_email_trgm;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_users_phone_trgm;");

            migrationBuilder.DropIndex(
                name: "ix_users_createdat",
                table: "AspNetUsers");
        }
    }
}

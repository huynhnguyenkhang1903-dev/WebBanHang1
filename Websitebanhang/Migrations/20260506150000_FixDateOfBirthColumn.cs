using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Websitebanhang.Migrations
{
    public partial class FixDateOfBirthColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns 
                               WHERE object_id = OBJECT_ID(N'[AspNetUsers]') 
                               AND name = 'DateOfBirth')
                BEGIN
                    ALTER TABLE [AspNetUsers] ADD [DateOfBirth] datetime2 NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Usually we don't want to drop it if it was added as a fix, 
            // but for completeness:
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns 
                           WHERE object_id = OBJECT_ID(N'[AspNetUsers]') 
                           AND name = 'DateOfBirth')
                BEGIN
                    ALTER TABLE [AspNetUsers] DROP COLUMN [DateOfBirth];
                END
            ");
        }
    }
}

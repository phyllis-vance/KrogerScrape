using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KrogerScrape.Entities.Migrations.Sqlite
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptIds",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserEntityId = table.Column<long>(nullable: false),
                    DivisionNumber = table.Column<string>(nullable: true),
                    StoreNumber = table.Column<string>(nullable: true),
                    TransactionDate = table.Column<string>(nullable: true),
                    TerminalNumber = table.Column<string>(nullable: true),
                    TransactionId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptIds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiptIds_Users_UserEntityId",
                        column: x => x.UserEntityId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Operations",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(nullable: false),
                    StartedTimestamp = table.Column<DateTimeOffset>(nullable: false),
                    CompletedTimestamp = table.Column<DateTimeOffset>(nullable: true),
                    ParentId = table.Column<long>(nullable: true),
                    UserEntityId = table.Column<long>(nullable: true),
                    ReceiptEntityId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Operations_Users_UserEntityId",
                        column: x => x.UserEntityId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Operations_ReceiptIds_ReceiptEntityId",
                        column: x => x.ReceiptEntityId,
                        principalTable: "ReceiptIds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Operations_Operations_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Responses",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OperationEntityId = table.Column<long>(nullable: false),
                    RequestType = table.Column<int>(nullable: false),
                    CompletedTimestamp = table.Column<DateTimeOffset>(nullable: false),
                    Method = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true),
                    CompressionType = table.Column<int>(nullable: false),
                    Body = table.Column<byte[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Responses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Responses_Operations_OperationEntityId",
                        column: x => x.OperationEntityId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Operations_UserEntityId",
                table: "Operations",
                column: "UserEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Operations_ReceiptEntityId",
                table: "Operations",
                column: "ReceiptEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Operations_ParentId",
                table: "Operations",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptIds_UserEntityId_DivisionNumber_StoreNumber_TransactionDate_TerminalNumber_TransactionId",
                table: "ReceiptIds",
                columns: new[] { "UserEntityId", "DivisionNumber", "StoreNumber", "TransactionDate", "TerminalNumber", "TransactionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Responses_OperationEntityId",
                table: "Responses",
                column: "OperationEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Responses");

            migrationBuilder.DropTable(
                name: "Operations");

            migrationBuilder.DropTable(
                name: "ReceiptIds");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

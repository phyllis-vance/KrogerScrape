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
                    Email = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Responses",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OperationEntityId = table.Column<long>(nullable: false),
                    RequestId = table.Column<string>(nullable: false),
                    RequestType = table.Column<int>(nullable: false),
                    CompletedTimestamp = table.Column<DateTimeOffset>(nullable: false),
                    Method = table.Column<string>(nullable: false),
                    Url = table.Column<string>(nullable: false),
                    CompressionType = table.Column<int>(nullable: false),
                    Bytes = table.Column<byte[]>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Responses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Receipts",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserEntityId = table.Column<long>(nullable: false),
                    DivisionNumber = table.Column<string>(nullable: false),
                    StoreNumber = table.Column<string>(nullable: false),
                    TransactionDate = table.Column<string>(nullable: false),
                    TerminalNumber = table.Column<string>(nullable: false),
                    TransactionId = table.Column<string>(nullable: false),
                    ReceiptResponseEntityId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Receipts_Responses_ReceiptResponseEntityId",
                        column: x => x.ReceiptResponseEntityId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Receipts_Users_UserEntityId",
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
                        name: "FK_Operations_Receipts_ReceiptEntityId",
                        column: x => x.ReceiptEntityId,
                        principalTable: "Receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Operations_Operations_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "IX_Receipts_ReceiptResponseEntityId",
                table: "Receipts",
                column: "ReceiptResponseEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_UserEntityId_DivisionNumber_StoreNumber_TransactionDate_TerminalNumber_TransactionId",
                table: "Receipts",
                columns: new[] { "UserEntityId", "DivisionNumber", "StoreNumber", "TransactionDate", "TerminalNumber", "TransactionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Responses_OperationEntityId",
                table: "Responses",
                column: "OperationEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Responses_RequestId",
                table: "Responses",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Responses_Operations_OperationEntityId",
                table: "Responses",
                column: "OperationEntityId",
                principalTable: "Operations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Operations_Users_UserEntityId",
                table: "Operations");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_Users_UserEntityId",
                table: "Receipts");

            migrationBuilder.DropForeignKey(
                name: "FK_Operations_Receipts_ReceiptEntityId",
                table: "Operations");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Receipts");

            migrationBuilder.DropTable(
                name: "Responses");

            migrationBuilder.DropTable(
                name: "Operations");
        }
    }
}

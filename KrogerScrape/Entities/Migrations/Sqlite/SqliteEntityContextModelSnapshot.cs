﻿// <auto-generated />
using System;
using KrogerScrape.Client;
using KrogerScrape.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KrogerScrape.Entities.Migrations.Sqlite
{
    [DbContext(typeof(SqliteEntityContext))]
    partial class SqliteEntityContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.0-rtm-35687");

            modelBuilder.Entity("KrogerScrape.Entities.OperationEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTimeOffset?>("CompletedTimestamp");

                    b.Property<long?>("ParentId");

                    b.Property<DateTimeOffset>("StartedTimestamp");

                    b.Property<int>("Type");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.ToTable("Operations");

                    b.HasDiscriminator<int>("Type").HasValue(1);
                });

            modelBuilder.Entity("KrogerScrape.Entities.ReceiptIdEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("DivisionNumber");

                    b.Property<string>("StoreNumber");

                    b.Property<string>("TerminalNumber");

                    b.Property<string>("TransactionDate");

                    b.Property<string>("TransactionId");

                    b.Property<long>("UserEntityId");

                    b.HasKey("Id");

                    b.HasIndex("UserEntityId", "DivisionNumber", "StoreNumber", "TransactionDate", "TerminalNumber", "TransactionId")
                        .IsUnique();

                    b.ToTable("ReceiptIds");
                });

            modelBuilder.Entity("KrogerScrape.Entities.ResponseEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Body");

                    b.Property<DateTimeOffset>("CompletedTimestamp");

                    b.Property<string>("Method");

                    b.Property<long>("OperationEntityId");

                    b.Property<int>("RequestType");

                    b.Property<string>("Url");

                    b.HasKey("Id");

                    b.HasIndex("OperationEntityId");

                    b.ToTable("Responses");
                });

            modelBuilder.Entity("KrogerScrape.Entities.UserEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Email");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("KrogerScrape.Entities.CommandEntity", b =>
                {
                    b.HasBaseType("KrogerScrape.Entities.OperationEntity");

                    b.Property<long>("UserEntityId");

                    b.HasIndex("UserEntityId");

                    b.HasDiscriminator().HasValue(0);
                });

            modelBuilder.Entity("KrogerScrape.Entities.GetReceiptEntity", b =>
                {
                    b.HasBaseType("KrogerScrape.Entities.OperationEntity");

                    b.Property<long>("ReceiptEntityId");

                    b.HasIndex("ReceiptEntityId");

                    b.HasDiscriminator().HasValue(4);
                });

            modelBuilder.Entity("KrogerScrape.Entities.GetReceiptSummariesEntity", b =>
                {
                    b.HasBaseType("KrogerScrape.Entities.OperationEntity");

                    b.HasDiscriminator().HasValue(3);
                });

            modelBuilder.Entity("KrogerScrape.Entities.SignInEntity", b =>
                {
                    b.HasBaseType("KrogerScrape.Entities.OperationEntity");

                    b.HasDiscriminator().HasValue(2);
                });

            modelBuilder.Entity("KrogerScrape.Entities.OperationEntity", b =>
                {
                    b.HasOne("KrogerScrape.Entities.OperationEntity", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentId");
                });

            modelBuilder.Entity("KrogerScrape.Entities.ReceiptIdEntity", b =>
                {
                    b.HasOne("KrogerScrape.Entities.UserEntity", "UserEntity")
                        .WithMany("ReceiptIdEntities")
                        .HasForeignKey("UserEntityId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("KrogerScrape.Entities.ResponseEntity", b =>
                {
                    b.HasOne("KrogerScrape.Entities.OperationEntity", "OperationEntity")
                        .WithMany("ResponseEntities")
                        .HasForeignKey("OperationEntityId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("KrogerScrape.Entities.CommandEntity", b =>
                {
                    b.HasOne("KrogerScrape.Entities.UserEntity", "UserEntity")
                        .WithMany("CommandEntities")
                        .HasForeignKey("UserEntityId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("KrogerScrape.Entities.GetReceiptEntity", b =>
                {
                    b.HasOne("KrogerScrape.Entities.ReceiptIdEntity", "ReceiptEntity")
                        .WithMany("GetReceiptOperationEntities")
                        .HasForeignKey("ReceiptEntityId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}

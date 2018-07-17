using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using cn.hitokoto.statusReport.DbModels;

namespace cn.hitokoto.statusReport.Migrations
{
    [DbContext(typeof(StatisticsContext))]
    partial class StatisticsContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.2");

            modelBuilder.Entity("cn.hitokoto.statusReport.DbModels.buffer", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("identification");

                    b.Property<string>("startTS");

                    b.HasKey("id");

                    b.ToTable("Buffer");
                });

            modelBuilder.Entity("cn.hitokoto.statusReport.DbModels.log", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("identification");

                    b.Property<long>("ts");

                    b.Property<string>("type");

                    b.HasKey("id");

                    b.ToTable("Log");
                });

            modelBuilder.Entity("cn.hitokoto.statusReport.DbModels.status", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd();

                    b.Property<uint>("down");

                    b.Property<string>("identification");

                    b.Property<uint>("totol");

                    b.Property<uint>("up");

                    b.HasKey("id");

                    b.ToTable("Status");
                });
        }
    }
}

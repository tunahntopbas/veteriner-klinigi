using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VeterinerKlinigi.Migrations
{
    /// <inheritdoc />
    public partial class IlkOlusturma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AdSoyad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Klinikler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ad = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Adres = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Telefon = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Klinikler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Uzmanliklar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ad = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uzmanliklar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvcilHayvanlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SahipId = table.Column<string>(type: "text", nullable: false),
                    Ad = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Tur = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Irk = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    DogumTarihi = table.Column<DateOnly>(type: "date", nullable: false),
                    Kilo = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    FotografDosyaAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    KayitZamani = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvcilHayvanlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvcilHayvanlar_AspNetUsers_SahipId",
                        column: x => x.SahipId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "McpErisimAnahtarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KullaniciId = table.Column<string>(type: "text", nullable: false),
                    Ad = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    AnahtarHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OnEk = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    OlusturmaZamani = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SonKullanimZamani = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IptalEdildi = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McpErisimAnahtarlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_McpErisimAnahtarlari_AspNetUsers_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalismaSaatleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KlinikId = table.Column<int>(type: "integer", nullable: false),
                    Gun = table.Column<int>(type: "integer", nullable: false),
                    AcilisSaati = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    KapanisSaati = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Kapali = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalismaSaatleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalismaSaatleri_Klinikler_KlinikId",
                        column: x => x.KlinikId,
                        principalTable: "Klinikler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hizmetler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KlinikId = table.Column<int>(type: "integer", nullable: false),
                    Ad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SureDakika = table.Column<int>(type: "integer", nullable: false),
                    Ucret = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hizmetler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hizmetler_Klinikler_KlinikId",
                        column: x => x.KlinikId,
                        principalTable: "Klinikler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Veterinerler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KlinikId = table.Column<int>(type: "integer", nullable: false),
                    KullaniciId = table.Column<string>(type: "text", nullable: false),
                    AdSoyad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Unvan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Telefon = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veterinerler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Veterinerler_AspNetUsers_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Veterinerler_Klinikler_KlinikId",
                        column: x => x.KlinikId,
                        principalTable: "Klinikler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AsiKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvcilHayvanId = table.Column<int>(type: "integer", nullable: false),
                    AsiAdi = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    YapildigiTarih = table.Column<DateOnly>(type: "date", nullable: false),
                    SonrakiDozTarihi = table.Column<DateOnly>(type: "date", nullable: true),
                    Notlar = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsiKayitlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsiKayitlari_EvcilHayvanlar_EvcilHayvanId",
                        column: x => x.EvcilHayvanId,
                        principalTable: "EvcilHayvanlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Randevular",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvcilHayvanId = table.Column<int>(type: "integer", nullable: false),
                    HizmetId = table.Column<int>(type: "integer", nullable: false),
                    VeterinerId = table.Column<int>(type: "integer", nullable: false),
                    BaslangicZamani = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BitisZamani = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SureDakika = table.Column<int>(type: "integer", nullable: false),
                    Ucret = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    SahipNotu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RedNedeni = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    OlusturmaZamani = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Randevular", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Randevular_EvcilHayvanlar_EvcilHayvanId",
                        column: x => x.EvcilHayvanId,
                        principalTable: "EvcilHayvanlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Randevular_Hizmetler_HizmetId",
                        column: x => x.HizmetId,
                        principalTable: "Hizmetler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Randevular_Veterinerler_VeterinerId",
                        column: x => x.VeterinerId,
                        principalTable: "Veterinerler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VeterinerHizmetleri",
                columns: table => new
                {
                    HizmetlerId = table.Column<int>(type: "integer", nullable: false),
                    VeterinerlerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeterinerHizmetleri", x => new { x.HizmetlerId, x.VeterinerlerId });
                    table.ForeignKey(
                        name: "FK_VeterinerHizmetleri_Hizmetler_HizmetlerId",
                        column: x => x.HizmetlerId,
                        principalTable: "Hizmetler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VeterinerHizmetleri_Veterinerler_VeterinerlerId",
                        column: x => x.VeterinerlerId,
                        principalTable: "Veterinerler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VeterinerMusaitlikleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VeterinerId = table.Column<int>(type: "integer", nullable: false),
                    Gun = table.Column<int>(type: "integer", nullable: false),
                    BaslangicSaati = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    BitisSaati = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeterinerMusaitlikleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VeterinerMusaitlikleri_Veterinerler_VeterinerId",
                        column: x => x.VeterinerId,
                        principalTable: "Veterinerler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VeterinerUzmanliklari",
                columns: table => new
                {
                    UzmanliklarId = table.Column<int>(type: "integer", nullable: false),
                    VeterinerlerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeterinerUzmanliklari", x => new { x.UzmanliklarId, x.VeterinerlerId });
                    table.ForeignKey(
                        name: "FK_VeterinerUzmanliklari_Uzmanliklar_UzmanliklarId",
                        column: x => x.UzmanliklarId,
                        principalTable: "Uzmanliklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VeterinerUzmanliklari_Veterinerler_VeterinerlerId",
                        column: x => x.VeterinerlerId,
                        principalTable: "Veterinerler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MuayeneNotlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RandevuId = table.Column<int>(type: "integer", nullable: false),
                    Teshis = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Tedavi = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notlar = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    YazilmaZamani = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuayeneNotlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MuayeneNotlari_Randevular_RandevuId",
                        column: x => x.RandevuId,
                        principalTable: "Randevular",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsiKayitlari_EvcilHayvanId",
                table: "AsiKayitlari",
                column: "EvcilHayvanId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalismaSaatleri_KlinikId_Gun",
                table: "CalismaSaatleri",
                columns: new[] { "KlinikId", "Gun" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvcilHayvanlar_SahipId",
                table: "EvcilHayvanlar",
                column: "SahipId");

            migrationBuilder.CreateIndex(
                name: "IX_Hizmetler_KlinikId",
                table: "Hizmetler",
                column: "KlinikId");

            migrationBuilder.CreateIndex(
                name: "IX_McpErisimAnahtarlari_AnahtarHash",
                table: "McpErisimAnahtarlari",
                column: "AnahtarHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_McpErisimAnahtarlari_KullaniciId",
                table: "McpErisimAnahtarlari",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_MuayeneNotlari_RandevuId",
                table: "MuayeneNotlari",
                column: "RandevuId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_EvcilHayvanId",
                table: "Randevular",
                column: "EvcilHayvanId");

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_HizmetId",
                table: "Randevular",
                column: "HizmetId");

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_VeterinerId_BaslangicZamani",
                table: "Randevular",
                columns: new[] { "VeterinerId", "BaslangicZamani" });

            migrationBuilder.CreateIndex(
                name: "IX_Uzmanliklar_Ad",
                table: "Uzmanliklar",
                column: "Ad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VeterinerHizmetleri_VeterinerlerId",
                table: "VeterinerHizmetleri",
                column: "VeterinerlerId");

            migrationBuilder.CreateIndex(
                name: "IX_Veterinerler_KlinikId",
                table: "Veterinerler",
                column: "KlinikId");

            migrationBuilder.CreateIndex(
                name: "IX_Veterinerler_KullaniciId",
                table: "Veterinerler",
                column: "KullaniciId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VeterinerMusaitlikleri_VeterinerId",
                table: "VeterinerMusaitlikleri",
                column: "VeterinerId");

            migrationBuilder.CreateIndex(
                name: "IX_VeterinerUzmanliklari_VeterinerlerId",
                table: "VeterinerUzmanliklari",
                column: "VeterinerlerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsiKayitlari");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CalismaSaatleri");

            migrationBuilder.DropTable(
                name: "McpErisimAnahtarlari");

            migrationBuilder.DropTable(
                name: "MuayeneNotlari");

            migrationBuilder.DropTable(
                name: "VeterinerHizmetleri");

            migrationBuilder.DropTable(
                name: "VeterinerMusaitlikleri");

            migrationBuilder.DropTable(
                name: "VeterinerUzmanliklari");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Randevular");

            migrationBuilder.DropTable(
                name: "Uzmanliklar");

            migrationBuilder.DropTable(
                name: "EvcilHayvanlar");

            migrationBuilder.DropTable(
                name: "Hizmetler");

            migrationBuilder.DropTable(
                name: "Veterinerler");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Klinikler");
        }
    }
}

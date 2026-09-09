    using Microsoft.EntityFrameworkCore.Migrations;

    #nullable disable

    namespace Cashere.Data.Migrations
    {
        /// <inheritdoc />
        public partial class AddProductPhotoPath : Migration
        {
            /// <inheritdoc />
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.AddColumn<string>(
                    name: "PhotoPath",
                    table: "Products",
                    type: "TEXT",
                    maxLength: 500,
                    nullable: true);
            }

            /// <inheritdoc />
            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropColumn(
                    name: "PhotoPath",
                    table: "Products");
            }
        }
    }

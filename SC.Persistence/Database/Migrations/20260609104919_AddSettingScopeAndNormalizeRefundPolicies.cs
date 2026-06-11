using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingScopeAndNormalizeRefundPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "Settings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Settings"
                SET "Group" = 'PAYMENT',
                    "Scope" = 'TOP_UP',
                    "Type" = 'decimal'
                WHERE UPPER("Code") IN (
                    'VND_PER_POINT',
                    'MIN_TOPUP_AMOUNT',
                    'MAX_TOPUP_AMOUNT'
                );

                UPDATE "Settings"
                SET "Scope" = 'DEFAULT'
                WHERE "Scope" IS NULL
                  AND UPPER("Group") <> 'REFUND_POLICY';

                DO $$
                DECLARE
                    policy_record RECORD;
                    policy_value JSONB;
                    policy_scope TEXT;
                    refund_percent NUMERIC;
                    requires_image BOOLEAN;
                BEGIN
                    FOR policy_record IN
                        SELECT *
                        FROM "Settings"
                        WHERE UPPER("Group") = 'REFUND_POLICY'
                    LOOP
                        BEGIN
                            policy_value := policy_record."Value"::jsonb;
                        EXCEPTION WHEN OTHERS THEN
                            RAISE EXCEPTION
                                'Refund policy % contains invalid JSON.',
                                policy_record."Code";
                        END;

                        IF jsonb_typeof(policy_value) <> 'object'
                           OR NOT policy_value ? 'percent'
                           OR NOT policy_value ? 'requiresImage'
                        THEN
                            RAISE EXCEPTION
                                'Refund policy % is missing percent or requiresImage.',
                                policy_record."Code";
                        END IF;

                        BEGIN
                            refund_percent := (policy_value ->> 'percent')::numeric;
                            requires_image := (policy_value ->> 'requiresImage')::boolean;
                        EXCEPTION WHEN OTHERS THEN
                            RAISE EXCEPTION
                                'Refund policy % has invalid percent or requiresImage.',
                                policy_record."Code";
                        END;

                        IF refund_percent <= 0 OR refund_percent > 100 THEN
                            RAISE EXCEPTION
                                'Refund policy % percent must be greater than 0 and at most 100.',
                                policy_record."Code";
                        END IF;

                        policy_scope := UPPER(
                            REGEXP_REPLACE(
                                policy_record."Code",
                                '^REFUND_POLICY_',
                                '',
                                'i'));

                        IF policy_scope = '' THEN
                            policy_scope := UPPER(policy_record."Code");
                        END IF;

                        INSERT INTO "Settings" (
                            "Id",
                            "Code",
                            "Name",
                            "Description",
                            "Group",
                            "Scope",
                            "Value",
                            "Type",
                            "IsDeleted",
                            "CreatedAtUtc",
                            "UpdatedAtUtc",
                            "CreatedBy",
                            "UpdatedBy")
                        VALUES
                            (
                                gen_random_uuid(),
                                'DESCRIPTION',
                                'Policy description',
                                'Description of the refund policy.',
                                'REFUND_POLICY',
                                policy_scope,
                                policy_record."Description",
                                'string',
                                policy_record."IsDeleted",
                                policy_record."CreatedAtUtc",
                                policy_record."UpdatedAtUtc",
                                policy_record."CreatedBy",
                                policy_record."UpdatedBy"
                            ),
                            (
                                gen_random_uuid(),
                                'PERCENT',
                                'Refund percent',
                                'Refund percentage for this policy.',
                                'REFUND_POLICY',
                                policy_scope,
                                refund_percent::text,
                                'decimal',
                                policy_record."IsDeleted",
                                policy_record."CreatedAtUtc",
                                policy_record."UpdatedAtUtc",
                                policy_record."CreatedBy",
                                policy_record."UpdatedBy"
                            ),
                            (
                                gen_random_uuid(),
                                'REQUIRES_IMAGE',
                                'Requires image',
                                'Whether evidence images are required.',
                                'REFUND_POLICY',
                                policy_scope,
                                LOWER(requires_image::text),
                                'bool',
                                policy_record."IsDeleted",
                                policy_record."CreatedAtUtc",
                                policy_record."UpdatedAtUtc",
                                policy_record."CreatedBy",
                                policy_record."UpdatedBy"
                            );

                        UPDATE "Settings"
                        SET "Code" = 'NAME',
                            "Name" = 'Policy name',
                            "Description" = 'Display name of the refund policy.',
                            "Group" = 'REFUND_POLICY',
                            "Scope" = policy_scope,
                            "Value" = policy_record."Name",
                            "Type" = 'string'
                        WHERE "Id" = policy_record."Id";
                    END LOOP;
                END $$;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Scope",
                table: "Settings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Settings_Group_Scope_Code",
                table: "Settings",
                columns: new[] { "Group", "Scope", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Settings_Group_Scope_Code",
                table: "Settings");

            migrationBuilder.Sql(
                """
                UPDATE "Settings" AS name_setting
                SET "Code" = 'REFUND_POLICY_' || name_setting."Scope",
                    "Name" = name_setting."Value",
                    "Description" = COALESCE(description_setting."Value", ''),
                    "Value" = jsonb_build_object(
                        'percent',
                        percent_setting."Value"::numeric,
                        'requiresImage',
                        requires_image_setting."Value"::boolean)::text,
                    "Type" = 'json'
                FROM "Settings" AS percent_setting,
                     "Settings" AS requires_image_setting
                LEFT JOIN "Settings" AS description_setting
                    ON description_setting."Group" = 'REFUND_POLICY'
                   AND description_setting."Scope" = requires_image_setting."Scope"
                   AND description_setting."Code" = 'DESCRIPTION'
                WHERE name_setting."Group" = 'REFUND_POLICY'
                  AND name_setting."Code" = 'NAME'
                  AND percent_setting."Group" = 'REFUND_POLICY'
                  AND percent_setting."Scope" = name_setting."Scope"
                  AND percent_setting."Code" = 'PERCENT'
                  AND requires_image_setting."Group" = 'REFUND_POLICY'
                  AND requires_image_setting."Scope" = name_setting."Scope"
                  AND requires_image_setting."Code" = 'REQUIRES_IMAGE';

                DELETE FROM "Settings"
                WHERE UPPER("Group") = 'REFUND_POLICY'
                  AND "Code" <> 'NAME'
                  AND "Code" NOT LIKE 'REFUND_POLICY_%';
                """);

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "Settings");
        }
    }
}

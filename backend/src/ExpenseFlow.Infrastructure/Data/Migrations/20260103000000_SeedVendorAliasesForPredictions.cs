using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations;

/// <summary>
/// Seeds vendor aliases to enable expense prediction matching.
/// Maps common bank transaction description patterns to canonical vendor names
/// used in expense patterns.
/// </summary>
public partial class SeedVendorAliasesForPredictions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Insert vendor aliases that map transaction descriptions to canonical names
        // These patterns use PostgreSQL ILIKE for case-insensitive contains matching

        // Software/Subscriptions
        migrationBuilder.Sql(@"
            INSERT INTO ""VendorAliases"" (""Id"", ""CanonicalName"", ""AliasPattern"", ""DisplayName"", ""Confidence"", ""MatchCount"", ""Category"", ""CreatedAt"", ""UpdatedAt"")
            VALUES
            -- Cursor AI: matches ""CURSOR  AI POWERED IDE""
            (gen_random_uuid(), 'CURSOR AI', 'CURSOR', 'Cursor AI', 1.0, 0, 0, NOW(), NOW()),

            -- Amazon: matches various Amazon transaction patterns
            (gen_random_uuid(), 'AMAZON', 'AMAZON', 'Amazon', 1.0, 0, 0, NOW(), NOW()),
            (gen_random_uuid(), 'AMAZON', 'AMZN', 'Amazon', 1.0, 0, 0, NOW(), NOW()),

            -- Addigy: matches ""ADDIGY  INC""
            (gen_random_uuid(), 'ADDIGY', 'ADDIGY', 'Addigy', 1.0, 0, 0, NOW(), NOW()),

            -- GoDaddy: matches ""PAYPAL *GODADDY.COM""
            (gen_random_uuid(), 'GODADDY', 'GODADDY', 'GoDaddy', 1.0, 0, 0, NOW(), NOW()),

            -- OpenAI/ChatGPT: matches ""GOOGLE *ChatGPT""
            (gen_random_uuid(), 'OPENAI', 'CHATGPT', 'OpenAI', 1.0, 0, 0, NOW(), NOW()),
            (gen_random_uuid(), 'OPENAI', 'OPENAI', 'OpenAI', 1.0, 0, 0, NOW(), NOW()),

            -- Anthropic Claude
            (gen_random_uuid(), 'ANTHROPIC CLAUDE', 'ANTHROPIC', 'Anthropic Claude', 1.0, 0, 0, NOW(), NOW()),
            (gen_random_uuid(), 'ANTHROPIC CLAUDE', 'CLAUDE', 'Anthropic Claude', 1.0, 0, 0, NOW(), NOW()),

            -- Fireflies.AI
            (gen_random_uuid(), 'FIREFLIES.AI', 'FIREFLIES', 'Fireflies.AI', 1.0, 0, 0, NOW(), NOW()),

            -- ElevenLabs
            (gen_random_uuid(), 'ELEVENLABS', 'ELEVENLABS', 'ElevenLabs', 1.0, 0, 0, NOW(), NOW()),
            (gen_random_uuid(), 'ELEVENLABS', 'ELEVEN LABS', 'ElevenLabs', 1.0, 0, 0, NOW(), NOW()),

            -- Superhuman
            (gen_random_uuid(), 'SUPERHUMAN', 'SUPERHUMAN', 'Superhuman', 1.0, 0, 0, NOW(), NOW()),

            -- Gamma
            (gen_random_uuid(), 'GAMMA', 'GAMMA', 'Gamma', 1.0, 0, 0, NOW(), NOW()),

            -- Foxit
            (gen_random_uuid(), 'FOXIT', 'FOXIT', 'Foxit', 1.0, 0, 0, NOW(), NOW()),

            -- Twilio
            (gen_random_uuid(), 'TWILIO', 'TWILIO', 'Twilio', 1.0, 0, 0, NOW(), NOW()),

            -- JetBrains
            (gen_random_uuid(), 'JETBRAINS PYCHARM', 'JETBRAINS', 'JetBrains PyCharm', 1.0, 0, 0, NOW(), NOW()),
            (gen_random_uuid(), 'JETBRAINS PYCHARM', 'PYCHARM', 'JetBrains PyCharm', 1.0, 0, 0, NOW(), NOW()),

            -- SAP Crystal Reports
            (gen_random_uuid(), 'SAP CRYSTAL REPORTS', 'CRYSTAL', 'SAP Crystal Reports', 1.0, 0, 0, NOW(), NOW()),
            (gen_random_uuid(), 'SAP CRYSTAL REPORTS', 'SAP', 'SAP Crystal Reports', 0.8, 0, 0, NOW(), NOW())

            ON CONFLICT DO NOTHING;
        ");

        // Travel & Hotels
        migrationBuilder.Sql(@"
            INSERT INTO ""VendorAliases"" (""Id"", ""CanonicalName"", ""AliasPattern"", ""DisplayName"", ""Confidence"", ""MatchCount"", ""Category"", ""CreatedAt"", ""UpdatedAt"")
            VALUES
            -- Delta Airlines
            (gen_random_uuid(), 'DELTA AIRLINES', 'DELTA', 'Delta Airlines', 1.0, 0, 1, NOW(), NOW()),

            -- American Airlines
            (gen_random_uuid(), 'AMERICAN AIRLINES', 'AMERICAN', 'American Airlines', 0.9, 0, 1, NOW(), NOW()),
            (gen_random_uuid(), 'AMERICAN AIRLINES', 'AA ', 'American Airlines', 1.0, 0, 1, NOW(), NOW()),

            -- Lyft
            (gen_random_uuid(), 'LYFT', 'LYFT', 'Lyft', 1.0, 0, 0, NOW(), NOW()),

            -- Hampton Inn
            (gen_random_uuid(), 'HAMPTON INN', 'HAMPTON', 'Hampton Inn', 1.0, 0, 2, NOW(), NOW()),

            -- DoubleTree
            (gen_random_uuid(), 'DOUBLETREE', 'DOUBLETREE', 'DoubleTree', 1.0, 0, 2, NOW(), NOW()),

            -- Tribute Portfolio
            (gen_random_uuid(), 'TRIBUTE PORTFOLIO HOTEL', 'TRIBUTE', 'Tribute Portfolio Hotel', 1.0, 0, 2, NOW(), NOW()),

            -- Hertz
            (gen_random_uuid(), 'HERTZ', 'HERTZ', 'Hertz', 1.0, 0, 0, NOW(), NOW()),

            -- RDU Airport Parking
            (gen_random_uuid(), 'RDU AIRPORT PARKING', 'RDU', 'RDU Airport Parking', 0.9, 0, 0, NOW(), NOW()),
            (gen_random_uuid(), 'RDU AIRPORT PARKING', 'AIRPORT PARKING', 'RDU Airport Parking', 0.8, 0, 0, NOW(), NOW())

            ON CONFLICT DO NOTHING;
        ");

        // Telecom & Utilities
        migrationBuilder.Sql(@"
            INSERT INTO ""VendorAliases"" (""Id"", ""CanonicalName"", ""AliasPattern"", ""DisplayName"", ""Confidence"", ""MatchCount"", ""Category"", ""CreatedAt"", ""UpdatedAt"")
            VALUES
            -- AT&T
            (gen_random_uuid(), 'AT&T', 'AT&T', 'AT&T', 1.0, 0, 3, NOW(), NOW()),
            (gen_random_uuid(), 'AT&T', 'ATT', 'AT&T', 0.9, 0, 3, NOW(), NOW()),

            -- Google Fiber
            (gen_random_uuid(), 'GOOGLE FIBER', 'GOOGLE FIBER', 'Google Fiber', 1.0, 0, 3, NOW(), NOW()),

            -- Starlink
            (gen_random_uuid(), 'STARLINK', 'STARLINK', 'Starlink', 1.0, 0, 3, NOW(), NOW())

            ON CONFLICT DO NOTHING;
        ");

        // Equipment & Office
        migrationBuilder.Sql(@"
            INSERT INTO ""VendorAliases"" (""Id"", ""CanonicalName"", ""AliasPattern"", ""DisplayName"", ""Confidence"", ""MatchCount"", ""Category"", ""CreatedAt"", ""UpdatedAt"")
            VALUES
            -- Dell
            (gen_random_uuid(), 'DELL', 'DELL', 'Dell', 1.0, 0, 0, NOW(), NOW()),

            -- Apple
            (gen_random_uuid(), 'APPLE', 'APPLE', 'Apple', 1.0, 0, 0, NOW(), NOW()),

            -- Best Buy
            (gen_random_uuid(), 'BEST BUY', 'BEST BUY', 'Best Buy', 1.0, 0, 0, NOW(), NOW()),
            (gen_random_uuid(), 'BEST BUY', 'BESTBUY', 'Best Buy', 1.0, 0, 0, NOW(), NOW()),

            -- Owl Labs
            (gen_random_uuid(), 'OWL LABS', 'OWL LABS', 'Owl Labs', 1.0, 0, 0, NOW(), NOW()),
            (gen_random_uuid(), 'OWL LABS', 'OWLLABS', 'Owl Labs', 1.0, 0, 0, NOW(), NOW()),

            -- UPS
            (gen_random_uuid(), 'UPS', 'UPS', 'UPS', 1.0, 0, 0, NOW(), NOW()),

            -- Walmart
            (gen_random_uuid(), 'WALMART', 'WALMART', 'Walmart', 1.0, 0, 0, NOW(), NOW())

            ON CONFLICT DO NOTHING;
        ");

        // Restaurants & Food
        migrationBuilder.Sql(@"
            INSERT INTO ""VendorAliases"" (""Id"", ""CanonicalName"", ""AliasPattern"", ""DisplayName"", ""Confidence"", ""MatchCount"", ""Category"", ""CreatedAt"", ""UpdatedAt"")
            VALUES
            -- Copeland's
            (gen_random_uuid(), 'COPELAND''S RESTAURANT', 'COPELAND', 'Copeland''s Restaurant', 1.0, 0, 0, NOW(), NOW()),

            -- Chili's
            (gen_random_uuid(), 'CHILIS', 'CHILI', 'Chili''s', 1.0, 0, 0, NOW(), NOW()),

            -- Five Guys
            (gen_random_uuid(), 'FIVE GUYS', 'FIVE GUYS', 'Five Guys', 1.0, 0, 0, NOW(), NOW()),

            -- Buffalo Wild Wings
            (gen_random_uuid(), 'BUFFALO WILD WINGS', 'BUFFALO WILD', 'Buffalo Wild Wings', 1.0, 0, 0, NOW(), NOW()),
            (gen_random_uuid(), 'BUFFALO WILD WINGS', 'BWW', 'Buffalo Wild Wings', 1.0, 0, 0, NOW(), NOW())

            ON CONFLICT DO NOTHING;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Remove seeded vendor aliases
        migrationBuilder.Sql(@"
            DELETE FROM ""VendorAliases""
            WHERE ""CanonicalName"" IN (
                'CURSOR AI', 'AMAZON', 'ADDIGY', 'GODADDY', 'OPENAI',
                'ANTHROPIC CLAUDE', 'FIREFLIES.AI', 'ELEVENLABS', 'SUPERHUMAN',
                'GAMMA', 'FOXIT', 'TWILIO', 'JETBRAINS PYCHARM', 'SAP CRYSTAL REPORTS',
                'DELTA AIRLINES', 'AMERICAN AIRLINES', 'LYFT', 'HAMPTON INN',
                'DOUBLETREE', 'TRIBUTE PORTFOLIO HOTEL', 'HERTZ', 'RDU AIRPORT PARKING',
                'AT&T', 'GOOGLE FIBER', 'STARLINK',
                'DELL', 'APPLE', 'BEST BUY', 'OWL LABS', 'UPS', 'WALMART',
                'COPELAND''S RESTAURANT', 'CHILIS', 'FIVE GUYS', 'BUFFALO WILD WINGS'
            );
        ");
    }
}

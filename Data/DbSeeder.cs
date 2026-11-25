using CommunityWeather.Models;
using CommunityWeather.Services;

namespace CommunityWeather.Data;

public static class DbSeeder
{
    /// <summary>
    /// Creates the first administrator from configuration (SeedAdmin section). The password is hashed
    /// before it is stored; no credentials are checked against code or configuration at login time.
    /// </summary>
    public static void Seed(AppDbContext db, SeedAdminSettings? admin)
    {
        if (admin != null
            && !string.IsNullOrWhiteSpace(admin.Email)
            && System.Text.RegularExpressions.Regex.IsMatch(admin.Password ?? "", PasswordRules.Pattern)
            && !db.Users.Any(u => u.Role == Roles.Admin))
        {
            db.Users.Add(new User
            {
                FullName = string.IsNullOrWhiteSpace(admin.FullName) ? "Community Administrator" : admin.FullName.Trim(),
                Email = AccountService.NormalizeEmail(admin.Email),
                PasswordHash = PasswordHelper.Hash(admin.Password!),
                Role = Roles.Admin,
                EmailAlerts = false
            });
        }

        if (!db.PreparednessGuides.Any())
        {
            db.PreparednessGuides.AddRange(
                new PreparednessGuide
                {
                    Title = "Before a Typhoon",
                    Hazard = "Typhoon",
                    Content = "Prepare a go-bag with water, food, flashlight, batteries, medicines and copies of IDs.\nSecure windows, roofs and loose outdoor objects.\nCharge phones and power banks.\nKnow your nearest evacuation center and the route to it.\nFollow official bulletins from PAGASA and your local government."
                },
                new PreparednessGuide
                {
                    Title = "During a Flood",
                    Hazard = "Flood",
                    Content = "Move to higher ground immediately.\nDo not walk or drive through floodwater.\nTurn off the main electrical switch if it is safe to do so.\nListen to local authorities for evacuation orders."
                },
                new PreparednessGuide
                {
                    Title = "Earthquake: Drop, Cover and Hold On",
                    Hazard = "Earthquake",
                    Content = "Drop to your hands and knees.\nCover your head and neck under a sturdy table.\nHold on until the shaking stops.\nAfter shaking, check for injuries and hazards, and expect aftershocks."
                },
                new PreparednessGuide
                {
                    Title = "Fire Safety at Home",
                    Hazard = "Fire",
                    Content = "Keep exits clear and know two ways out of every room.\nNever leave cooking unattended.\nIf there is fire, get out, stay out, and call emergency services.\nCrawl low under smoke."
                },
                new PreparednessGuide
                {
                    Title = "Landslide Warning Signs",
                    Hazard = "Landslide",
                    Content = "Watch for new cracks in the ground or walls, tilting trees or poles, and muddy water in streams.\nAfter heavy rain, move away from steep slopes.\nEvacuate early when advised by authorities."
                });
        }

        if (!db.EvacuationCenters.Any())
        {
            // SAMPLE DATA ONLY - replace with real, verified facilities through the admin pages.
            db.EvacuationCenters.AddRange(
                new EvacuationCenter
                {
                    Name = "Sample Barangay Gymnasium",
                    Address = "Sample Street, Cebu City",
                    ContactNumber = "000-0000",
                    OperationalNotes = "Sample record. Replace with a verified evacuation center.",
                    FacilityType = "Evacuation Center",
                    Latitude = 10.3170,
                    Longitude = 123.8900
                },
                new EvacuationCenter
                {
                    Name = "Sample District Hospital",
                    Address = "Sample Avenue, Cebu City",
                    ContactNumber = "000-0001",
                    OperationalNotes = "Sample record. Replace with a verified facility.",
                    FacilityType = "Hospital",
                    Latitude = 10.3120,
                    Longitude = 123.8850
                });
        }

        db.SaveChanges();
    }
}

using CommunityWeather.Models;

namespace CommunityWeather.Data;

public static class AdvisoryQueries
{
    /// <summary>Published advisories that have started and have not expired.</summary>
    public static IQueryable<Advisory> Active(this IQueryable<Advisory> q, DateTime now) =>
        q.Where(a => a.Status == "Published"
                     && a.PublishedAt <= now
                     && (a.ValidUntil == null || a.ValidUntil >= now));

    /// <summary>Archived advisories, plus published ones whose validity period has ended.</summary>
    public static IQueryable<Advisory> Archived(this IQueryable<Advisory> q, DateTime now) =>
        q.Where(a => a.Status == "Archived"
                     || (a.Status == "Published" && a.ValidUntil != null && a.ValidUntil < now));
}

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CommunityWeather.Models;
using Microsoft.Extensions.Options;

namespace CommunityWeather.Services;

/// <summary>Email alerts through the SendGrid v3 Mail Send API.</summary>
public class EmailService
{
    private readonly HttpClient _http;
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(HttpClient http, IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.SendGridApiKey) && !string.IsNullOrWhiteSpace(_settings.FromEmail);

    /// <summary>Returns true when the alert was accepted by SendGrid (or there was nobody to notify).</summary>
    public async Task<bool> SendAdvisoryAsync(Advisory advisory, IReadOnlyList<(string Email, string Name)> recipients)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("SendGrid is not configured; advisory email was not sent.");
            return false;
        }
        if (recipients.Count == 0) return true;

        var body = new StringBuilder()
            .AppendLine(advisory.Title)
            .AppendLine($"Affected area: {advisory.AffectedArea}")
            .AppendLine($"Category: {advisory.Category}")
            .AppendLine()
            .AppendLine(advisory.Message)
            .AppendLine()
            .AppendLine("This is a community notice. Always follow official warnings from PAGASA and your local government.")
            .ToString();

        var allOk = true;
        foreach (var chunk in recipients.Chunk(500))
        {
            // One personalization per recipient so addresses are not exposed to each other.
            var payload = new
            {
                personalizations = chunk.Select(r => new { to = new[] { new { email = r.Email, name = r.Name } } }),
                from = new { email = _settings.FromEmail, name = _settings.FromName },
                subject = $"[{advisory.Priority.ToUpperInvariant()}] {advisory.Title}",
                content = new[] { new { type = "text/plain", value = body } }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.SendGridApiKey);

            try
            {
                using var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    allOk = false;
                    _logger.LogError("SendGrid returned {Status}: {Body}", (int)response.StatusCode,
                        await response.Content.ReadAsStringAsync());
                }
            }
            catch (HttpRequestException ex)
            {
                allOk = false;
                _logger.LogError(ex, "SendGrid request failed.");
            }
        }

        return allOk;
    }
}

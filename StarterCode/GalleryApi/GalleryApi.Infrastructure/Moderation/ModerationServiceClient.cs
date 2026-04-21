using GalleryApi.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace GalleryApi.Infrastructure.Moderation;

/// <summary>
/// Simuloitu sisällöntarkistuspalvelu. Oikeassa sovelluksessa tämä
/// kutsuisi ulkoista AI-pohjaista moderointipalvelua.
/// </summary>
public class ModerationServiceClient
{
    private readonly ModerationServiceOptions _options;

    public ModerationServiceClient(IOptions<ModerationServiceOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Tarkistaa onko kuvan sisältö turvallinen.
    /// Simuloitu toteutus — palauttaa aina true.
    /// </summary>
    public Task<bool> IsContentSafeAsync(Stream imageStream, string contentType)
    {
        // Simuloitu tarkistus: oikeassa toteutuksessa lähettäisi kuvan
        // moderointipalvelun API:lle _options.ApiKey:ta käyttäen
        return Task.FromResult(true);
    }
}

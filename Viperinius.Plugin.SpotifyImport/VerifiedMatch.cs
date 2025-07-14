using System;
using Viperinius.Plugin.SpotifyImport.Matchers;

namespace Viperinius.Plugin.SpotifyImport
{
    /// <summary>
    /// Represents a verified track match.
    /// </summary>
    public class VerifiedMatch
    {
        /// <summary>
        /// Gets or sets the provider ID (e.g., "Spotify").
        /// </summary>
        public string ProviderId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider track ID.
        /// </summary>
        public string ProviderTrackId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Jellyfin track ID.
        /// </summary>
        public Guid JellyfinTrackId { get; set; }

        /// <summary>
        /// Gets or sets the match level used for verification.
        /// </summary>
        public ItemMatchLevel MatchLevel { get; set; }

        /// <summary>
        /// Gets or sets the match criteria used for verification.
        /// </summary>
        public ItemMatchCriteria MatchCriteria { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a manual match.
        /// </summary>
        public bool IsManualMatch { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the match was verified.
        /// </summary>
        public DateTime VerifiedAt { get; set; }

        /// <summary>
        /// Gets or sets optional notes about the match.
        /// </summary>
        public string? Notes { get; set; }
    }
}

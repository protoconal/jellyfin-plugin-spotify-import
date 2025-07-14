using System;
using System.Collections.Generic;
using Viperinius.Plugin.SpotifyImport.Matchers;

namespace Viperinius.Plugin.SpotifyImport.Api
{
    /// <summary>
    /// Response object for track matching operations.
    /// </summary>
    public class TrackMatchingResponse
    {
        /// <summary>
        /// Gets or sets the list of tracks.
        /// </summary>
        public List<TrackMatchingItem> Tracks { get; set; } = new();

        /// <summary>
        /// Gets or sets the current page number.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the total count of items.
        /// </summary>
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// Represents a track matching item.
    /// </summary>
    public class TrackMatchingItem
    {
        /// <summary>
        /// Gets or sets the provider track information.
        /// </summary>
        public ProviderTrackInfo ProviderTrack { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of potential matches.
        /// </summary>
        public List<JellyfinTrackInfo> PotentialMatches { get; set; } = new();

        /// <summary>
        /// Gets or sets the current match.
        /// </summary>
        public JellyfinTrackInfo? CurrentMatch { get; set; }

        /// <summary>
        /// Gets or sets the match type.
        /// </summary>
        public TrackMatchType MatchType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a manual match.
        /// </summary>
        public bool IsManualMatch { get; set; }

        /// <summary>
        /// Gets or sets the match level.
        /// </summary>
        public ItemMatchLevel? MatchLevel { get; set; }

        /// <summary>
        /// Gets or sets the match criteria.
        /// </summary>
        public ItemMatchCriteria? MatchCriteria { get; set; }
    }

    /// <summary>
    /// Represents Jellyfin track information.
    /// </summary>
    public class JellyfinTrackInfo
    {
        /// <summary>
        /// Gets or sets the track ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the track name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the album name.
        /// </summary>
        public string AlbumName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the artist names.
        /// </summary>
        public List<string> ArtistNames { get; set; } = new();

        /// <summary>
        /// Gets or sets the album artist names.
        /// </summary>
        public List<string> AlbumArtistNames { get; set; } = new();

        /// <summary>
        /// Gets or sets the track number.
        /// </summary>
        public int TrackNumber { get; set; }

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of differences from the provider track.
        /// </summary>
        public List<TrackDifference> Differences { get; set; } = new();
    }

    /// <summary>
    /// Represents a difference between provider and Jellyfin track data.
    /// </summary>
    public class TrackDifference
    {
        /// <summary>
        /// Gets or sets the field name that differs.
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider value.
        /// </summary>
        public string ProviderValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Jellyfin value.
        /// </summary>
        public string JellyfinValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents matched track information.
    /// </summary>
    public class MatchedTrackInfo
    {
        /// <summary>
        /// Gets or sets the provider track.
        /// </summary>
        public ProviderTrackInfo ProviderTrack { get; set; } = new();

        /// <summary>
        /// Gets or sets the Jellyfin track.
        /// </summary>
        public JellyfinTrackInfo JellyfinTrack { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether this is a manual match.
        /// </summary>
        public bool IsManualMatch { get; set; }

        /// <summary>
        /// Gets or sets the match level.
        /// </summary>
        public ItemMatchLevel MatchLevel { get; set; }

        /// <summary>
        /// Gets or sets the match criteria.
        /// </summary>
        public ItemMatchCriteria MatchCriteria { get; set; }
    }

    /// <summary>
    /// Request object for accepting track matches.
    /// </summary>
    public class TrackMatchingAcceptRequest
    {
        /// <summary>
        /// Gets or sets the provider ID.
        /// </summary>
        public string ProviderId { get; set; } = "Spotify";

        /// <summary>
        /// Gets or sets the provider track ID.
        /// </summary>
        public string ProviderTrackId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Jellyfin track ID.
        /// </summary>
        public Guid JellyfinTrackId { get; set; }
    }

    /// <summary>
    /// Response object for accepting track matches.
    /// </summary>
    public class TrackMatchingAcceptResponse
    {
        /// <summary>
        /// Gets or sets the list of results.
        /// </summary>
        public List<TrackMatchingAcceptResult> Results { get; set; } = new();
    }

    /// <summary>
    /// Result object for individual track match acceptance.
    /// </summary>
    public class TrackMatchingAcceptResult
    {
        /// <summary>
        /// Gets or sets the provider track ID.
        /// </summary>
        public string ProviderTrackId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the operation failed.
        /// </summary>
        public string? Error { get; set; }
    }

    /// <summary>
    /// Enumeration of track match types.
    /// </summary>
    public enum TrackMatchType
    {
        /// <summary>
        /// One-to-one match.
        /// </summary>
        OneToOne,

        /// <summary>
        /// One-to-many match.
        /// </summary>
        OneToMany
    }
}

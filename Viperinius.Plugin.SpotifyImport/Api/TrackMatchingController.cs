using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Viperinius.Plugin.SpotifyImport.Matchers;
using Viperinius.Plugin.SpotifyImport.Utils;

namespace Viperinius.Plugin.SpotifyImport.Api
{
    /// <summary>
    /// The API controller for track matching interface.
    /// </summary>
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize]
    public sealed class TrackMatchingController : ControllerBase, IDisposable
    {
        private readonly ILogger<TrackMatchingController> _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly DbRepository _dbRepository;
        private readonly ManualMapStore _manualMapStore;
        private readonly VerifiedMatchStore _verifiedMatchStore;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackMatchingController"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        public TrackMatchingController(
            ILogger<TrackMatchingController> logger,
            ILibraryManager libraryManager,
            ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _dbRepository = new DbRepository(Plugin.Instance?.DbPath ?? throw new InvalidOperationException("Plugin not initialized"));
            _manualMapStore = new ManualMapStore(loggerFactory.CreateLogger<ManualMapStore>());
            _verifiedMatchStore = new VerifiedMatchStore(loggerFactory.CreateLogger<VerifiedMatchStore>());
        }

        /// <summary>
        /// Gets unmatched Spotify tracks that need matching.
        /// </summary>
        /// <param name="providerId">The provider ID (e.g., "Spotify").</param>
        /// <param name="page">Page number for pagination.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <returns>List of unmatched tracks with potential matches.</returns>
        [HttpGet($"{nameof(Viperinius)}.{nameof(Viperinius.Plugin)}.{nameof(SpotifyImport)}/TrackMatching/Unmatched")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<TrackMatchingResponse> GetUnmatchedTracks(
            [FromQuery] string providerId = "Spotify",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var unmatchedTracks = GetUnmatchedTracksFromDb(providerId, page, pageSize);
                var response = new TrackMatchingResponse
                {
                    Tracks = unmatchedTracks.Select(track => new TrackMatchingItem
                    {
                        ProviderTrack = track,
                        PotentialMatches = FindPotentialMatches(track),
                        MatchType = GetMatchType(track)
                    }).ToList(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = GetUnmatchedTracksCount(providerId)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unmatched tracks");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Gets matched tracks for verification.
        /// </summary>
        /// <param name="providerId">The provider ID (e.g., "Spotify").</param>
        /// <param name="page">Page number for pagination.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <returns>List of matched tracks for verification.</returns>
        [HttpGet($"{nameof(Viperinius)}.{nameof(Viperinius.Plugin)}.{nameof(SpotifyImport)}/TrackMatching/Matched")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<TrackMatchingResponse> GetMatchedTracks(
            [FromQuery] string providerId = "Spotify",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var matchedTracks = GetMatchedTracksFromDb(providerId, page, pageSize);
                var response = new TrackMatchingResponse
                {
                    Tracks = matchedTracks.Select(item => new TrackMatchingItem
                    {
                        ProviderTrack = item.ProviderTrack,
                        CurrentMatch = item.JellyfinTrack,
                        MatchType = TrackMatchType.OneToOne,
                        IsManualMatch = item.IsManualMatch,
                        MatchLevel = item.MatchLevel,
                        MatchCriteria = item.MatchCriteria
                    }).ToList(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = GetMatchedTracksCount(providerId)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting matched tracks");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Accept manual track matches.
        /// </summary>
        /// <param name="matches">List of manual matches to accept.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result of the operation.</returns>
        [HttpPost($"{nameof(Viperinius)}.{nameof(Viperinius.Plugin)}.{nameof(SpotifyImport)}/TrackMatching/AcceptMatches")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<ActionResult<TrackMatchingAcceptResponse>> AcceptMatches(
            [FromBody, Required] IList<TrackMatchingAcceptRequest> matches,
            CancellationToken cancellationToken)
        {
            try
            {
                if (matches == null || matches.Count == 0)
                {
                    return Task.FromResult<ActionResult<TrackMatchingAcceptResponse>>(BadRequest("No matches provided"));
                }

                // Load current stores
                if (!_manualMapStore.Load())
                {
                    _logger.LogWarning("Failed to load manual map store, creating new one");
                }

                if (!_verifiedMatchStore.Load())
                {
                    _logger.LogWarning("Failed to load verified match store, creating new one");
                }

                var results = new List<TrackMatchingAcceptResult>();

                foreach (var match in matches)
                {
                    var result = ProcessMatchAcceptance(match, cancellationToken);
                    results.Add(result);
                }

                // Save stores
                var manualSaved = _manualMapStore.Save();
                var verifiedSaved = _verifiedMatchStore.Save();

                if (manualSaved && verifiedSaved)
                {
                    return Task.FromResult<ActionResult<TrackMatchingAcceptResponse>>(Ok(new TrackMatchingAcceptResponse { Results = results }));
                }
                else
                {
                    _logger.LogError("Failed to save match stores");
                    return Task.FromResult<ActionResult<TrackMatchingAcceptResponse>>(StatusCode(500, "Failed to save manual matches"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting matches");
                return Task.FromResult<ActionResult<TrackMatchingAcceptResponse>>(StatusCode(500, "Internal server error"));
            }
        }

        /// <summary>
        /// Search for potential Jellyfin matches for a specific track.
        /// </summary>
        /// <param name="providerTrackId">The provider track ID.</param>
        /// <param name="providerId">The provider ID.</param>
        /// <param name="searchQuery">Optional search query to filter results.</param>
        /// <returns>List of potential matches.</returns>
        [HttpGet($"{nameof(Viperinius)}.{nameof(Viperinius.Plugin)}.{nameof(SpotifyImport)}/TrackMatching/Search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<List<JellyfinTrackInfo>> SearchPotentialMatches(
            [FromQuery, Required] string providerTrackId,
            [FromQuery] string providerId = "Spotify",
            [FromQuery] string? searchQuery = null)
        {
            try
            {
                var trackDbId = _dbRepository.GetProviderTrackDbId(providerId, providerTrackId);
                if (trackDbId == null)
                {
                    return NotFound("Provider track not found");
                }

                var providerTrack = _dbRepository.GetProviderTrack(providerId, trackDbId.Value);
                if (providerTrack == null)
                {
                    return NotFound("Provider track not found");
                }

                var potentialMatches = FindPotentialMatches(providerTrack, searchQuery);
                return Ok(potentialMatches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching potential matches");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Disposes the controller and its resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _dbRepository?.Dispose();
            }

            _disposed = true;
        }

        private List<ProviderTrackInfo> GetUnmatchedTracksFromDb(string providerId, int page, int pageSize)
        {
            var tracks = new List<ProviderTrackInfo>();

            // For now, return empty list since we can't access the protected Connection property
            // This would need to be implemented with a public method in DbRepository
            return tracks;
        }

        private List<MatchedTrackInfo> GetMatchedTracksFromDb(string providerId, int page, int pageSize)
        {
            var tracks = new List<MatchedTrackInfo>();

            // For now, return empty list since we can't access the protected Connection property
            // This would need to be implemented with a public method in DbRepository
            return tracks;
        }

        private int GetUnmatchedTracksCount(string providerId)
        {
            // For now, return 0 since we can't access the protected Connection property
            // This would need to be implemented with a public method in DbRepository
            return 0;
        }

        private int GetMatchedTracksCount(string providerId)
        {
            // For now, return 0 since we can't access the protected Connection property
            // This would need to be implemented with a public method in DbRepository
            return 0;
        }

        private List<JellyfinTrackInfo> FindPotentialMatches(ProviderTrackInfo providerTrack, string? searchQuery = null)
        {
            var matches = new List<JellyfinTrackInfo>();

            try
            {
                // Use the existing matching logic from PlaylistSync
                var searchTerm = searchQuery ?? providerTrack.Name;
                var queryResult = _libraryManager.GetItemsResult(new MediaBrowser.Controller.Entities.InternalItemsQuery
                {
                    SearchTerm = searchTerm.Length > 50 ? searchTerm.Substring(0, 50) : searchTerm,
                    MediaTypes = new[] { "Audio" },
                    Limit = 20 // Limit to prevent too many results
                });

                foreach (var item in queryResult.Items)
                {
                    if (item is Audio audioItem)
                    {
                        var jellyfinTrack = new JellyfinTrackInfo
                        {
                            Id = audioItem.Id,
                            Name = audioItem.Name,
                            AlbumName = audioItem.Album ?? string.Empty,
                            ArtistNames = audioItem.Artists.ToList(),
                            AlbumArtistNames = audioItem.AlbumArtists.ToList(),
                            TrackNumber = audioItem.IndexNumber ?? 0,
                            Path = audioItem.Path,
                            Differences = CalculateDifferences(providerTrack, audioItem)
                        };

                        matches.Add(jellyfinTrack);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding potential matches for track {TrackName}", providerTrack.Name);
            }

            return matches.OrderBy(m => m.Differences.Count).ToList();
        }

        private TrackMatchType GetMatchType(ProviderTrackInfo track)
        {
            var potentialMatches = FindPotentialMatches(track);
            return potentialMatches.Count > 1 ? TrackMatchType.OneToMany : TrackMatchType.OneToOne;
        }

        private List<TrackDifference> CalculateDifferences(ProviderTrackInfo providerTrack, Audio jellyfinTrack)
        {
            var differences = new List<TrackDifference>();

            // Compare track names
            if (!string.Equals(providerTrack.Name, jellyfinTrack.Name, StringComparison.OrdinalIgnoreCase))
            {
                differences.Add(new TrackDifference
                {
                    Field = "TrackName",
                    ProviderValue = providerTrack.Name,
                    JellyfinValue = jellyfinTrack.Name
                });
            }

            // Compare album names
            if (!string.Equals(providerTrack.AlbumName, jellyfinTrack.Album, StringComparison.OrdinalIgnoreCase))
            {
                differences.Add(new TrackDifference
                {
                    Field = "AlbumName",
                    ProviderValue = providerTrack.AlbumName,
                    JellyfinValue = jellyfinTrack.Album ?? string.Empty
                });
            }

            // Compare artists
            var providerArtists = string.Join(", ", providerTrack.ArtistNames);
            var jellyfinArtists = string.Join(", ", jellyfinTrack.Artists);
            if (!string.Equals(providerArtists, jellyfinArtists, StringComparison.OrdinalIgnoreCase))
            {
                differences.Add(new TrackDifference
                {
                    Field = "Artists",
                    ProviderValue = providerArtists,
                    JellyfinValue = jellyfinArtists
                });
            }

            // Compare album artists
            var providerAlbumArtists = string.Join(", ", providerTrack.AlbumArtistNames);
            var jellyfinAlbumArtists = string.Join(", ", jellyfinTrack.AlbumArtists);
            if (!string.Equals(providerAlbumArtists, jellyfinAlbumArtists, StringComparison.OrdinalIgnoreCase))
            {
                differences.Add(new TrackDifference
                {
                    Field = "AlbumArtists",
                    ProviderValue = providerAlbumArtists,
                    JellyfinValue = jellyfinAlbumArtists
                });
            }

            return differences;
        }

        private TrackMatchingAcceptResult ProcessMatchAcceptance(TrackMatchingAcceptRequest match, CancellationToken cancellationToken)
        {
            try
            {
                // Get the provider track
                var trackDbId = _dbRepository.GetProviderTrackDbId(match.ProviderId, match.ProviderTrackId);
                if (trackDbId == null)
                {
                    return new TrackMatchingAcceptResult
                    {
                        ProviderTrackId = match.ProviderTrackId,
                        Success = false,
                        Error = "Provider track not found"
                    };
                }

                var providerTrack = _dbRepository.GetProviderTrack(match.ProviderId, trackDbId.Value);
                if (providerTrack == null)
                {
                    return new TrackMatchingAcceptResult
                    {
                        ProviderTrackId = match.ProviderTrackId,
                        Success = false,
                        Error = "Provider track not found"
                    };
                }

                // Get the Jellyfin track
                var jellyfinTrack = _libraryManager.GetItemById<Audio>(match.JellyfinTrackId);
                if (jellyfinTrack == null)
                {
                    return new TrackMatchingAcceptResult
                    {
                        ProviderTrackId = match.ProviderTrackId,
                        Success = false,
                        Error = "Jellyfin track not found"
                    };
                }

                // Create manual map entry
                var manualMapTrack = new ManualMapTrack
                {
                    Provider = new ManualMapTrack.ProviderTrack
                    {
                        Name = providerTrack.Name,
                        AlbumName = providerTrack.AlbumName,
                        AlbumArtistNames = providerTrack.AlbumArtistNames,
                        ArtistNames = providerTrack.ArtistNames
                    },
                    Jellyfin = new ManualMapTrack.JellyfinTrack
                    {
                        Track = jellyfinTrack.Id.ToString()
                    }
                };

                // Add to manual map store
                var existingMap = _manualMapStore.GetByProviderTrackInfo(providerTrack);
                if (existingMap != null)
                {
                    _manualMapStore.Remove(existingMap);
                }

                _manualMapStore.Add(manualMapTrack);

                // Create verified match entry
                var verifiedMatch = new VerifiedMatch
                {
                    ProviderId = match.ProviderId,
                    ProviderTrackId = match.ProviderTrackId,
                    JellyfinTrackId = match.JellyfinTrackId,
                    MatchLevel = Plugin.Instance?.Configuration?.ItemMatchLevel ?? ItemMatchLevel.Default,
                    MatchCriteria = Plugin.Instance?.Configuration?.ItemMatchCriteria ?? ItemMatchCriteria.TrackName,
                    IsManualMatch = true,
                    VerifiedAt = DateTime.UtcNow,
                    Notes = "Manually accepted through Track Matching interface"
                };

                _verifiedMatchStore.Add(verifiedMatch);

                // Also save to database cache
                var currentConfig = Plugin.Instance?.Configuration;
                if (currentConfig != null)
                {
                    _dbRepository.InsertProviderTrackMatch(
                        trackDbId.Value,
                        jellyfinTrack.Id.ToString(),
                        ItemMatchLevel.Default, // Manual matches are considered default level
                        ItemMatchCriteria.TrackName | ItemMatchCriteria.AlbumName | ItemMatchCriteria.Artists | ItemMatchCriteria.AlbumArtists);
                }

                return new TrackMatchingAcceptResult
                {
                    ProviderTrackId = match.ProviderTrackId,
                    Success = true,
                    Error = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing match acceptance for track {TrackId}", match.ProviderTrackId);
                return new TrackMatchingAcceptResult
                {
                    ProviderTrackId = match.ProviderTrackId,
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Viperinius.Plugin.SpotifyImport.Matchers;

namespace Viperinius.Plugin.SpotifyImport
{
    /// <summary>
    /// Store for verified track matches.
    /// </summary>
    internal class VerifiedMatchStore
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true
        };

        private readonly ILogger<VerifiedMatchStore> _logger;
        private readonly List<VerifiedMatch> _verifiedMatches;
        private readonly string _filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="VerifiedMatchStore"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        public VerifiedMatchStore(ILogger<VerifiedMatchStore> logger)
        {
            _logger = logger;
            _verifiedMatches = new List<VerifiedMatch>();
            _filePath = Path.Combine(Plugin.Instance?.DataFolderPath ?? string.Empty, "verified_matches.json");
        }

        /// <summary>
        /// Gets the number of verified matches.
        /// </summary>
        public int Count => _verifiedMatches.Count;

        /// <summary>
        /// Adds a verified match.
        /// </summary>
        /// <param name="match">The verified match to add.</param>
        public void Add(VerifiedMatch match)
        {
            ArgumentNullException.ThrowIfNull(match);

            // Remove any existing match for the same provider track
            var existing = _verifiedMatches.FirstOrDefault(m =>
                m.ProviderId == match.ProviderId &&
                m.ProviderTrackId == match.ProviderTrackId);

            if (existing != null)
            {
                _verifiedMatches.Remove(existing);
            }

            _verifiedMatches.Add(match);
        }

        /// <summary>
        /// Removes a verified match.
        /// </summary>
        /// <param name="match">The verified match to remove.</param>
        /// <returns>True if the match was removed; otherwise, false.</returns>
        public bool Remove(VerifiedMatch match)
        {
            if (match == null)
            {
                return false;
            }

            return _verifiedMatches.Remove(match);
        }

        /// <summary>
        /// Removes a verified match by provider track ID.
        /// </summary>
        /// <param name="providerId">The provider ID.</param>
        /// <param name="providerTrackId">The provider track ID.</param>
        /// <returns>True if the match was removed; otherwise, false.</returns>
        public bool Remove(string providerId, string providerTrackId)
        {
            var match = _verifiedMatches.FirstOrDefault(m =>
                m.ProviderId == providerId &&
                m.ProviderTrackId == providerTrackId);

            if (match != null)
            {
                return _verifiedMatches.Remove(match);
            }

            return false;
        }

        /// <summary>
        /// Gets a verified match by provider track ID.
        /// </summary>
        /// <param name="providerId">The provider ID.</param>
        /// <param name="providerTrackId">The provider track ID.</param>
        /// <returns>The verified match if found; otherwise, null.</returns>
        public VerifiedMatch? GetByProviderTrackId(string providerId, string providerTrackId)
        {
            return _verifiedMatches.FirstOrDefault(m =>
                m.ProviderId == providerId &&
                m.ProviderTrackId == providerTrackId);
        }

        /// <summary>
        /// Gets a verified match by Jellyfin track ID.
        /// </summary>
        /// <param name="jellyfinTrackId">The Jellyfin track ID.</param>
        /// <returns>The verified match if found; otherwise, null.</returns>
        public VerifiedMatch? GetByJellyfinTrackId(Guid jellyfinTrackId)
        {
            return _verifiedMatches.FirstOrDefault(m => m.JellyfinTrackId == jellyfinTrackId);
        }

        /// <summary>
        /// Gets all verified matches.
        /// </summary>
        /// <returns>A list of all verified matches.</returns>
        public List<VerifiedMatch> GetAll()
        {
            return new List<VerifiedMatch>(_verifiedMatches);
        }

        /// <summary>
        /// Gets all verified matches for a specific provider.
        /// </summary>
        /// <param name="providerId">The provider ID.</param>
        /// <returns>A list of verified matches for the provider.</returns>
        public List<VerifiedMatch> GetByProvider(string providerId)
        {
            return _verifiedMatches.Where(m => m.ProviderId == providerId).ToList();
        }

        /// <summary>
        /// Clears all verified matches.
        /// </summary>
        public void Clear()
        {
            _verifiedMatches.Clear();
        }

        /// <summary>
        /// Loads verified matches from the file system.
        /// </summary>
        /// <returns>True if the matches were loaded successfully; otherwise, false.</returns>
        public bool Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    _logger.LogInformation("Verified matches file does not exist, starting with empty store");
                    return true;
                }

                var json = File.ReadAllText(_filePath);
                var matches = JsonSerializer.Deserialize<List<VerifiedMatch>>(json);

                if (matches != null)
                {
                    _verifiedMatches.Clear();
                    _verifiedMatches.AddRange(matches);
                    _logger.LogInformation("Loaded {Count} verified matches", _verifiedMatches.Count);
                    return true;
                }

                _logger.LogWarning("Failed to deserialize verified matches file");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading verified matches");
                return false;
            }
        }

        /// <summary>
        /// Saves verified matches to the file system.
        /// </summary>
        /// <returns>True if the matches were saved successfully; otherwise, false.</returns>
        public bool Save()
        {
            try
            {
                var dataFolderPath = Plugin.Instance?.DataFolderPath;
                if (string.IsNullOrEmpty(dataFolderPath))
                {
                    _logger.LogError("Data folder path is not available");
                    return false;
                }

                if (!Directory.Exists(dataFolderPath))
                {
                    Directory.CreateDirectory(dataFolderPath);
                }

                var json = JsonSerializer.Serialize(_verifiedMatches, SerializerOptions);

                File.WriteAllText(_filePath, json);
                _logger.LogInformation("Saved {Count} verified matches", _verifiedMatches.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving verified matches");
                return false;
            }
        }
    }
}

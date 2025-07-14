let currentSpotifyTrack = null;
let currentMatchedTrack = null;
let selectedMatches = new Set();
let selectedMatchedTracks = new Set();
let currentPage = 1;
let currentMatchedPage = 1;
let totalPages = 1;
let totalMatchedPages = 1;
const pageSize = 20;

const SpotifyImportConfig = {
    pluginUniqueId: 'F03D0ADB-289F-4986-BD6F-2468025249B3',
    pluginApiBaseUrl: 'Viperinius.Plugin.SpotifyImport'
};

let apiQueryOpts = {};

function initializeTrackMatching() {
    // Initialize API options
    apiQueryOpts.UserId = Dashboard.getCurrentUserId();
    apiQueryOpts.api_key = ApiClient.accessToken();

    // Setup tab switching
    setupTabs();
    
    // Setup search functionality
    setupSearch();
    
    // Setup pagination
    setupPagination();
    
    // Setup bulk actions
    setupBulkActions();
    
    // Load initial data
    loadUnmatchedTracks();
}

function setupTabs() {
    const tabButtons = document.querySelectorAll('.tab-button');
    const tabContents = document.querySelectorAll('.tab-content');
    
    tabButtons.forEach(button => {
        button.addEventListener('click', function() {
            const tabId = this.dataset.tab;
            
            // Update active tab button
            tabButtons.forEach(btn => btn.classList.remove('active'));
            this.classList.add('active');
            
            // Update active tab content
            tabContents.forEach(content => content.classList.remove('active'));
            document.getElementById(tabId + '-tab').classList.add('active');
            
            // Load appropriate data
            if (tabId === 'one-to-many') {
                loadUnmatchedTracks();
            } else if (tabId === 'one-to-one') {
                loadMatchedTracks();
            }
        });
    });
}

function setupSearch() {
    const spotifySearchInput = document.getElementById('spotifySearchInput');
    const jellyfinSearchInput = document.getElementById('jellyfinSearchInput');
    const matchedSearchInput = document.getElementById('matchedSearchInput');
    
    let searchTimeout;
    
    spotifySearchInput.addEventListener('input', function() {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
            currentPage = 1;
            loadUnmatchedTracks();
        }, 300);
    });
    
    jellyfinSearchInput.addEventListener('input', function() {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
            if (currentSpotifyTrack) {
                loadPotentialMatches(currentSpotifyTrack.Id);
            }
        }, 300);
    });
    
    matchedSearchInput.addEventListener('input', function() {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
            currentMatchedPage = 1;
            loadMatchedTracks();
        }, 300);
    });
}

function setupPagination() {
    // Unmatched tracks pagination
    document.getElementById('prevSpotifyPage').addEventListener('click', function() {
        if (currentPage > 1) {
            currentPage--;
            loadUnmatchedTracks();
        }
    });
    
    document.getElementById('nextSpotifyPage').addEventListener('click', function() {
        if (currentPage < totalPages) {
            currentPage++;
            loadUnmatchedTracks();
        }
    });
    
    // Matched tracks pagination
    document.getElementById('prevMatchedPage').addEventListener('click', function() {
        if (currentMatchedPage > 1) {
            currentMatchedPage--;
            loadMatchedTracks();
        }
    });
    
    document.getElementById('nextMatchedPage').addEventListener('click', function() {
        if (currentMatchedPage < totalMatchedPages) {
            currentMatchedPage++;
            loadMatchedTracks();
        }
    });
}

function setupBulkActions() {
    document.getElementById('acceptSelectedMatches').addEventListener('click', acceptSelectedMatches);
    document.getElementById('rejectSelectedMatches').addEventListener('click', rejectSelectedMatches);
    document.getElementById('refreshUnmatched').addEventListener('click', () => loadUnmatchedTracks());
    document.getElementById('verifySelectedMatches').addEventListener('click', verifySelectedMatches);
    document.getElementById('removeSelectedMatches').addEventListener('click', removeSelectedMatches);
    document.getElementById('refreshMatched').addEventListener('click', () => loadMatchedTracks());
}

function loadUnmatchedTracks() {
    const searchQuery = document.getElementById('spotifySearchInput').value;
    const apiUrl = ApiClient.getUrl(SpotifyImportConfig.pluginApiBaseUrl + '/TrackMatching/Unmatched', {
        providerId: 'Spotify',
        page: currentPage,
        pageSize: pageSize,
        searchQuery: searchQuery || undefined,
        'api_key': apiQueryOpts.api_key
    });
    
    document.getElementById('spotifyTracksList').innerHTML = '<div class="loading">Loading Spotify tracks...</div>';
    
    ApiClient.fetch({
        url: apiUrl,
        type: 'GET',
        headers: {
            accept: 'application/json'
        }
    }, true).then(function (response) {
        if (response.ok) {
            return response.json();
        }
        throw new Error('Failed to load unmatched tracks');
    }).then(function (data) {
        displaySpotifyTracks(data.tracks || []);
        updatePagination(data.page, data.pageSize, data.totalCount, 'spotify');
    }).catch(function (error) {
        console.error('Error loading unmatched tracks:', error);
        document.getElementById('spotifyTracksList').innerHTML = '<div class="empty-state">Error loading tracks</div>';
    });
}

function loadMatchedTracks() {
    const searchQuery = document.getElementById('matchedSearchInput').value;
    const apiUrl = ApiClient.getUrl(SpotifyImportConfig.pluginApiBaseUrl + '/TrackMatching/Matched', {
        providerId: 'Spotify',
        page: currentMatchedPage,
        pageSize: pageSize,
        searchQuery: searchQuery || undefined,
        'api_key': apiQueryOpts.api_key
    });
    
    document.getElementById('matchedTracksList').innerHTML = '<div class="loading">Loading matched tracks...</div>';
    
    ApiClient.fetch({
        url: apiUrl,
        type: 'GET',
        headers: {
            accept: 'application/json'
        }
    }, true).then(function (response) {
        if (response.ok) {
            return response.json();
        }
        throw new Error('Failed to load matched tracks');
    }).then(function (data) {
        displayMatchedTracks(data.tracks || []);
        updatePagination(data.page, data.pageSize, data.totalCount, 'matched');
    }).catch(function (error) {
        console.error('Error loading matched tracks:', error);
        document.getElementById('matchedTracksList').innerHTML = '<div class="empty-state">Error loading tracks</div>';
    });
}

function loadPotentialMatches(providerTrackId) {
    const searchQuery = document.getElementById('jellyfinSearchInput').value;
    const apiUrl = ApiClient.getUrl(SpotifyImportConfig.pluginApiBaseUrl + '/TrackMatching/Search', {
        providerTrackId: providerTrackId,
        providerId: 'Spotify',
        searchQuery: searchQuery || undefined,
        'api_key': apiQueryOpts.api_key
    });
    
    document.getElementById('jellyfinMatchesList').innerHTML = '<div class="loading">Loading potential matches...</div>';
    
    ApiClient.fetch({
        url: apiUrl,
        type: 'GET',
        headers: {
            accept: 'application/json'
        }
    }, true).then(function (response) {
        if (response.ok) {
            return response.json();
        }
        throw new Error('Failed to load potential matches');
    }).then(function (data) {
        displayPotentialMatches(data);
    }).catch(function (error) {
        console.error('Error loading potential matches:', error);
        document.getElementById('jellyfinMatchesList').innerHTML = '<div class="empty-state">Error loading matches</div>';
    });
}

function displaySpotifyTracks(tracks) {
    const container = document.getElementById('spotifyTracksList');
    
    if (tracks.length === 0) {
        container.innerHTML = '<div class="empty-state">No unmatched tracks found</div>';
        return;
    }
    
    let html = '';
    tracks.forEach(trackItem => {
        const track = trackItem.providerTrack;
        const matchType = trackItem.matchType === 1 ? 'One-to-Many' : 'One-to-One';
        
        html += `
            <div class="track-item" data-track-id="${track.id}" onclick="selectSpotifyTrack('${track.id}')">
                <div class="track-name">${escapeHtml(track.name)}</div>
                <div class="track-details">
                    <div><strong>Album:</strong> ${escapeHtml(track.albumName)}</div>
                    <div><strong>Artists:</strong> ${escapeHtml(track.artistNames.join(', '))}</div>
                    <div><strong>Album Artists:</strong> ${escapeHtml(track.albumArtistNames.join(', '))}</div>
                    <div><strong>Match Type:</strong> ${matchType}</div>
                </div>
            </div>
        `;
    });
    
    container.innerHTML = html;
}

function displayMatchedTracks(tracks) {
    const container = document.getElementById('matchedTracksList');
    
    if (tracks.length === 0) {
        container.innerHTML = '<div class="empty-state">No matched tracks found</div>';
        return;
    }
    
    let html = '';
    tracks.forEach(trackItem => {
        const track = trackItem.providerTrack;
        const matchStatus = trackItem.isManualMatch ? 'Manual' : 'Auto';
        const statusClass = trackItem.isManualMatch ? 'manual-match' : 'verified-match';
        
        html += `
            <div class="track-item ${statusClass}" data-track-id="${track.id}" onclick="selectMatchedTrack('${track.id}')">
                <div class="track-name">
                    <span class="status-icon status-${trackItem.isManualMatch ? 'manual' : 'auto'}"></span>
                    ${escapeHtml(track.name)}
                </div>
                <div class="track-details">
                    <div><strong>Album:</strong> ${escapeHtml(track.albumName)}</div>
                    <div><strong>Artists:</strong> ${escapeHtml(track.artistNames.join(', '))}</div>
                    <div><strong>Match Status:</strong> ${matchStatus}</div>
                </div>
            </div>
        `;
    });
    
    container.innerHTML = html;
}

function displayPotentialMatches(matches) {
    const container = document.getElementById('jellyfinMatchesList');
    
    if (matches.length === 0) {
        container.innerHTML = '<div class="empty-state">No potential matches found</div>';
        return;
    }
    
    let html = '';
    matches.forEach(match => {
        const confidence = calculateMatchConfidence(match.differences);
        const confidenceClass = confidence > 0.8 ? 'high' : confidence > 0.5 ? 'medium' : 'low';
        const matchClass = confidence > 0.8 ? 'perfect-match' : confidence > 0.5 ? 'partial-match' : 'poor-match';
        
        html += `
            <div class="match-item ${matchClass}" data-match-id="${match.id}">
                <div class="match-confidence confidence-${confidenceClass}">
                    ${Math.round(confidence * 100)}%
                </div>
                <div class="track-name">${escapeHtml(match.name)}</div>
                <div class="track-details">
                    <div><strong>Album:</strong> ${escapeHtml(match.albumName)}</div>
                    <div><strong>Artists:</strong> ${escapeHtml(match.artistNames.join(', '))}</div>
                    <div><strong>Album Artists:</strong> ${escapeHtml(match.albumArtistNames.join(', '))}</div>
                    <div><strong>Path:</strong> ${escapeHtml(match.path)}</div>
                </div>
                ${displayDifferences(match.differences)}
                <div class="match-actions">
                    <button class="emby-button-small" onclick="acceptMatch('${match.id}')">Accept</button>
                    <button class="emby-button-small" onclick="rejectMatch('${match.id}')">Reject</button>
                </div>
            </div>
        `;
    });
    
    container.innerHTML = html;
}

function displayDifferences(differences) {
    if (!differences || differences.length === 0) {
        return '';
    }
    
    let html = '<div class="differences"><strong>Differences:</strong>';
    differences.forEach(diff => {
        html += `
            <div class="difference">
                <span class="field">${diff.field}:</span>
                <span class="provider-value">${escapeHtml(diff.providerValue)}</span>
                <span>→</span>
                <span class="jellyfin-value">${escapeHtml(diff.jellyfinValue)}</span>
            </div>
        `;
    });
    html += '</div>';
    
    return html;
}

function selectSpotifyTrack(trackId) {
    // Update UI selection
    document.querySelectorAll('#spotifyTracksList .track-item').forEach(item => {
        item.classList.remove('selected');
    });
    document.querySelector(`#spotifyTracksList .track-item[data-track-id="${trackId}"]`).classList.add('selected');
    
    // Store current selection
    currentSpotifyTrack = { Id: trackId };
    
    // Load potential matches
    loadPotentialMatches(trackId);
}

function selectMatchedTrack(trackId) {
    // Update UI selection
    document.querySelectorAll('#matchedTracksList .track-item').forEach(item => {
        item.classList.remove('selected');
    });
    document.querySelector(`#matchedTracksList .track-item[data-track-id="${trackId}"]`).classList.add('selected');
    
    // Store current selection
    currentMatchedTrack = { Id: trackId };
    
    // Load match verification details
    loadMatchVerificationDetails(trackId);
}

function loadMatchVerificationDetails(trackId) {
    document.getElementById('matchVerificationPanel').innerHTML = '<div class="loading">Loading match details...</div>';
    
    // TODO: Implement match verification details display
    // For now, show placeholder
    document.getElementById('matchVerificationPanel').innerHTML = `
        <div class="match-verification">
            <h3>Match Verification</h3>
            <p>Verification details for track ${trackId} will be displayed here.</p>
            <div class="verification-actions">
                <button class="emby-button" onclick="verifyMatch('${trackId}')">Verify Match</button>
                <button class="emby-button" onclick="removeMatch('${trackId}')">Remove Match</button>
            </div>
        </div>
    `;
}

function acceptMatch(jellyfinTrackId) {
    if (!currentSpotifyTrack) {
        Dashboard.alert('Please select a Spotify track first');
        return;
    }
    
    const matches = [{
        providerId: 'Spotify',
        providerTrackId: currentSpotifyTrack.Id,
        jellyfinTrackId: jellyfinTrackId
    }];
    
    acceptMatches(matches);
}

function acceptMatches(matches) {
    const apiUrl = ApiClient.getUrl(SpotifyImportConfig.pluginApiBaseUrl + '/TrackMatching/AcceptMatches');
    
    Dashboard.showLoadingMsg();
    
    ApiClient.fetch({
        url: apiUrl,
        type: 'POST',
        data: JSON.stringify(matches),
        headers: {
            'Content-Type': 'application/json',
            accept: 'application/json'
        }
    }, true).then(function (response) {
        if (response.ok) {
            return response.json();
        }
        throw new Error('Failed to accept matches');
    }).then(function (data) {
        Dashboard.hideLoadingMsg();
        
        let successCount = 0;
        let failureCount = 0;
        
        data.results.forEach(result => {
            if (result.success) {
                successCount++;
            } else {
                failureCount++;
            }
        });
        
        if (successCount > 0) {
            Dashboard.alert(`Successfully accepted ${successCount} match(es)`);
            loadUnmatchedTracks(); // Refresh the list
        }
        
        if (failureCount > 0) {
            Dashboard.alert(`Failed to accept ${failureCount} match(es)`);
        }
    }).catch(function (error) {
        Dashboard.hideLoadingMsg();
        console.error('Error accepting matches:', error);
        Dashboard.alert('Error accepting matches');
    });
}

function rejectMatch(jellyfinTrackId) {
    // TODO: Implement reject functionality
    Dashboard.alert('Reject functionality not yet implemented');
}

function acceptSelectedMatches() {
    if (selectedMatches.size === 0) {
        Dashboard.alert('Please select matches to accept');
        return;
    }
    
    const matches = Array.from(selectedMatches).map(matchId => ({
        providerId: 'Spotify',
        providerTrackId: currentSpotifyTrack?.Id || '',
        jellyfinTrackId: matchId
    }));
    
    acceptMatches(matches);
}

function rejectSelectedMatches() {
    // TODO: Implement bulk reject functionality
    Dashboard.alert('Bulk reject functionality not yet implemented');
}

function verifySelectedMatches() {
    // TODO: Implement bulk verify functionality
    Dashboard.alert('Bulk verify functionality not yet implemented');
}

function removeSelectedMatches() {
    // TODO: Implement bulk remove functionality
    Dashboard.alert('Bulk remove functionality not yet implemented');
}

function verifyMatch(trackId) {
    // TODO: Implement individual verify functionality
    Dashboard.alert('Verify functionality not yet implemented');
}

function removeMatch(trackId) {
    // TODO: Implement individual remove functionality
    Dashboard.alert('Remove functionality not yet implemented');
}

function calculateMatchConfidence(differences) {
    if (!differences || differences.length === 0) {
        return 1.0; // Perfect match
    }
    
    // Simple confidence calculation based on number of differences
    const maxDifferences = 4; // TrackName, AlbumName, Artists, AlbumArtists
    const confidence = Math.max(0, (maxDifferences - differences.length) / maxDifferences);
    
    return confidence;
}

function updatePagination(page, pageSize, totalCount, type) {
    const totalPages = Math.ceil(totalCount / pageSize);
    
    if (type === 'spotify') {
        currentPage = page;
        this.totalPages = totalPages;
        
        document.getElementById('prevSpotifyPage').disabled = page <= 1;
        document.getElementById('nextSpotifyPage').disabled = page >= totalPages;
        document.getElementById('spotifyPageInfo').textContent = `Page ${page} of ${totalPages}`;
    } else if (type === 'matched') {
        currentMatchedPage = page;
        this.totalMatchedPages = totalPages;
        
        document.getElementById('prevMatchedPage').disabled = page <= 1;
        document.getElementById('nextMatchedPage').disabled = page >= totalPages;
        document.getElementById('matchedPageInfo').textContent = `Page ${page} of ${totalPages}`;
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

export default function (view) {
    view.dispatchEvent(new CustomEvent('create'));
    
    view.addEventListener('viewshow', function () {
        Dashboard.showLoadingMsg();
        initializeTrackMatching();
        Dashboard.hideLoadingMsg();
    });
}

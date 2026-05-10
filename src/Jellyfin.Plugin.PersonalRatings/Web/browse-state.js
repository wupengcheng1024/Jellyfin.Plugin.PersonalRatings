(function () {
    'use strict';

    if (window.PersonalRatingsBrowseState) {
        return;
    }

    /**
     * Owns mutable state and lightweight transitions for the front browse route.
     * This module does not touch DOM or perform network requests.
     */
    window.PersonalRatingsBrowseState = {
        create: function () {
            return {
                features: {
                    manageEnabled: true
                },
                isAdministrator: false,
                isLoading: false,
                isTagLoading: false,
                isFeatureLoading: false,
                isUserLoading: false,
                featuresLoaded: false,
                userContextLoaded: false,
                pageNumber: 1,
                pageSize: 36,
                needsReload: true,
                scoreFilter: 'rated',
                tagIds: [],
                tagMatchMode: 'any',
                playedFilter: 'all',
                mediaType: 'all',
                sortValue: 'ratedAt:desc',
                search: '',
                viewMode: 'poster',
                lastResult: null,
                lastLoadFailed: false,
                tags: [],
                requestVersion: 0,
                tagsLoaded: false
            };
        },

        buildQueryRequest: function (state) {
            var sortParts = String(state.sortValue || 'ratedAt:desc').split(':');
            var request = {
                keyword: state.search || null,
                tagIds: state.tagIds.slice(),
                tagMatchMode: state.tagMatchMode || 'any',
                sortBy: sortParts[0] || 'ratedAt',
                sortOrder: sortParts[1] || 'desc',
                pageNumber: state.pageNumber,
                pageSize: state.pageSize
            };

            if (state.scoreFilter === 'rated') {
                request.isRated = true;
            } else if (state.scoreFilter === 'all') {
                request.isRated = null;
            } else if (state.scoreFilter === 'unrated') {
                request.isRated = false;
            } else {
                request.isRated = true;
                request.score = parseInt(state.scoreFilter, 10);
            }

            if (state.playedFilter === 'played') {
                request.isPlayed = true;
            } else if (state.playedFilter === 'unplayed') {
                request.isPlayed = false;
            }

            if (state.mediaType !== 'all') {
                request.mediaTypes = [state.mediaType];
            }

            return request;
        },

        setFeatureState: function (state, manageEnabled) {
            state.features.manageEnabled = !!manageEnabled;
            state.featuresLoaded = true;
        },

        setUserAdministrator: function (state, isAdministrator) {
            state.isAdministrator = !!isAdministrator;
            state.userContextLoaded = true;
        },

        setTags: function (state, tags) {
            state.tags = Array.isArray(tags) ? tags : [];
            state.tagsLoaded = true;
        },

        setResult: function (state, result) {
            state.lastResult = result;
        },

        setScoreFilter: function (state, scoreFilter) {
            state.scoreFilter = scoreFilter || 'rated';
            state.pageNumber = 1;
        },

        setPlayedFilter: function (state, playedFilter) {
            state.playedFilter = playedFilter || 'all';
            state.pageNumber = 1;
        },

        setMediaType: function (state, mediaType) {
            state.mediaType = mediaType || 'all';
            state.pageNumber = 1;
        },

        setSortValue: function (state, sortValue) {
            state.sortValue = sortValue || 'ratedAt:desc';
            state.pageNumber = 1;
        },

        setTagMatchMode: function (state, tagMatchMode) {
            state.tagMatchMode = tagMatchMode || 'any';
            state.pageNumber = 1;
        },

        setSearch: function (state, search) {
            state.search = String(search || '').trim();
            state.pageNumber = 1;
        },

        clearSearch: function (state) {
            state.search = '';
            state.pageNumber = 1;
        },

        toggleTagFilter: function (state, tagId) {
            if (!tagId || Number.isNaN(tagId)) {
                return;
            }

            var index = state.tagIds.indexOf(tagId);
            if (index >= 0) {
                state.tagIds.splice(index, 1);
            } else {
                state.tagIds.push(tagId);
            }

            state.pageNumber = 1;
        },

        setViewMode: function (state, viewMode) {
            state.viewMode = viewMode === 'list' ? 'list' : 'poster';
            state.pageSize = state.viewMode === 'list' ? 24 : 36;
            state.pageNumber = 1;
        },

        changePage: function (state, delta) {
            var nextPage = state.pageNumber + delta;
            if (nextPage < 1) {
                return false;
            }

            state.pageNumber = nextPage;
            return true;
        }
    };
})();

(function () {
    'use strict';

    if (window.PersonalRatingsBrowseState) {
        return;
    }

    var storageKey = 'personalRatingsBrowseStateV2';

    function buildDefaultState() {
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
            scrollTop: 0,
            activePanelMode: null,
            lastResult: null,
            lastLoadFailed: false,
            tags: [],
            requestVersion: 0,
            tagsLoaded: false
        };
    }

    function hydrateStoredState() {
        var defaults = buildDefaultState();
        var raw = null;
        var parsed = null;
        try {
            raw = window.sessionStorage ? window.sessionStorage.getItem(storageKey) : null;
            parsed = raw ? JSON.parse(raw) : null;
        } catch (error) {
            parsed = null;
        }

        if (!parsed || typeof parsed !== 'object') {
            return defaults;
        }

        if (Array.isArray(parsed.tagIds)) {
            defaults.tagIds = parsed.tagIds.filter(function (value) {
                return typeof value === 'number';
            });
        }

        if (typeof parsed.pageNumber === 'number' && parsed.pageNumber > 0) {
            defaults.pageNumber = parsed.pageNumber;
        }

        if (typeof parsed.scoreFilter === 'string') {
            defaults.scoreFilter = parsed.scoreFilter;
        }

        if (typeof parsed.tagMatchMode === 'string') {
            defaults.tagMatchMode = parsed.tagMatchMode === 'all' ? 'all' : 'any';
        }

        if (typeof parsed.playedFilter === 'string') {
            defaults.playedFilter = parsed.playedFilter;
        }

        if (typeof parsed.mediaType === 'string') {
            defaults.mediaType = parsed.mediaType;
        }

        if (typeof parsed.sortValue === 'string') {
            defaults.sortValue = parsed.sortValue;
        }

        if (typeof parsed.search === 'string') {
            defaults.search = parsed.search;
        }

        if (typeof parsed.viewMode === 'string') {
            defaults.viewMode = parsed.viewMode === 'list' ? 'list' : 'poster';
        }

        if (typeof parsed.scrollTop === 'number' && parsed.scrollTop >= 0) {
            defaults.scrollTop = parsed.scrollTop;
        }

        return defaults;
    }

    function persistState(state) {
        if (!window.sessionStorage) {
            return;
        }

        try {
            window.sessionStorage.setItem(storageKey, JSON.stringify({
                pageNumber: state.pageNumber,
                scoreFilter: state.scoreFilter,
                tagIds: state.tagIds,
                tagMatchMode: state.tagMatchMode,
                playedFilter: state.playedFilter,
                mediaType: state.mediaType,
                sortValue: state.sortValue,
                search: state.search,
                viewMode: state.viewMode,
                scrollTop: state.scrollTop || 0
            }));
        } catch (error) {
            void error;
        }
    }

    /**
     * Owns mutable state and lightweight transitions for the front browse route.
     * This module does not touch DOM or perform network requests.
     */
    window.PersonalRatingsBrowseState = {
        create: function () {
            return hydrateStoredState();
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
            persistState(state);
        },

        setPlayedFilter: function (state, playedFilter) {
            state.playedFilter = playedFilter || 'all';
            state.pageNumber = 1;
            persistState(state);
        },

        setMediaType: function (state, mediaType) {
            state.mediaType = mediaType || 'all';
            state.pageNumber = 1;
            persistState(state);
        },

        setSortValue: function (state, sortValue) {
            state.sortValue = sortValue || 'ratedAt:desc';
            state.pageNumber = 1;
            persistState(state);
        },

        setTagMatchMode: function (state, tagMatchMode) {
            state.tagMatchMode = tagMatchMode || 'any';
            state.pageNumber = 1;
            persistState(state);
        },

        setSearch: function (state, search) {
            state.search = String(search || '').trim();
            state.pageNumber = 1;
            persistState(state);
        },

        clearSearch: function (state) {
            state.search = '';
            state.pageNumber = 1;
            persistState(state);
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
            persistState(state);
        },

        setViewMode: function (state, viewMode) {
            state.viewMode = viewMode === 'list' ? 'list' : 'poster';
            persistState(state);
        },

        changePage: function (state, delta) {
            var nextPage = state.pageNumber + delta;
            if (nextPage < 1) {
                return false;
            }

            state.pageNumber = nextPage;
            persistState(state);
            return true;
        },

        setActivePanelMode: function (state, mode) {
            state.activePanelMode = mode || null;
            persistState(state);
        },

        setScrollTop: function (state, scrollTop) {
            state.scrollTop = typeof scrollTop === 'number' && scrollTop >= 0 ? scrollTop : 0;
            persistState(state);
        },

        persist: function (state) {
            persistState(state);
        }
    };
})();

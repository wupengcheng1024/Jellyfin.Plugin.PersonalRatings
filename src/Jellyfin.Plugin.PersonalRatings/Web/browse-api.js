(function () {
    'use strict';

    if (window.PersonalRatingsBrowseApi) {
        return;
    }

    function ensureSharedFeatureStateCache() {
        if (window.PersonalRatingsFeatureStateCache) {
            return window.PersonalRatingsFeatureStateCache;
        }

        window.PersonalRatingsFeatureStateCache = {
            value: null,
            promise: null,
            get: function (forceReload) {
                var self = this;
                if (forceReload) {
                    self.value = null;
                    self.promise = null;
                }

                if (self.value) {
                    return Promise.resolve(self.value);
                }

                if (self.promise) {
                    return self.promise;
                }

                self.promise = window.fetch('/Plugins/PersonalRatings/features', {
                    credentials: 'same-origin'
                }).then(function (response) {
                    if (!response.ok) {
                        throw new Error('Failed to load plugin feature state.');
                    }

                    return response.json();
                }).then(function (result) {
                    self.value = result || {};
                    return self.value;
                }).finally(function () {
                    self.promise = null;
                });

                return self.promise;
            }
        };

        return window.PersonalRatingsFeatureStateCache;
    }

    var sharedFeatureStateCache = ensureSharedFeatureStateCache();

    /**
     * Wraps all front browse page API access so route orchestration can stay
     * focused on state transitions and DOM lifecycle.
     */
    window.PersonalRatingsBrowseApi = {
        getFeatureState: function (forceReload) {
            return sharedFeatureStateCache.get(!!forceReload);
        },

        getCurrentUser: function () {
            return this.getApiClient().getCurrentUser();
        },

        getTags: function () {
            return this.getApiClient().ajax({
                type: 'GET',
                url: this.getApiClient().getUrl('Plugins/PersonalRatings/tags'),
                dataType: 'json'
            });
        },

        queryRatings: function (payload) {
            return this.getApiClient().ajax({
                type: 'POST',
                url: this.getApiClient().getUrl('Plugins/PersonalRatings/ratings/query'),
                contentType: 'application/json',
                dataType: 'json',
                data: JSON.stringify(payload)
            });
        },

        buildImageUrl: function (itemId) {
            return this.getApiClient().getUrl('Items/' + itemId + '/Images/Primary', {
                fillHeight: 520,
                fillWidth: 348,
                quality: 90
            });
        },

        getServerId: function () {
            return this.getApiClient().serverId();
        },

        navigateTo: function (targetRoute) {
            if (window.Dashboard && typeof window.Dashboard.navigate === 'function') {
                window.Dashboard.navigate(targetRoute);
                return;
            }

            window.location.hash = '#/' + targetRoute;
        },

        getApiClient: function () {
            return window.ApiClient;
        }
    };
})();

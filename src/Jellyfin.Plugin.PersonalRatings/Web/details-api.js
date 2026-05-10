(function () {
    'use strict';

    if (window.PersonalRatingsDetailApi) {
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

    var availableTagsCache = null;
    var availableTagsPromise = null;
    var sharedFeatureStateCache = ensureSharedFeatureStateCache();

    /**
     * Encapsulates detail-panel API access and shared request helpers.
     * Panel rendering should not know how requests are transported.
     */
    window.PersonalRatingsDetailApi = {
        getFeatureState: function (forceReload) {
            return sharedFeatureStateCache.get(!!forceReload);
        },

        getCurrentUser: function () {
            return this.getApiClient().getCurrentUser();
        },

        getRating: function (itemId) {
            return this.getApiClient().ajax({
                type: 'GET',
                url: this.getApiClient().getUrl('Plugins/PersonalRatings/rating', {
                    itemId: itemId
                }),
                dataType: 'json'
            });
        },

        getItemTags: function (itemId) {
            return this.getApiClient().ajax({
                type: 'GET',
                url: this.getApiClient().getUrl('Plugins/PersonalRatings/item-tags', {
                    itemId: itemId
                }),
                dataType: 'json'
            });
        },

        getAvailableTags: function (forceReload) {
            if (forceReload) {
                availableTagsCache = null;
                availableTagsPromise = null;
            }

            if (Array.isArray(availableTagsCache)) {
                return Promise.resolve(availableTagsCache);
            }

            if (availableTagsPromise) {
                return availableTagsPromise;
            }

            availableTagsPromise = this.apiGetJson('Plugins/PersonalRatings/tags').then(function (result) {
                availableTagsCache = Array.isArray(result) ? result : [];
                return availableTagsCache;
            }).catch(function () {
                availableTagsCache = [];
                return availableTagsCache;
            }).finally(function () {
                availableTagsPromise = null;
            });

            return availableTagsPromise;
        },

        createTag: function (name, color) {
            var payload = {
                name: name,
                color: color || null,
                sortOrder: 0,
                isEnabled: true
            };

            return this.postJson('Plugins/PersonalRatings/tags', payload).then(function (result) {
                availableTagsCache = null;
                availableTagsPromise = null;
                return result;
            });
        },

        setRating: function (itemId, score) {
            return this.postJson('Plugins/PersonalRatings/rating', {
                itemId: itemId,
                score: score
            });
        },

        clearRating: function (itemId) {
            return this.getApiClient().ajax({
                type: 'DELETE',
                url: this.getApiClient().getUrl('Plugins/PersonalRatings/rating', {
                    itemId: itemId
                }),
                dataType: 'json'
            });
        },

        setPendingDelete: function (itemId, isPendingDelete) {
            return this.postJson(
                isPendingDelete
                    ? 'Plugins/PersonalRatings/ratings/batch/unset-pending-delete'
                    : 'Plugins/PersonalRatings/ratings/batch/set-pending-delete',
                {
                    itemIds: [itemId]
                });
        },

        replaceItemTags: function (itemId, tagIds) {
            return this.putJson('Plugins/PersonalRatings/item-tags', {
                itemId: itemId,
                tagIds: tagIds
            });
        },

        deletePhysical: function (itemId) {
            return this.postJson('Plugins/PersonalRatings/ratings/batch/delete-physical', {
                itemIds: [itemId],
                confirmDelete: true
            });
        },

        postJson: function (path, payload) {
            return this.getApiClient().ajax({
                type: 'POST',
                url: this.getApiClient().getUrl(path),
                contentType: 'application/json',
                dataType: 'json',
                data: JSON.stringify(payload)
            });
        },

        putJson: function (path, payload) {
            return this.getApiClient().ajax({
                type: 'PUT',
                url: this.getApiClient().getUrl(path),
                contentType: 'application/json',
                dataType: 'json',
                data: JSON.stringify(payload)
            });
        },

        apiGetJson: function (path) {
            return this.getApiClient().ajax({
                type: 'GET',
                url: this.getApiClient().getUrl(path),
                dataType: 'json'
            });
        },

        getApiClient: function () {
            return window.ApiClient;
        },

        invalidateAvailableTagsCache: function () {
            availableTagsCache = null;
            availableTagsPromise = null;
        }
    };
})();

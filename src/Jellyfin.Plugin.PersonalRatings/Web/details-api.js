(function () {
    'use strict';

    if (window.PersonalRatingsDetailApi) {
        return;
    }

    var availableTagsCache = null;
    var availableTagsPromise = null;

    /**
     * Encapsulates detail-panel API access and shared request helpers.
     * Panel rendering should not know how requests are transported.
     */
    window.PersonalRatingsDetailApi = {
        getFeatureState: function () {
            return window.fetch('/Plugins/PersonalRatings/features', {
                credentials: 'same-origin'
            }).then(function (response) {
                if (!response.ok) {
                    throw new Error('Failed to load plugin feature state.');
                }

                return response.json();
            });
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

        getAvailableTags: function () {
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
        }
    };
})();

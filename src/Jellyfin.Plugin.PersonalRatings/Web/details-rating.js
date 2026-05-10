(function () {
    'use strict';

    if (window.PersonalRatingsDetailInjection) {
        return;
    }

    if (!window.PersonalRatingsDetailApi || !window.PersonalRatingsDetailPanel) {
        return;
    }

    /**
     * Boots the detail-page unified operation area and coordinates feature state,
     * user context and action handlers across the dedicated API / panel modules.
     */
    window.PersonalRatingsDetailInjection = true;

    var currentRequestVersion = 0;
    var deleteFeatureEnabled = true;
    var isAdministrator = false;
    var isFeatureStateLoading = false;
    var isFeatureStateLoaded = false;
    var isUserContextLoading = false;
    var isUserContextLoaded = false;
    var managePageEnabled = true;
    var route = 'personalratings';
    var syncTimerIds = [];

    window.PersonalRatingsDetailPanel.injectStyles();
    bindShell();
    scheduleSyncBurst();

    function bindShell() {
        window.addEventListener('hashchange', scheduleSyncBurst);
        window.addEventListener('popstate', scheduleSyncBurst);
        window.addEventListener('pageshow', scheduleSyncBurst);
        document.addEventListener('visibilitychange', function () {
            if (!document.hidden) {
                scheduleSyncBurst();
            }
        });
    }

    function scheduleSyncBurst() {
        clearSyncTimers();
        [0, 80, 220, 480].forEach(function (delay) {
            var timerId = window.setTimeout(function () {
                sync();
            }, delay);
            syncTimerIds.push(timerId);
        });
    }

    function clearSyncTimers() {
        syncTimerIds.forEach(function (timerId) {
            window.clearTimeout(timerId);
        });
        syncTimerIds = [];
    }

    function sync() {
        if (!window.ApiClient || typeof window.ApiClient.isLoggedIn !== 'function' || !window.ApiClient.isLoggedIn()) {
            deleteFeatureEnabled = true;
            isAdministrator = false;
            isFeatureStateLoading = false;
            isFeatureStateLoaded = false;
            isUserContextLoading = false;
            isUserContextLoaded = false;
            managePageEnabled = true;
            window.PersonalRatingsDetailPanel.removeDetailPanel();
            window.PersonalRatingsDetailPanel.hideLauncher();
            return;
        }

        var itemId = getCurrentItemId();
        var detailsPage = itemId ? document.querySelector('.itemDetailPage:not(.hide)') : null;

        if (!detailsPage || !itemId) {
            currentRequestVersion += 1;
            window.PersonalRatingsDetailPanel.removeDetailPanel();
            window.PersonalRatingsDetailPanel.hideLauncher();
            return;
        }

        if (!isFeatureStateLoaded) {
            ensureFeatureState();
        }

        if (!isUserContextLoaded) {
            ensureUserContext();
        }

        window.PersonalRatingsDetailPanel.hideLauncher();

        var panel = window.PersonalRatingsDetailPanel.ensureDetailPanel(detailsPage, itemId, {
            onApplyScore: applyScore,
            onClearScore: clearScore,
            onTogglePendingDelete: togglePendingDelete,
            onToggleTag: toggleTag,
            onCreateTag: createTag,
            onDeletePhysical: deletePhysical,
            onOpenManagePage: openManagePage
        });

        if (!panel) {
            return;
        }

        window.PersonalRatingsDetailPanel.renderAdminControls(panel, isAdministrator, deleteFeatureEnabled, managePageEnabled);

        if (panel.dataset.itemId === itemId && panel._personalRatingsLoadedForItemId !== itemId) {
            panel._personalRatingsLoadedForItemId = itemId;
            loadPanelState(itemId);
        }
    }

    function ensureFeatureState() {
        if (isFeatureStateLoading) {
            return;
        }

        isFeatureStateLoading = true;
        window.PersonalRatingsDetailApi.getFeatureState().then(function (result) {
            deleteFeatureEnabled = !!(result && result.IsDeleteFeatureEnabled);
            managePageEnabled = !!(result && result.IsManagePageEnabled);
            isFeatureStateLoaded = true;
        }).catch(function () {
            deleteFeatureEnabled = true;
            managePageEnabled = true;
            isFeatureStateLoaded = true;
        }).finally(function () {
            isFeatureStateLoading = false;
            sync();
        });
    }

    function ensureUserContext() {
        if (isUserContextLoading || !window.ApiClient || typeof window.ApiClient.getCurrentUser !== 'function') {
            return;
        }

        isUserContextLoading = true;
        window.PersonalRatingsDetailApi.getCurrentUser().then(function (user) {
            isAdministrator = !!(user && user.Policy && user.Policy.IsAdministrator);
            isUserContextLoaded = true;
        }).catch(function () {
            isAdministrator = false;
            isUserContextLoaded = true;
        }).finally(function () {
            isUserContextLoading = false;
            sync();
        });
    }

    function getCurrentItemId() {
        var hash = window.location.hash || '';
        if (hash.indexOf('#/details') !== 0) {
            return null;
        }

        var parsedUrl = new URL(window.location.origin + '/' + hash.substring(2));
        return parsedUrl.searchParams.get('id');
    }

    function loadPanelState(itemId) {
        currentRequestVersion += 1;
        var requestVersion = currentRequestVersion;
        Promise.all([
            window.PersonalRatingsDetailApi.getRating(itemId),
            window.PersonalRatingsDetailApi.getAvailableTags(),
            window.PersonalRatingsDetailApi.getItemTags(itemId)
        ]).then(function (values) {
            if (requestVersion !== currentRequestVersion) {
                return;
            }

            var panel = window.PersonalRatingsDetailPanel.getActivePanel(itemId);
            if (!panel) {
                return;
            }

            var rating = values[0];
            var availableTags = values[1];
            var itemTagsResponse = values[2];
            var selectedTags = itemTagsResponse && itemTagsResponse.Tags ? itemTagsResponse.Tags : [];
            window.PersonalRatingsDetailPanel.updatePanelState(panel, rating, selectedTags, availableTags);
            window.PersonalRatingsDetailPanel.renderAdminControls(panel, isAdministrator, deleteFeatureEnabled, managePageEnabled);
        }).catch(function (error) {
            var panel = window.PersonalRatingsDetailPanel.getActivePanel(itemId);
            if (!panel) {
                return;
            }

            window.PersonalRatingsDetailPanel.syncScoreButtons(panel, 0);
            window.PersonalRatingsDetailPanel.renderTagPickerError(panel);
            window.PersonalRatingsDetailPanel.setTagCreateBusy(panel, false);
            window.PersonalRatingsDetailPanel.renderSummary(
                panel,
                null,
                error && error.status === 404 ? '当前条目不存在或无法访问。' : '读取评分失败。');
        });
    }

    function applyScore(itemId, score) {
        window.PersonalRatingsDetailPanel.updateActivePanelMessage(itemId, '正在保存 ' + score + ' 分...');
        window.PersonalRatingsDetailApi.setRating(itemId, score).then(function (result) {
            var panel = window.PersonalRatingsDetailPanel.getActivePanel(itemId);
            if (!panel) {
                return;
            }

            panel._personalRatingsRating = result;
            panel.dataset.isPendingDelete = result.IsPendingDelete ? 'true' : 'false';
            window.PersonalRatingsDetailPanel.syncScoreButtons(panel, result.Score);
            window.PersonalRatingsDetailPanel.renderSummary(
                panel,
                result,
                window.PersonalRatingsDetailPanel.buildSummary(
                    result,
                    window.PersonalRatingsDetailPanel.getSelectedTags(
                        panel,
                        [])));
            loadPanelState(itemId);
        }).catch(function () {
            window.PersonalRatingsDetailPanel.updateActivePanelMessage(itemId, '保存评分失败。');
        });
    }

    function clearScore(itemId) {
        window.PersonalRatingsDetailPanel.updateActivePanelMessage(itemId, '正在清除评分...');
        window.PersonalRatingsDetailApi.clearRating(itemId).then(function () {
            loadPanelState(itemId);
        }).catch(function () {
            window.PersonalRatingsDetailPanel.updateActivePanelMessage(itemId, '清除评分失败。');
        });
    }

    function togglePendingDelete(itemId, isPendingDelete) {
        window.PersonalRatingsDetailPanel.updateActivePanelMessage(itemId, isPendingDelete ? '正在取消待删除...' : '正在标记待删除...');
        window.PersonalRatingsDetailApi.setPendingDelete(itemId, isPendingDelete).then(function () {
            loadPanelState(itemId);
        }).catch(function () {
            window.PersonalRatingsDetailPanel.updateActivePanelMessage(itemId, '待删除状态更新失败。');
        });
    }

    function toggleTag(itemId, tagId) {
        if (!tagId || Number.isNaN(tagId)) {
            return;
        }

        var panel = window.PersonalRatingsDetailPanel.getActivePanel(itemId);
        if (!panel) {
            return;
        }

        var currentTagIds = window.PersonalRatingsDetailPanel.getSelectedTagIds(panel);
        var nextTagIds = currentTagIds.slice();
        var existingIndex = nextTagIds.indexOf(tagId);
        if (existingIndex >= 0) {
            nextTagIds.splice(existingIndex, 1);
        } else {
            nextTagIds.push(tagId);
        }

        window.PersonalRatingsDetailPanel.updateActivePanelMessage(itemId, '正在更新标签...');
        window.PersonalRatingsDetailApi.replaceItemTags(itemId, nextTagIds).then(function () {
            loadPanelState(itemId);
        }).catch(function () {
            window.PersonalRatingsDetailPanel.updateActivePanelMessage(itemId, '更新标签失败。');
        });
    }

    function createTag(itemId, rawName) {
        var panel = window.PersonalRatingsDetailPanel.getActivePanel(itemId);
        if (!panel) {
            return;
        }

        var name = String(rawName || '').trim();
        if (!name) {
            window.PersonalRatingsDetailPanel.renderTagCreateMessage(panel, '请输入标签名。', 'error');
            return;
        }

        window.PersonalRatingsDetailPanel.setTagCreateBusy(panel, true);
        window.PersonalRatingsDetailPanel.renderTagCreateMessage(panel, '正在新增标签...', 'loading');

        var createdTag = null;
        window.PersonalRatingsDetailApi.createTag(name, null).then(function (tag) {
            createdTag = tag;
            var currentTagIds = window.PersonalRatingsDetailPanel.getSelectedTagIds(panel);
            var nextTagIds = currentTagIds.slice();
            if (nextTagIds.indexOf(tag.Id) < 0) {
                nextTagIds.push(tag.Id);
            }

            window.PersonalRatingsDetailApi.invalidateAvailableTagsCache();
            return window.PersonalRatingsDetailApi.replaceItemTags(itemId, nextTagIds).then(function () {
                window.PersonalRatingsDetailPanel.clearTagCreateInput(panel);
                window.PersonalRatingsDetailPanel.renderTagCreateMessage(panel, '已新增并选中标签“' + name + '”。', 'success');
                loadPanelState(itemId);
            });
        }).catch(function (error) {
            window.PersonalRatingsDetailPanel.setTagCreateBusy(panel, false);

            if (createdTag) {
                window.PersonalRatingsDetailPanel.renderTagCreateMessage(panel, '标签已新增，但自动选中失败，请手动点选。', 'error');
                window.PersonalRatingsDetailApi.invalidateAvailableTagsCache();
                loadPanelState(itemId);
                return;
            }

            if (error && error.status === 403) {
                window.PersonalRatingsDetailPanel.renderTagCreateMessage(panel, '只有管理员可以新增标签。', 'error');
                return;
            }

            var responseText = '';
            if (error && error.responseText) {
                responseText = String(error.responseText);
            } else if (error && error.responseJSON) {
                responseText = JSON.stringify(error.responseJSON);
            } else if (error && error.message) {
                responseText = String(error.message);
            }

            if (responseText.indexOf('same name already exists') >= 0) {
                window.PersonalRatingsDetailPanel.renderTagCreateMessage(panel, '同名标签已存在，请直接点选已有标签。', 'error');
                return;
            }

            window.PersonalRatingsDetailPanel.renderTagCreateMessage(panel, '新增标签失败。', 'error');
        });
    }

    function deletePhysical(itemId) {
        if (!deleteFeatureEnabled) {
            window.PersonalRatingsDetailPanel.updateActivePanelMessage(itemId, '物理删除功能当前已被插件配置禁用。');
            return;
        }

        if (!isAdministrator) {
            window.PersonalRatingsDetailPanel.updateActivePanelMessage(itemId, '只有管理员可以执行物理删除。');
            return;
        }

        if (!window.confirm('物理删除会直接删除 Jellyfin 条目及其底层文件位置，且会写入审计日志。确定继续吗？')) {
            window.PersonalRatingsDetailPanel.updateActivePanelMessage(itemId, '已取消物理删除。');
            return;
        }

        window.PersonalRatingsDetailPanel.updateActivePanelMessage(itemId, '正在执行物理删除...');
        window.PersonalRatingsDetailApi.deletePhysical(itemId).then(function (result) {
            var deletedCount = result && typeof result.DeletedCount === 'number' ? result.DeletedCount : 0;
            var itemResult = result && result.Items && result.Items.length > 0 ? result.Items[0] : null;
            if (deletedCount > 0) {
                var successMessage = '条目已物理删除，正在跳转到打分库...';
                var redirectDelay = 350;
                if (itemResult && itemResult.SuggestedAction) {
                    successMessage = '条目已物理删除，但仍需处理：' + itemResult.SuggestedAction;
                    redirectDelay = 1400;
                }

                window.PersonalRatingsDetailPanel.updateActivePanelMessage(itemId, successMessage);
                window.setTimeout(openManagePage, redirectDelay);
                return;
            }

            if (itemResult && itemResult.Message) {
                var failureMessage = '物理删除失败：' + itemResult.Message;
                if (itemResult.SuggestedAction) {
                    failureMessage += ' ' + itemResult.SuggestedAction;
                }

                window.PersonalRatingsDetailPanel.updateActivePanelMessage(itemId, failureMessage);
                return;
            }

            window.PersonalRatingsDetailPanel.updateActivePanelMessage(itemId, '物理删除未成功。');
        }).catch(function (error) {
            if (error && error.status === 403) {
                window.PersonalRatingsDetailPanel.updateActivePanelMessage(itemId, '只有管理员可以执行物理删除。');
                return;
            }

            if (error && error.status === 409) {
                window.PersonalRatingsDetailPanel.updateActivePanelMessage(itemId, '物理删除功能当前已被插件配置禁用。');
                return;
            }

            window.PersonalRatingsDetailPanel.updateActivePanelMessage(itemId, '物理删除失败。');
        });
    }

    function openManagePage() {
        if (!managePageEnabled) {
            return;
        }

        if (window.PersonalRatingsBrowseApi && typeof window.PersonalRatingsBrowseApi.navigateTo === 'function') {
            window.PersonalRatingsBrowseApi.navigateTo(route);
            return;
        }

        window.location.hash = '#/' + route;
    }
})();

(function () {
    'use strict';

    if (window.PersonalRatingsDetailInjection) {
        return;
    }

    window.PersonalRatingsDetailInjection = true;

    var route = 'configurationpage?name=PersonalRatingsManagePage';
    var styleId = 'personalRatingsInjectedStyles';
    var launcherId = 'personalRatingsLauncher';
    var panelClassName = 'personalRatingsDetailPanel';
    var deleteFeatureEnabled = true;
    var isAdministrator = false;
    var isFeatureStateLoading = false;
    var isUserContextLoading = false;
    var managePageEnabled = true;
    var currentRequestVersion = 0;

    injectStyles();
    ensureLauncher();
    observeShell();
    sync();

    function observeShell() {
        var mutationObserver = new MutationObserver(function () {
            sync();
        });

        mutationObserver.observe(document.body, {
            childList: true,
            subtree: true
        });

        window.addEventListener('hashchange', sync);
        window.addEventListener('popstate', sync);
    }

    function sync() {
        if (!window.ApiClient || typeof window.ApiClient.isLoggedIn !== 'function' || !window.ApiClient.isLoggedIn()) {
            deleteFeatureEnabled = true;
            isAdministrator = false;
            isFeatureStateLoading = false;
            isUserContextLoading = false;
            managePageEnabled = true;
            removeDetailPanel();
            hideLauncher();
            return;
        }

        ensureFeatureState();
        ensureUserContext();
        ensureLauncher();

        var detailsPage = document.querySelector('.itemDetailPage:not(.hide)');
        var itemId = getCurrentItemId();

        if (!detailsPage || !itemId) {
            removeDetailPanel();
            updateLauncherVisibility();
            return;
        }

        updateLauncherVisibility();
        ensureDetailPanel(detailsPage, itemId);
    }

    function getCurrentItemId() {
        var hash = window.location.hash || '';
        if (hash.indexOf('#/details') !== 0) {
            return null;
        }

        var parsedUrl = new URL(window.location.origin + '/' + hash.substring(2));
        return parsedUrl.searchParams.get('id');
    }

    function ensureDetailPanel(detailsPage, itemId) {
        var panel = detailsPage.querySelector('.' + panelClassName);
        var buttonRow = detailsPage.querySelector('.mainDetailButtons');
        if (!buttonRow) {
            return;
        }

        if (!panel) {
            panel = document.createElement('section');
            panel.className = panelClassName;
            panel.innerHTML = [
                '<div class="personalRatingsPanelHeader">',
                '  <div>',
                '    <div class="personalRatingsPanelEyebrow">Personal Ratings</div>',
                '    <h3>个人评分</h3>',
                '  </div>',
                '  <button type="button" class="button-flat personalRatingsManageButton">我的评分库</button>',
                '</div>',
                '<div class="personalRatingsScoreRow">',
                '  <button type="button" class="button-flat personalRatingsScoreButton" data-score="1">1分</button>',
                '  <button type="button" class="button-flat personalRatingsScoreButton" data-score="2">2分</button>',
                '  <button type="button" class="button-flat personalRatingsScoreButton" data-score="3">3分</button>',
                '  <button type="button" class="button-flat personalRatingsScoreButton" data-score="4">4分</button>',
                '  <button type="button" class="button-flat personalRatingsScoreButton" data-score="5">5分</button>',
                '</div>',
                '<div class="personalRatingsMetaRow">',
                '  <button type="button" class="button-flat personalRatingsClearButton">清除评分</button>',
                '  <button type="button" class="button-flat personalRatingsPendingButton">标记待删除</button>',
                '  <button type="button" class="button-flat personalRatingsDeleteButton" hidden="hidden">物理删除</button>',
                '</div>',
                '<div class="personalRatingsDetailSummary">正在读取当前评分...</div>'
            ].join('');

            panel.addEventListener('click', function (event) {
                var target = event.target;
                if (!target) {
                    return;
                }

                var scoreButton = target.closest('.personalRatingsScoreButton');
                if (scoreButton) {
                    applyScore(panel.dataset.itemId, parseInt(scoreButton.getAttribute('data-score'), 10));
                    return;
                }

                if (target.closest('.personalRatingsClearButton')) {
                    clearScore(panel.dataset.itemId);
                    return;
                }

                if (target.closest('.personalRatingsPendingButton')) {
                    togglePendingDelete(panel.dataset.itemId, panel.dataset.isPendingDelete === 'true');
                    return;
                }

                if (target.closest('.personalRatingsDeleteButton')) {
                    deletePhysical(panel.dataset.itemId);
                    return;
                }

                if (target.closest('.personalRatingsManageButton')) {
                    openManagePage();
                }
            });

            buttonRow.insertAdjacentElement('afterend', panel);
        }

        renderAdminControls(panel);

        if (panel.dataset.itemId !== itemId) {
            panel.dataset.itemId = itemId;
            panel.dataset.isPendingDelete = 'false';
            renderSummary(panel, null, '正在读取当前评分...');
            loadRating(itemId);
        }
    }

    function loadRating(itemId) {
        currentRequestVersion += 1;
        var requestVersion = currentRequestVersion;
        var apiClient = window.ApiClient;
        var url = apiClient.getUrl('Plugins/PersonalRatings/rating', {
            itemId: itemId
        });

        apiClient.ajax({
            type: 'GET',
            url: url,
            dataType: 'json'
        }).then(function (result) {
            if (requestVersion !== currentRequestVersion) {
                return;
            }

            var panel = getActivePanel(itemId);
            if (!panel) {
                return;
            }

            panel.dataset.isPendingDelete = result.IsPendingDelete ? 'true' : 'false';
            renderSummary(panel, result, buildSummary(result));
            syncScoreButtons(panel, result.Score);
        }).catch(function (error) {
            var panel = getActivePanel(itemId);
            if (!panel) {
                return;
            }

            syncScoreButtons(panel, 0);
            renderSummary(panel, null, error && error.status === 404 ? '当前条目不存在或无法访问。' : '读取评分失败。');
        });
    }

    function applyScore(itemId, score) {
        updateActivePanelMessage(itemId, '正在保存 ' + score + ' 分...');
        postJson('Plugins/PersonalRatings/rating', {
            itemId: itemId,
            score: score
        }).then(function (result) {
            var panel = getActivePanel(itemId);
            if (!panel) {
                return;
            }

            panel.dataset.isPendingDelete = result.IsPendingDelete ? 'true' : 'false';
            syncScoreButtons(panel, result.Score);
            renderSummary(panel, result, buildSummary(result));
        }).catch(function () {
            updateActivePanelMessage(itemId, '保存评分失败。');
        });
    }

    function clearScore(itemId) {
        updateActivePanelMessage(itemId, '正在清除评分...');

        var apiClient = window.ApiClient;
        apiClient.ajax({
            type: 'DELETE',
            url: apiClient.getUrl('Plugins/PersonalRatings/rating', {
                itemId: itemId
            }),
            dataType: 'json'
        }).then(function (result) {
            var panel = getActivePanel(itemId);
            if (!panel) {
                return;
            }

            panel.dataset.isPendingDelete = result.IsPendingDelete ? 'true' : 'false';
            syncScoreButtons(panel, result.Score);
            renderSummary(panel, result, buildSummary(result));
        }).catch(function () {
            updateActivePanelMessage(itemId, '清除评分失败。');
        });
    }

    function togglePendingDelete(itemId, isPendingDelete) {
        updateActivePanelMessage(itemId, isPendingDelete ? '正在取消待删除...' : '正在标记待删除...');

        postJson(
            isPendingDelete
                ? 'Plugins/PersonalRatings/ratings/batch/unset-pending-delete'
                : 'Plugins/PersonalRatings/ratings/batch/set-pending-delete',
            {
                itemIds: [itemId]
            }).then(function () {
                loadRating(itemId);
            }).catch(function () {
                updateActivePanelMessage(itemId, '待删除状态更新失败。');
            });
    }

    function deletePhysical(itemId) {
        if (!deleteFeatureEnabled) {
            updateActivePanelMessage(itemId, '物理删除功能当前已被插件配置禁用。');
            return;
        }

        if (!isAdministrator) {
            updateActivePanelMessage(itemId, '只有管理员可以执行物理删除。');
            return;
        }

        if (!window.confirm('物理删除会直接删除 Jellyfin 条目及其底层文件位置，且会写入审计日志。确定继续吗？')) {
            updateActivePanelMessage(itemId, '已取消物理删除。');
            return;
        }

        updateActivePanelMessage(itemId, '正在执行物理删除...');

        postJson('Plugins/PersonalRatings/ratings/batch/delete-physical', {
            itemIds: [itemId],
            confirmDelete: true
        }).then(function (result) {
            var deletedCount = result && typeof result.DeletedCount === 'number' ? result.DeletedCount : 0;
            var itemResult = result && result.Items && result.Items.length > 0 ? result.Items[0] : null;
            if (deletedCount > 0) {
                var successMessage = '条目已物理删除，正在跳转到我的评分库...';
                var redirectDelay = 350;
                if (itemResult && itemResult.SuggestedAction) {
                    successMessage = '条目已物理删除，但仍需处理：' + itemResult.SuggestedAction;
                    redirectDelay = 1400;
                }

                updateActivePanelMessage(itemId, successMessage);
                window.setTimeout(openManagePage, redirectDelay);
                return;
            }

            if (itemResult && itemResult.Message) {
                var failureMessage = '物理删除失败：' + itemResult.Message;
                if (itemResult.SuggestedAction) {
                    failureMessage += ' ' + itemResult.SuggestedAction;
                }

                updateActivePanelMessage(itemId, failureMessage);
                return;
            }

            updateActivePanelMessage(itemId, '物理删除未成功。');
        }).catch(function (error) {
            if (error && error.status === 403) {
                updateActivePanelMessage(itemId, '只有管理员可以执行物理删除。');
                return;
            }

            if (error && error.status === 409) {
                updateActivePanelMessage(itemId, '物理删除功能当前已被插件配置禁用。');
                return;
            }

            updateActivePanelMessage(itemId, '物理删除失败。');
        });
    }

    function buildSummary(result) {
        var scoreText = result.Score > 0 ? ('当前评分：' + result.Score + ' 分') : '当前评分：未评分';
        var tags = [];

        if (result.IsPlayed) {
            tags.push('已播放');
        }

        if (result.IsPendingDelete) {
            tags.push('待删除');
        }

        if (result.RatedAt) {
            tags.push('最近评分 ' + formatDate(result.RatedAt));
        }

        if (tags.length === 0) {
            tags.push('可以直接在此处完成打分、清分和待删除切换。');
        }

        return scoreText + ' · ' + tags.join(' · ');
    }

    function renderSummary(panel, result, message) {
        var summaryNode = panel.querySelector('.personalRatingsDetailSummary');
        var pendingButton = panel.querySelector('.personalRatingsPendingButton');

        summaryNode.textContent = message;

        if (result && result.IsPendingDelete) {
            pendingButton.textContent = '取消待删除';
        } else {
            pendingButton.textContent = '标记待删除';
        }
    }

    function syncScoreButtons(panel, score) {
        panel.querySelectorAll('.personalRatingsScoreButton').forEach(function (button) {
            var buttonScore = parseInt(button.getAttribute('data-score'), 10);
            button.classList.toggle('is-active', buttonScore === score);
        });
    }

    function updateActivePanelMessage(itemId, message) {
        var panel = getActivePanel(itemId);
        if (!panel) {
            return;
        }

        panel.querySelector('.personalRatingsDetailSummary').textContent = message;
    }

    function getActivePanel(itemId) {
        var panel = document.querySelector('.' + panelClassName);
        if (!panel || panel.dataset.itemId !== itemId) {
            return null;
        }

        return panel;
    }

    function ensureLauncher() {
        var launcher = document.getElementById(launcherId);
        if (!launcher) {
            launcher = document.createElement('button');
            launcher.id = launcherId;
            launcher.type = 'button';
            launcher.className = 'button-flat';
            launcher.textContent = '我的评分库';
            launcher.addEventListener('click', openManagePage);
            document.body.appendChild(launcher);
        }

        updateLauncherVisibility();
    }

    function hideLauncher() {
        var launcher = document.getElementById(launcherId);
        if (launcher) {
            launcher.classList.add('is-hidden');
        }
    }

    function updateLauncherVisibility() {
        var launcher = document.getElementById(launcherId);
        if (!launcher) {
            return;
        }

        var isManagePage = (window.location.hash || '').indexOf('configurationpage?name=PersonalRatingsManagePage') !== -1;
        launcher.classList.toggle('is-hidden', isManagePage || !managePageEnabled);
    }

    function openManagePage() {
        if (!managePageEnabled) {
            return;
        }

        if (window.Dashboard && typeof window.Dashboard.navigate === 'function') {
            window.Dashboard.navigate(route);
            return;
        }

        window.location.hash = '#/' + route;
    }

    function removeDetailPanel() {
        var panel = document.querySelector('.' + panelClassName);
        if (panel) {
            panel.remove();
        }
    }

    function ensureUserContext() {
        if (isUserContextLoading || !window.ApiClient || typeof window.ApiClient.getCurrentUser !== 'function') {
            return;
        }

        isUserContextLoading = true;

        window.ApiClient.getCurrentUser().then(function (user) {
            isAdministrator = !!(user && user.Policy && user.Policy.IsAdministrator);
            isUserContextLoading = false;
            renderAdminControls(document.querySelector('.' + panelClassName));
        }).catch(function () {
            isAdministrator = false;
            isUserContextLoading = false;
            renderAdminControls(document.querySelector('.' + panelClassName));
        });
    }

    function ensureFeatureState() {
        if (isFeatureStateLoading) {
            return;
        }

        isFeatureStateLoading = true;

        window.fetch('/Plugins/PersonalRatings/features', {
            credentials: 'same-origin'
        }).then(function (response) {
            if (!response.ok) {
                throw new Error('Failed to load plugin feature state.');
            }

            return response.json();
        }).then(function (result) {
            deleteFeatureEnabled = !!(result && result.IsDeleteFeatureEnabled);
            managePageEnabled = !!(result && result.IsManagePageEnabled);
            isFeatureStateLoading = false;
            renderAdminControls(document.querySelector('.' + panelClassName));
            updateLauncherVisibility();
        }).catch(function () {
            deleteFeatureEnabled = true;
            managePageEnabled = true;
            isFeatureStateLoading = false;
            renderAdminControls(document.querySelector('.' + panelClassName));
            updateLauncherVisibility();
        });
    }

    function renderAdminControls(panel) {
        if (!panel) {
            return;
        }

        var deleteButton = panel.querySelector('.personalRatingsDeleteButton');
        if (deleteButton) {
            deleteButton.hidden = !isAdministrator || !deleteFeatureEnabled;
        }

        var manageButton = panel.querySelector('.personalRatingsManageButton');
        if (manageButton) {
            manageButton.hidden = !managePageEnabled;
        }
    }

    function postJson(path, payload) {
        var apiClient = window.ApiClient;
        return apiClient.ajax({
            type: 'POST',
            url: apiClient.getUrl(path),
            contentType: 'application/json',
            dataType: 'json',
            data: JSON.stringify(payload)
        });
    }

    function formatDate(value) {
        if (!value) {
            return '';
        }

        var date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return '';
        }

        return date.toLocaleString('zh-CN', {
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    function injectStyles() {
        if (document.getElementById(styleId)) {
            return;
        }

        var style = document.createElement('style');
        style.id = styleId;
        style.textContent = [
            '.personalRatingsDetailPanel {',
            '  margin-top: 18px;',
            '  padding: 18px 20px;',
            '  border-radius: 22px;',
            '  border: 1px solid rgba(255, 255, 255, 0.08);',
            '  background: linear-gradient(155deg, rgba(14, 18, 26, 0.92), rgba(28, 37, 52, 0.86));',
            '  box-shadow: 0 20px 42px rgba(0, 0, 0, 0.24);',
            '}',
            '.personalRatingsPanelHeader {',
            '  display: flex;',
            '  justify-content: space-between;',
            '  gap: 12px;',
            '  align-items: center;',
            '}',
            '.personalRatingsPanelHeader h3 {',
            '  margin: 4px 0 0;',
            '}',
            '.personalRatingsPanelEyebrow {',
            '  font-size: 11px;',
            '  letter-spacing: 0.14em;',
            '  text-transform: uppercase;',
            '  color: rgba(255, 255, 255, 0.62);',
            '}',
            '.personalRatingsScoreRow, .personalRatingsMetaRow {',
            '  display: flex;',
            '  flex-wrap: wrap;',
            '  gap: 10px;',
            '  margin-top: 14px;',
            '}',
            '.personalRatingsScoreButton.is-active {',
            '  background: rgba(229, 139, 47, 0.22);',
            '  border-color: rgba(229, 139, 47, 0.4);',
            '  color: #fff3e2;',
            '}',
            '.personalRatingsDeleteButton {',
            '  border-color: rgba(255, 107, 107, 0.32);',
            '  color: #ffd0d0;',
            '}',
            '.personalRatingsDeleteButton:hover {',
            '  background: rgba(255, 107, 107, 0.16);',
            '}',
            '.personalRatingsDetailSummary {',
            '  margin-top: 14px;',
            '  color: rgba(255, 255, 255, 0.74);',
            '  line-height: 1.55;',
            '}',
            '#' + launcherId + ' {',
            '  position: fixed;',
            '  right: 20px;',
            '  bottom: 20px;',
            '  z-index: 3000;',
            '  padding: 12px 16px;',
            '  border-radius: 999px;',
            '  border: 1px solid rgba(229, 139, 47, 0.32);',
            '  background: rgba(229, 139, 47, 0.18);',
            '  color: #fff6ea;',
            '  box-shadow: 0 16px 36px rgba(0, 0, 0, 0.26);',
            '}',
            '#' + launcherId + '.is-hidden {',
            '  display: none;',
            '}',
            '@media (max-width: 760px) {',
            '  .personalRatingsDetailPanel {',
            '    padding: 16px;',
            '  }',
            '  .personalRatingsPanelHeader {',
            '    flex-direction: column;',
            '    align-items: flex-start;',
            '  }',
            '  #' + launcherId + ' {',
            '    right: 12px;',
            '    bottom: 12px;',
            '  }',
            '}'
        ].join('\n');

        document.head.appendChild(style);
    }
})();

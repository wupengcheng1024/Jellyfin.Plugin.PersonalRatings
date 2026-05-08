(function () {
    'use strict';

    if (window.PersonalRatingsManagePage) {
        return;
    }

    var ManagePage = {
        register: function (page) {
            if (!page || page.dataset.personalRatingsRegistered === 'true') {
                return;
            }

            page.dataset.personalRatingsRegistered = 'true';
            page._personalRatingsState = {
                preset: 'ratedAll',
                pageNumber: 1,
                pageSize: 25,
                sortBy: 'updatedAt',
                sortOrder: 'desc',
                keyword: '',
                selectedItemIds: {},
                lastResult: null,
                isLoading: false,
                requestVersion: 0
            };

            this.bindEvents(page);
            this.safeLoad(page);

            page.addEventListener('pageshow', function () {
                ManagePage.safeLoad(page);
            });
        },

        bindEvents: function (page) {
            page.addEventListener('click', function (event) {
                var target = event.target;
                if (!target) {
                    return;
                }

                var button = target.closest('button, a');
                if (!button) {
                    return;
                }

                if (button.classList.contains('personalRatingsRefreshButton')) {
                    event.preventDefault();
                    ManagePage.safeLoad(page);
                    return;
                }

                if (button.classList.contains('personalRatingsClearSearchButton')) {
                    event.preventDefault();
                    page.querySelector('.txtKeyword').value = '';
                    ManagePage.applyKeyword(page);
                    return;
                }

                if (button.hasAttribute('data-preset')) {
                    event.preventDefault();
                    ManagePage.applyPreset(page, button.getAttribute('data-preset'));
                    return;
                }

                if (button.hasAttribute('data-batch-score')) {
                    event.preventDefault();
                    ManagePage.runBatch(page, 'setScore', button.getAttribute('data-batch-score'));
                    return;
                }

                if (button.hasAttribute('data-batch-action')) {
                    event.preventDefault();
                    ManagePage.runBatch(page, button.getAttribute('data-batch-action'));
                    return;
                }

                if (button.hasAttribute('data-row-pending')) {
                    event.preventDefault();
                    ManagePage.runRowPendingToggle(
                        page,
                        button.getAttribute('data-item-id'),
                        button.getAttribute('data-row-pending') === 'true');
                    return;
                }

                if (button.classList.contains('personalRatingsPrevPageButton')) {
                    event.preventDefault();
                    ManagePage.changePage(page, -1);
                    return;
                }

                if (button.classList.contains('personalRatingsNextPageButton')) {
                    event.preventDefault();
                    ManagePage.changePage(page, 1);
                }
            });

            page.addEventListener('change', function (event) {
                var target = event.target;
                if (!target) {
                    return;
                }

                if (target.classList.contains('selectSort')) {
                    ManagePage.applySort(page, target.value);
                    return;
                }

                if (target.classList.contains('selectPageSize')) {
                    ManagePage.applyPageSize(page, target.value);
                    return;
                }

                if (target.classList.contains('checkSelectAll')) {
                    ManagePage.toggleSelectAll(page, target.checked);
                    return;
                }

                if (target.classList.contains('personalRatingsRowCheckbox')) {
                    ManagePage.toggleSelectedItem(page, target.getAttribute('data-item-id'), target.checked);
                }
            });

            page.querySelector('.personalRatingsSearchForm').addEventListener('submit', function (event) {
                event.preventDefault();
                ManagePage.applyKeyword(page);
            });
        },

        applyPreset: function (page, preset) {
            var state = this.getState(page);
            state.preset = preset;
            state.pageNumber = 1;

            if (preset === 'recent') {
                state.sortBy = 'ratedAt';
                state.sortOrder = 'desc';
                page.querySelector('.selectSort').value = 'ratedAt:desc';
            } else if (preset === 'playedUnrated') {
                state.sortBy = 'lastPlayedAt';
                state.sortOrder = 'desc';
                page.querySelector('.selectSort').value = 'lastPlayedAt:desc';
            }

            this.safeLoad(page);
        },

        applyKeyword: function (page) {
            var state = this.getState(page);
            state.keyword = page.querySelector('.txtKeyword').value.trim();
            state.pageNumber = 1;
            this.safeLoad(page);
        },

        applySort: function (page, value) {
            var state = this.getState(page);
            var parts = value.split(':');
            state.sortBy = parts[0] || 'updatedAt';
            state.sortOrder = parts[1] || 'desc';
            state.pageNumber = 1;
            this.safeLoad(page);
        },

        applyPageSize: function (page, value) {
            var state = this.getState(page);
            state.pageSize = parseInt(value, 10) || 25;
            state.pageNumber = 1;
            this.safeLoad(page);
        },

        changePage: function (page, delta) {
            var state = this.getState(page);
            var nextPage = state.pageNumber + delta;
            if (nextPage < 1) {
                return;
            }

            state.pageNumber = nextPage;
            this.safeLoad(page);
        },

        toggleSelectAll: function (page, isSelected) {
            var state = this.getState(page);
            var items = state.lastResult && state.lastResult.Items ? state.lastResult.Items : [];
            var selectedItemIds = {};

            if (isSelected) {
                items.forEach(function (item) {
                    selectedItemIds[item.ItemId] = true;
                });
            }

            state.selectedItemIds = selectedItemIds;
            this.render(page);
        },

        toggleSelectedItem: function (page, itemId, isSelected) {
            var state = this.getState(page);
            if (!itemId) {
                return;
            }

            if (isSelected) {
                state.selectedItemIds[itemId] = true;
            } else {
                delete state.selectedItemIds[itemId];
            }

            this.renderSelectionState(page);
        },

        runBatch: function (page, action, value) {
            var selectedItemIds = this.getSelectedItemIds(page);
            if (selectedItemIds.length === 0) {
                this.setStatus(page, '请先选择至少一个条目。', 'error');
                return;
            }

            var path = '';
            var payload = {
                itemIds: selectedItemIds
            };

            if (action === 'setScore') {
                path = 'Plugins/PersonalRatings/ratings/batch/set-score';
                payload.score = parseInt(value, 10);
            } else if (action === 'clear') {
                path = 'Plugins/PersonalRatings/ratings/batch/clear-score';
            } else if (action === 'pendingOn') {
                path = 'Plugins/PersonalRatings/ratings/batch/set-pending-delete';
            } else if (action === 'pendingOff') {
                path = 'Plugins/PersonalRatings/ratings/batch/unset-pending-delete';
            }

            if (!path) {
                return;
            }

            this.setStatus(page, '正在提交批量操作...', 'loading');

            try {
                this.postJson(path, payload).then(function (result) {
                    var affectedCount = result && typeof result.AffectedCount === 'number' ? result.AffectedCount : 0;
                    ManagePage.setStatus(page, '批量操作完成，已影响 ' + affectedCount + ' 条记录。', 'success');
                    ManagePage.getState(page).selectedItemIds = {};
                    ManagePage.safeLoad(page);
                }).catch(function (error) {
                    ManagePage.handleRequestError(page, error, '批量操作失败。');
                });
            } catch (error) {
                this.handleRequestError(page, error, '批量操作失败。');
            }
        },

        runRowPendingToggle: function (page, itemId, shouldSetPendingDelete) {
            if (!itemId) {
                return;
            }

            var path = shouldSetPendingDelete
                ? 'Plugins/PersonalRatings/ratings/batch/set-pending-delete'
                : 'Plugins/PersonalRatings/ratings/batch/unset-pending-delete';

            try {
                this.postJson(path, {
                    itemIds: [itemId]
                }).then(function () {
                    ManagePage.setStatus(page, shouldSetPendingDelete ? '已标记待删除。' : '已取消待删除。', 'success');
                    ManagePage.safeLoad(page);
                }).catch(function (error) {
                    ManagePage.handleRequestError(page, error, '更新待删除状态失败。');
                });
            } catch (error) {
                this.handleRequestError(page, error, '更新待删除状态失败。');
            }
        },

        safeLoad: function (page) {
            try {
                this.load(page);
            } catch (error) {
                this.handleRequestError(page, error, '当前页面未取得 Jellyfin Web 的登录上下文。');
            }
        },

        load: function (page) {
            var state = this.getState(page);
            var requestBody = this.buildQueryRequest(state);
            state.requestVersion += 1;

            var currentVersion = state.requestVersion;
            this.setLoading(page, true);
            this.setStatus(page, '正在加载列表...', 'loading');

            this.postJson('Plugins/PersonalRatings/ratings/query', requestBody).then(function (result) {
                if (ManagePage.getState(page).requestVersion !== currentVersion) {
                    return;
                }

                state.lastResult = result;
                state.selectedItemIds = ManagePage.pruneSelection(state.selectedItemIds, result.Items || []);
                ManagePage.render(page);
                ManagePage.setStatus(page, '列表已刷新。', 'success');
            }).catch(function (error) {
                if (ManagePage.getState(page).requestVersion !== currentVersion) {
                    return;
                }

                state.lastResult = {
                    Items: [],
                    TotalCount: 0,
                    PageNumber: state.pageNumber,
                    PageSize: state.pageSize
                };
                ManagePage.render(page);
                ManagePage.handleRequestError(page, error, '加载评分列表失败。');
            }).then(function () {
                if (ManagePage.getState(page).requestVersion === currentVersion) {
                    ManagePage.setLoading(page, false);
                }
            });
        },

        render: function (page) {
            var state = this.getState(page);
            var result = state.lastResult || {
                Items: [],
                TotalCount: 0,
                PageNumber: state.pageNumber,
                PageSize: state.pageSize
            };
            var items = result.Items || [];
            var rowsContainer = page.querySelector('.personalRatingsRows');
            var apiClient = this.getApiClient();
            var serverId = apiClient.serverId();

            page.querySelectorAll('.personalRatingsPresetButton').forEach(function (button) {
                button.classList.toggle('is-active', button.getAttribute('data-preset') === state.preset);
            });

            if (items.length === 0) {
                rowsContainer.innerHTML = '<tr><td colspan="7" class="personalRatingsEmptyState">当前筛选条件下没有记录。</td></tr>';
            } else {
                rowsContainer.innerHTML = items.map(function (item) {
                    var itemName = ManagePage.escapeHtml(item.ItemName || item.ItemId);
                    var itemType = ManagePage.escapeHtml(item.ItemType || item.MediaType || 'Unknown');
                    var yearText = item.ProductionYear ? ' / ' + item.ProductionYear : '';
                    var detailsUrl = '#/details?id=' + encodeURIComponent(item.ItemId) + '&serverId=' + encodeURIComponent(serverId);
                    var isSelected = !!state.selectedItemIds[item.ItemId];
                    var scoreText = item.Score > 0 ? item.Score + '分' : '未评分';
                    var statusTags = [];

                    if (item.IsPendingDelete) {
                        statusTags.push('<span class="personalRatingsTag">待删除</span>');
                    }

                    if (item.IsPlayed) {
                        statusTags.push('<span class="personalRatingsTag">已播放</span>');
                    }

                    if (!item.IsPlayed && item.Score === 0) {
                        statusTags.push('<span class="personalRatingsTag">未播放未评分</span>');
                    }

                    if (statusTags.length === 0) {
                        statusTags.push('<span class="personalRatingsTag">正常</span>');
                    }

                    return ''
                        + '<tr>'
                        + '<td class="personalRatingsCheckboxColumn"><input is="emby-checkbox" type="checkbox" class="personalRatingsRowCheckbox" data-item-id="' + ManagePage.escapeHtml(item.ItemId) + '"' + (isSelected ? ' checked="checked"' : '') + ' /></td>'
                        + '<td>'
                        + '<a class="personalRatingsItemName" href="' + detailsUrl + '">' + itemName + '</a>'
                        + '<div class="personalRatingsItemMeta">' + itemType + yearText + '</div>'
                        + '</td>'
                        + '<td><span class="personalRatingsScoreBadge' + (item.Score > 0 ? ' is-rated' : '') + '">' + scoreText + '</span></td>'
                        + '<td><div class="personalRatingsTagList">' + statusTags.join('') + '</div></td>'
                        + '<td>' + ManagePage.formatDate(item.RatedAt) + '</td>'
                        + '<td>' + ManagePage.formatDate(item.UpdatedAt) + '</td>'
                        + '<td><div class="personalRatingsRowActions">'
                        + '<a is="emby-linkbutton" class="button-flat" href="' + detailsUrl + '">打开详情</a>'
                        + '<button is="emby-button" type="button" class="button-flat" data-item-id="' + ManagePage.escapeHtml(item.ItemId) + '" data-row-pending="' + (!item.IsPendingDelete ? 'true' : 'false') + '">' + (item.IsPendingDelete ? '取消待删除' : '标记待删除') + '</button>'
                        + '</div></td>'
                        + '</tr>';
                }).join('');
            }

            this.renderSelectionState(page);
            this.renderPagination(page, result);
            this.renderSummary(page, result);
        },

        renderSelectionState: function (page) {
            var selectedItemIds = this.getSelectedItemIds(page);
            page.querySelector('.selectedCountText').textContent = '已选 ' + selectedItemIds.length + ' 项';

            var result = this.getState(page).lastResult;
            var items = result && result.Items ? result.Items : [];
            var hasItems = items.length > 0;
            var isAllSelected = hasItems && selectedItemIds.length === items.length;
            page.querySelector('.checkSelectAll').checked = isAllSelected;
        },

        renderPagination: function (page, result) {
            var totalCount = result.TotalCount || 0;
            var pageSize = result.PageSize || this.getState(page).pageSize || 25;
            var pageNumber = result.PageNumber || this.getState(page).pageNumber || 1;
            var totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

            page.querySelector('.personalRatingsPageText').textContent = '第 ' + pageNumber + ' / ' + totalPages + ' 页';
            page.querySelector('.personalRatingsPrevPageButton').disabled = pageNumber <= 1;
            page.querySelector('.personalRatingsNextPageButton').disabled = pageNumber >= totalPages;
        },

        renderSummary: function (page, result) {
            var totalCount = result.TotalCount || 0;
            var pageNumber = result.PageNumber || 1;
            var pageSize = result.PageSize || this.getState(page).pageSize || 25;
            var startIndex = totalCount === 0 ? 0 : ((pageNumber - 1) * pageSize) + 1;
            var endIndex = Math.min(totalCount, pageNumber * pageSize);
            page.querySelector('.personalRatingsSummaryText').textContent = '共 ' + totalCount + ' 条，当前显示 ' + startIndex + '-' + endIndex + '。';
        },

        buildQueryRequest: function (state) {
            var request = {
                pageNumber: state.pageNumber,
                pageSize: state.pageSize,
                sortBy: state.sortBy,
                sortOrder: state.sortOrder
            };

            if (state.keyword) {
                request.keyword = state.keyword;
            }

            switch (state.preset) {
                case 'ratedAll':
                    request.isRated = true;
                    break;
                case 'score5':
                    request.isRated = true;
                    request.score = 5;
                    break;
                case 'score4':
                    request.isRated = true;
                    request.score = 4;
                    break;
                case 'score3':
                    request.isRated = true;
                    request.score = 3;
                    break;
                case 'score2':
                    request.isRated = true;
                    request.score = 2;
                    break;
                case 'score1':
                    request.isRated = true;
                    request.score = 1;
                    break;
                case 'unrated':
                    request.isRated = false;
                    request.score = 0;
                    break;
                case 'pendingDelete':
                    request.isPendingDelete = true;
                    break;
                case 'recent':
                    request.isRated = true;
                    break;
                case 'playedUnrated':
                    request.isPlayed = true;
                    request.isRated = false;
                    request.score = 0;
                    break;
            }

            return request;
        },

        pruneSelection: function (selectedItemIds, items) {
            var nextSelectedItemIds = {};
            var validItemIds = {};

            items.forEach(function (item) {
                validItemIds[item.ItemId] = true;
            });

            Object.keys(selectedItemIds || {}).forEach(function (itemId) {
                if (validItemIds[itemId]) {
                    nextSelectedItemIds[itemId] = true;
                }
            });

            return nextSelectedItemIds;
        },

        getSelectedItemIds: function (page) {
            return Object.keys(this.getState(page).selectedItemIds || {});
        },

        setLoading: function (page, isLoading) {
            this.getState(page).isLoading = isLoading;
            page.classList.toggle('is-loading', isLoading);
        },

        setStatus: function (page, message, statusType) {
            var statusNode = page.querySelector('.personalRatingsStatusText');
            statusNode.textContent = message || '';
            statusNode.classList.remove('is-error', 'is-success', 'is-loading');

            if (statusType === 'error') {
                statusNode.classList.add('is-error');
            } else if (statusType === 'success') {
                statusNode.classList.add('is-success');
            } else if (statusType === 'loading') {
                statusNode.classList.add('is-loading');
            }
        },

        handleRequestError: function (page, error, fallbackMessage) {
            var message = fallbackMessage;

            if (error && typeof error.status === 'number') {
                message += ' HTTP ' + error.status + '.';
            } else if (error && error.message) {
                message += ' ' + error.message;
            }

            this.setStatus(page, message, 'error');
        },

        postJson: function (path, payload) {
            var apiClient = this.getApiClient();
            return apiClient.ajax({
                type: 'POST',
                url: apiClient.getUrl(path),
                contentType: 'application/json',
                dataType: 'json',
                data: JSON.stringify(payload)
            });
        },

        getApiClient: function () {
            if (!window.ApiClient || typeof window.ApiClient.isLoggedIn !== 'function' || !window.ApiClient.isLoggedIn()) {
                throw new Error('ApiClient is unavailable or the user is not authenticated.');
            }

            return window.ApiClient;
        },

        getState: function (page) {
            return page._personalRatingsState;
        },

        formatDate: function (value) {
            if (!value) {
                return '-';
            }

            var date = new Date(value);
            if (Number.isNaN(date.getTime())) {
                return '-';
            }

            return date.toLocaleString('zh-CN', {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit'
            });
        },

        escapeHtml: function (value) {
            return String(value)
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#39;');
        }
    };

    window.PersonalRatingsManagePage = ManagePage;
})();

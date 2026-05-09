(function () {
    'use strict';

    if (window.PersonalRatingsAuditPage) {
        return;
    }

    var AuditPage = {
        register: function (page) {
            if (!page || page.dataset.personalRatingsAuditRegistered === 'true') {
                return;
            }

            page.dataset.personalRatingsAuditRegistered = 'true';
            page._personalRatingsAuditState = {
                keyword: '',
                itemId: '',
                result: '',
                createdAfter: '',
                createdBefore: '',
                pageNumber: 1,
                pageSize: 25,
                isAdministrator: false,
                features: {
                    manageEnabled: true
                },
                requestVersion: 0,
                lastResult: null
            };

            this.bindEvents(page);
            this.loadFeatureState(page).then(function () {
                return AuditPage.loadUserContext(page);
            }).finally(function () {
                AuditPage.safeLoad(page);
            });

            page.addEventListener('pageshow', function () {
                AuditPage.safeLoad(page);
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

                if (button.classList.contains('personalRatingsAuditRefreshButton')) {
                    event.preventDefault();
                    AuditPage.safeLoad(page);
                    return;
                }

                if (button.classList.contains('personalRatingsAuditClearButton')) {
                    event.preventDefault();
                    AuditPage.clearFilters(page);
                    return;
                }

                if (button.classList.contains('personalRatingsAuditPrevPageButton')) {
                    event.preventDefault();
                    AuditPage.changePage(page, -1);
                    return;
                }

                if (button.classList.contains('personalRatingsAuditNextPageButton')) {
                    event.preventDefault();
                    AuditPage.changePage(page, 1);
                }
            });

            page.querySelector('.selectAuditPageSize').addEventListener('change', function (event) {
                var state = AuditPage.getState(page);
                state.pageSize = parseInt(event.target.value, 10) || 25;
                state.pageNumber = 1;
                AuditPage.safeLoad(page);
            });

            page.querySelector('.personalRatingsAuditSearchForm').addEventListener('submit', function (event) {
                event.preventDefault();
                AuditPage.applyFilters(page);
            });
        },

        loadFeatureState: function (page) {
            return fetch('/Plugins/PersonalRatings/features', {
                credentials: 'same-origin'
            }).then(function (response) {
                if (!response.ok) {
                    throw new Error('Failed to load plugin feature state.');
                }

                return response.json();
            }).then(function (result) {
                var state = AuditPage.getState(page);
                state.features.manageEnabled = !!(result && result.IsManagePageEnabled);
            }).catch(function () {
                var state = AuditPage.getState(page);
                state.features.manageEnabled = true;
            });
        },

        loadUserContext: function (page) {
            try {
                return this.getApiClient().getCurrentUser().then(function (user) {
                    AuditPage.getState(page).isAdministrator = !!(user && user.Policy && user.Policy.IsAdministrator);
                }).catch(function () {
                    AuditPage.getState(page).isAdministrator = false;
                });
            } catch (error) {
                AuditPage.getState(page).isAdministrator = false;
                return Promise.resolve();
            }
        },

        safeLoad: function (page) {
            var state = this.getState(page);
            if (!state.features.manageEnabled) {
                this.renderDisabled(page, '“我的评分库”功能当前已被插件配置禁用。');
                return;
            }

            if (!state.isAdministrator) {
                this.renderDisabled(page, '只有管理员可以查看删除审计日志。');
                return;
            }

            this.load(page);
        },

        load: function (page) {
            var state = this.getState(page);
            state.requestVersion += 1;
            var currentVersion = state.requestVersion;

            this.setLoading(page, true);
            this.setStatus(page, '正在加载删除审计...', 'loading');

            this.postJson('Plugins/PersonalRatings/audit-logs/query', this.buildQueryRequest(state)).then(function (result) {
                if (AuditPage.getState(page).requestVersion !== currentVersion) {
                    return;
                }

                state.lastResult = result;
                AuditPage.render(page);
                AuditPage.setStatus(page, '审计列表已刷新。', 'success');
            }).catch(function (error) {
                if (AuditPage.getState(page).requestVersion !== currentVersion) {
                    return;
                }

                state.lastResult = {
                    Items: [],
                    TotalCount: 0,
                    PageNumber: state.pageNumber,
                    PageSize: state.pageSize
                };
                AuditPage.render(page);
                AuditPage.handleRequestError(page, error, '加载删除审计失败。');
            }).then(function () {
                if (AuditPage.getState(page).requestVersion === currentVersion) {
                    AuditPage.setLoading(page, false);
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
            var rowsContainer = page.querySelector('.personalRatingsAuditRows');
            var serverId = this.getApiClient().serverId();

            if (!items.length) {
                rowsContainer.innerHTML = '<tr><td colspan="5" class="personalRatingsEmptyState">当前筛选条件下没有审计记录。</td></tr>';
            } else {
                rowsContainer.innerHTML = items.map(function (item) {
                    var itemName = AuditPage.escapeHtml(item.ItemName || item.ItemId);
                    var itemId = AuditPage.escapeHtml(item.ItemId);
                    var detailsUrl = '#/details?id=' + encodeURIComponent(item.ItemId) + '&serverId=' + encodeURIComponent(serverId);
                    var detailLine = item.ItemId
                        ? '<div class="personalRatingsItemMeta"><a class="personalRatingsItemName" href="' + detailsUrl + '">' + itemName + '</a><div class="personalRatingsItemMeta">' + itemId + '</div></div>'
                        : '<div class="personalRatingsItemMeta">' + itemName + '</div>';

                    return ''
                        + '<tr>'
                        + '<td>' + AuditPage.formatDate(item.CreatedAt) + '</td>'
                        + '<td><span class="personalRatingsTag">' + AuditPage.escapeHtml(item.Result || '-') + '</span></td>'
                        + '<td>' + detailLine + '</td>'
                        + '<td><div>' + AuditPage.escapeHtml(item.Message || '-') + '</div><div class="personalRatingsItemMeta">' + AuditPage.escapeHtml(item.Action || '-') + '</div></td>'
                        + '<td><code>' + AuditPage.escapeHtml(item.OperatorUserId || '-') + '</code></td>'
                        + '</tr>';
                }).join('');
            }

            this.renderPagination(page, result);
            this.renderSummary(page, result);
        },

        renderDisabled: function (page, message) {
            page.querySelector('.personalRatingsAuditRows').innerHTML = '<tr><td colspan="5" class="personalRatingsEmptyState">' + this.escapeHtml(message) + '</td></tr>';
            page.querySelector('.personalRatingsAuditSummaryText').textContent = message;
            this.setStatus(page, message, 'error');
            page.querySelector('.personalRatingsAuditPrevPageButton').disabled = true;
            page.querySelector('.personalRatingsAuditNextPageButton').disabled = true;
        },

        renderPagination: function (page, result) {
            var totalCount = result.TotalCount || 0;
            var pageSize = result.PageSize || this.getState(page).pageSize || 25;
            var pageNumber = result.PageNumber || this.getState(page).pageNumber || 1;
            var totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

            page.querySelector('.personalRatingsAuditPageText').textContent = '第 ' + pageNumber + ' / ' + totalPages + ' 页';
            page.querySelector('.personalRatingsAuditPrevPageButton').disabled = pageNumber <= 1;
            page.querySelector('.personalRatingsAuditNextPageButton').disabled = pageNumber >= totalPages;
        },

        renderSummary: function (page, result) {
            var totalCount = result.TotalCount || 0;
            var pageNumber = result.PageNumber || 1;
            var pageSize = result.PageSize || this.getState(page).pageSize || 25;
            var startIndex = totalCount === 0 ? 0 : ((pageNumber - 1) * pageSize) + 1;
            var endIndex = Math.min(totalCount, pageNumber * pageSize);
            page.querySelector('.personalRatingsAuditSummaryText').textContent = '共 ' + totalCount + ' 条，当前显示 ' + startIndex + '-' + endIndex + '。';
        },

        applyFilters: function (page) {
            var state = this.getState(page);
            state.keyword = page.querySelector('.txtAuditKeyword').value.trim();
            state.itemId = page.querySelector('.txtAuditItemId').value.trim();
            state.result = page.querySelector('.selectAuditResult').value || '';
            state.createdAfter = page.querySelector('.txtAuditCreatedAfter').value || '';
            state.createdBefore = page.querySelector('.txtAuditCreatedBefore').value || '';
            state.pageNumber = 1;
            this.safeLoad(page);
        },

        clearFilters: function (page) {
            page.querySelector('.txtAuditKeyword').value = '';
            page.querySelector('.txtAuditItemId').value = '';
            page.querySelector('.selectAuditResult').value = '';
            page.querySelector('.txtAuditCreatedAfter').value = '';
            page.querySelector('.txtAuditCreatedBefore').value = '';

            var state = this.getState(page);
            state.keyword = '';
            state.itemId = '';
            state.result = '';
            state.createdAfter = '';
            state.createdBefore = '';
            state.pageNumber = 1;
            this.safeLoad(page);
        },

        buildQueryRequest: function (state) {
            var request = {
                pageNumber: state.pageNumber,
                pageSize: state.pageSize
            };

            if (state.keyword) {
                request.keyword = state.keyword;
            }

            if (state.itemId) {
                request.itemId = state.itemId;
            }

            if (state.result) {
                request.result = state.result;
            }

            if (state.createdAfter) {
                request.createdAfterUtc = new Date(state.createdAfter).toISOString();
            }

            if (state.createdBefore) {
                request.createdBeforeUtc = new Date(state.createdBefore).toISOString();
            }

            return request;
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

        setStatus: function (page, message, statusType) {
            var statusNode = page.querySelector('.personalRatingsAuditStatusText');
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

        setLoading: function (page, isLoading) {
            page.classList.toggle('is-loading', isLoading);
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
            return page._personalRatingsAuditState;
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

    window.PersonalRatingsAuditPage = AuditPage;
})();

(function () {
    'use strict';

    if (window.PersonalRatingsTagManagePage) {
        return;
    }

    /**
     * Provides a minimal Jellyfin-style backend page for maintaining tag
     * definitions. It stays inside configurationpage and relies on tag APIs.
     */
    var TagManagePage = {
        register: function (page) {
            if (!page || page.dataset.personalRatingsTagManageRegistered === 'true') {
                return;
            }

            page.dataset.personalRatingsTagManageRegistered = 'true';
            page._personalRatingsTagManageState = {
                features: {
                    manageEnabled: true
                },
                isAdministrator: false,
                items: [],
                isLoading: false,
                requestVersion: 0,
                editingTagId: null
            };

            this.bindEvents(page);
            this.resetForm(page);
            this.loadFeatureState(page).then(function () {
                return TagManagePage.loadUserContext(page);
            }).finally(function () {
                TagManagePage.safeLoad(page);
            });

            page.addEventListener('pageshow', function () {
                TagManagePage.safeLoad(page);
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

                if (button.classList.contains('personalRatingsTagRefreshButton')) {
                    event.preventDefault();
                    TagManagePage.safeLoad(page);
                    return;
                }

                if (button.classList.contains('personalRatingsTagCancelEditButton')) {
                    event.preventDefault();
                    TagManagePage.resetForm(page);
                    return;
                }

                if (button.hasAttribute('data-tag-action')) {
                    event.preventDefault();
                    TagManagePage.handleRowAction(
                        page,
                        button.getAttribute('data-tag-action'),
                        parseInt(button.getAttribute('data-tag-id'), 10));
                }
            });

            page.querySelector('.personalRatingsTagForm').addEventListener('submit', function (event) {
                event.preventDefault();
                TagManagePage.saveTag(page);
            });

            page.querySelector('.txtTagColorPicker').addEventListener('input', function (event) {
                page.querySelector('.txtTagColorText').value = event.target.value;
            });

            page.querySelector('.txtTagColorText').addEventListener('change', function (event) {
                TagManagePage.syncColorInputs(page, event.target.value);
            });
        },

        loadFeatureState: function (page) {
            return window.fetch('/Plugins/PersonalRatings/features', {
                credentials: 'same-origin'
            }).then(function (response) {
                if (!response.ok) {
                    throw new Error('Failed to load plugin feature state.');
                }

                return response.json();
            }).then(function (result) {
                TagManagePage.getState(page).features.manageEnabled = !!(result && result.IsManagePageEnabled);
            }).catch(function () {
                TagManagePage.getState(page).features.manageEnabled = true;
            });
        },

        loadUserContext: function (page) {
            try {
                return this.getApiClient().getCurrentUser().then(function (user) {
                    TagManagePage.getState(page).isAdministrator = !!(user && user.Policy && user.Policy.IsAdministrator);
                }).catch(function () {
                    TagManagePage.getState(page).isAdministrator = false;
                });
            } catch (error) {
                TagManagePage.getState(page).isAdministrator = false;
                return Promise.resolve();
            }
        },

        safeLoad: function (page) {
            var state = this.getState(page);
            if (!state.features.manageEnabled) {
                this.renderDisabled(page, '“打分库 / 评分后台”入口当前已被插件配置禁用。');
                return;
            }

            if (!state.isAdministrator) {
                this.renderDisabled(page, '只有管理员可以维护标签定义。');
                return;
            }

            this.load(page);
        },

        load: function (page) {
            var state = this.getState(page);
            state.requestVersion += 1;
            var requestVersion = state.requestVersion;
            state.isLoading = true;
            this.setStatus(page, '正在加载标签定义...', 'loading');

            this.apiGetJson('Plugins/PersonalRatings/tags?includeDisabled=true').then(function (result) {
                if (TagManagePage.getState(page).requestVersion !== requestVersion) {
                    return;
                }

                state.items = Array.isArray(result) ? result : [];
                TagManagePage.render(page);
                TagManagePage.setStatus(page, '标签列表已刷新。', 'success');
            }).catch(function (error) {
                if (TagManagePage.getState(page).requestVersion !== requestVersion) {
                    return;
                }

                state.items = [];
                TagManagePage.render(page);
                TagManagePage.handleRequestError(page, error, '加载标签定义失败。');
            }).finally(function () {
                if (TagManagePage.getState(page).requestVersion === requestVersion) {
                    state.isLoading = false;
                }
            });
        },

        render: function (page) {
            var state = this.getState(page);
            var rowsContainer = page.querySelector('.personalRatingsTagRows');
            var items = Array.isArray(state.items) ? state.items : [];

            if (!items.length) {
                rowsContainer.innerHTML = '<tr><td colspan="6" class="personalRatingsEmptyState">当前还没有标签定义。</td></tr>';
            } else {
                rowsContainer.innerHTML = items.map(function (tag) {
                    var color = TagManagePage.escapeHtml(tag.Color || '#d88b2f');
                    return ''
                        + '<tr>'
                        + '<td><strong>' + TagManagePage.escapeHtml(tag.Name || '-') + '</strong></td>'
                        + '<td><div class="personalRatingsTagSwatch"><span class="personalRatingsTagSwatchPreview" style="background:' + color + ';"></span><code>' + color + '</code></div></td>'
                        + '<td><span class="personalRatingsTag' + (tag.IsEnabled ? '' : ' personalRatingsTagStatus-disabled') + '">' + (tag.IsEnabled ? '启用中' : '已停用') + '</span></td>'
                        + '<td>' + TagManagePage.escapeHtml(String(tag.SortOrder)) + '</td>'
                        + '<td>' + TagManagePage.formatDate(tag.UpdatedAt) + '</td>'
                        + '<td><div class="personalRatingsRowActions">'
                        + '<button is="emby-button" type="button" class="button-flat" data-tag-action="edit" data-tag-id="' + tag.Id + '">编辑</button>'
                        + '<button is="emby-button" type="button" class="button-flat" data-tag-action="toggle" data-tag-id="' + tag.Id + '">' + (tag.IsEnabled ? '停用' : '启用') + '</button>'
                        + '<button is="emby-button" type="button" class="button-flat personalRatingsDangerButton" data-tag-action="delete" data-tag-id="' + tag.Id + '">删除</button>'
                        + '</div></td>'
                        + '</tr>';
                }).join('');
            }

            page.querySelector('.personalRatingsTagSummaryText').textContent = '共 ' + items.length + ' 个标签定义。';
        },

        renderDisabled: function (page, message) {
            page.querySelector('.personalRatingsTagRows').innerHTML = '<tr><td colspan="6" class="personalRatingsEmptyState">' + this.escapeHtml(message) + '</td></tr>';
            page.querySelector('.personalRatingsTagSummaryText').textContent = message;
            this.setStatus(page, message, 'error');
        },

        saveTag: function (page) {
            var state = this.getState(page);
            if (!state.isAdministrator) {
                this.setStatus(page, '只有管理员可以维护标签定义。', 'error');
                return;
            }

            var payload = this.buildFormPayload(page);
            var path = 'Plugins/PersonalRatings/tags';
            var request;
            var successMessage = '标签已创建。';

            this.setStatus(page, state.editingTagId ? '正在保存标签修改...' : '正在创建标签...', 'loading');

            if (state.editingTagId) {
                path += '/' + encodeURIComponent(state.editingTagId);
                request = this.putJson(path, payload);
                successMessage = '标签已更新。';
            } else {
                request = this.postJson(path, payload);
            }

            request.then(function () {
                TagManagePage.setStatus(page, successMessage, 'success');
                TagManagePage.resetForm(page);
                TagManagePage.load(page);
            }).catch(function (error) {
                TagManagePage.handleRequestError(page, error, '保存标签失败。');
            });
        },

        handleRowAction: function (page, action, tagId) {
            var tag = this.findTag(page, tagId);
            if (!tag) {
                this.setStatus(page, '未找到对应标签。', 'error');
                return;
            }

            if (action === 'edit') {
                this.startEdit(page, tag);
                return;
            }

            if (action === 'toggle') {
                this.toggleEnabled(page, tag);
                return;
            }

            if (action === 'delete') {
                this.deleteTag(page, tag);
            }
        },

        startEdit: function (page, tag) {
            var state = this.getState(page);
            state.editingTagId = tag.Id;
            page.querySelector('.txtTagName').value = tag.Name || '';
            this.syncColorInputs(page, tag.Color || '#d88b2f');
            page.querySelector('.txtTagSortOrder').value = String(tag.SortOrder);
            page.querySelector('.chkTagEnabled').checked = !!tag.IsEnabled;
            page.querySelector('.personalRatingsTagSaveButton span').textContent = '保存修改';
            this.setStatus(page, '已载入标签“' + (tag.Name || '-') + '”供编辑。', 'success');
        },

        toggleEnabled: function (page, tag) {
            this.setStatus(page, tag.IsEnabled ? '正在停用标签...' : '正在启用标签...', 'loading');
            this.putJson('Plugins/PersonalRatings/tags/' + encodeURIComponent(tag.Id), {
                name: tag.Name,
                color: tag.Color,
                sortOrder: tag.SortOrder,
                isEnabled: !tag.IsEnabled
            }).then(function () {
                TagManagePage.setStatus(page, tag.IsEnabled ? '标签已停用。' : '标签已启用。', 'success');
                TagManagePage.load(page);
            }).catch(function (error) {
                TagManagePage.handleRequestError(page, error, '更新标签状态失败。');
            });
        },

        deleteTag: function (page, tag) {
            if (!window.confirm('删除标签“' + (tag.Name || '-') + '”会清理它在用户条目上的关联关系。确定继续吗？')) {
                this.setStatus(page, '已取消删除标签。', 'success');
                return;
            }

            this.setStatus(page, '正在删除标签...', 'loading');
            this.deleteJson('Plugins/PersonalRatings/tags/' + encodeURIComponent(tag.Id)).then(function () {
                TagManagePage.setStatus(page, '标签已删除。', 'success');
                if (TagManagePage.getState(page).editingTagId === tag.Id) {
                    TagManagePage.resetForm(page);
                }

                TagManagePage.load(page);
            }).catch(function (error) {
                TagManagePage.handleRequestError(page, error, '删除标签失败。');
            });
        },

        buildFormPayload: function (page) {
            return {
                name: page.querySelector('.txtTagName').value.trim(),
                color: page.querySelector('.txtTagColorText').value.trim() || '#d88b2f',
                sortOrder: parseInt(page.querySelector('.txtTagSortOrder').value, 10) || 0,
                isEnabled: !!page.querySelector('.chkTagEnabled').checked
            };
        },

        resetForm: function (page) {
            var state = this.getState(page);
            state.editingTagId = null;
            page.querySelector('.txtTagName').value = '';
            page.querySelector('.txtTagSortOrder').value = '10';
            page.querySelector('.chkTagEnabled').checked = true;
            this.syncColorInputs(page, '#d88b2f');
            page.querySelector('.personalRatingsTagSaveButton span').textContent = '保存标签';
        },

        syncColorInputs: function (page, value) {
            var normalized = this.normalizeColor(value);
            page.querySelector('.txtTagColorPicker').value = normalized;
            page.querySelector('.txtTagColorText').value = normalized;
        },

        normalizeColor: function (value) {
            var fallback = '#d88b2f';
            var text = String(value || '').trim();
            if (!/^#([0-9a-fA-F]{6})$/.test(text)) {
                return fallback;
            }

            return text.toLowerCase();
        },

        findTag: function (page, tagId) {
            return this.getState(page).items.find(function (tag) {
                return tag.Id === tagId;
            }) || null;
        },

        setStatus: function (page, message, type) {
            var statusNode = page.querySelector('.personalRatingsTagStatusText');
            statusNode.textContent = message;
            statusNode.classList.remove('is-error', 'is-success', 'is-loading');
            if (type) {
                statusNode.classList.add('is-' + type);
            }
        },

        handleRequestError: function (page, error, fallbackMessage) {
            if (error && error.status === 403) {
                this.setStatus(page, '只有管理员可以维护标签定义。', 'error');
                return;
            }

            if (error && error.status === 400 && error.responseText) {
                this.setStatus(page, error.responseText, 'error');
                return;
            }

            this.setStatus(page, fallbackMessage, 'error');
        },

        getState: function (page) {
            return page._personalRatingsTagManageState;
        },

        apiGetJson: function (path) {
            return this.getApiClient().ajax({
                type: 'GET',
                url: this.getApiClient().getUrl(path),
                dataType: 'json'
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

        deleteJson: function (path) {
            return this.getApiClient().ajax({
                type: 'DELETE',
                url: this.getApiClient().getUrl(path)
            });
        },

        getApiClient: function () {
            return window.ApiClient;
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
            return String(value || '')
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#39;');
        }
    };

    window.PersonalRatingsTagManagePage = TagManagePage;
})();

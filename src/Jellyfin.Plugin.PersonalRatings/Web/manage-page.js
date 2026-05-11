(function () {
    'use strict';

    if (window.PersonalRatingsManagePage) {
        return;
    }

    /**
     * Handles shared tag-definition loading, filter chips and batch add/remove calls
     * for the management page without expanding the main page controller further.
     */
    var ManagePageTagHelper = {
        loadAvailableTags: function (page, forceReload) {
            var state = ManagePage.getState(page);
            var request;
            if (state.isTagLoading) {
                return state.tagLoadingPromise || Promise.resolve(state.availableTags);
            }

            if (state.tagsLoaded && !forceReload) {
                this.renderPanel(page);
                this.renderFilters(page);
                return Promise.resolve(state.availableTags);
            }

            state.isTagLoading = true;
            try {
                request = ManagePage.getJson('Plugins/PersonalRatings/tags');
            } catch (error) {
                state.availableTags = [];
                state.tagsLoaded = true;
                state.selectedBatchTagIds = [];
                state.selectedFilterTagIds = [];
                state.isTagLoading = false;
                this.renderPanel(page);
                this.renderFilters(page);
                return Promise.resolve(state.availableTags);
            }

            state.tagLoadingPromise = request.then(function (result) {
                state.availableTags = Array.isArray(result) ? result : [];
                state.tagsLoaded = true;
                state.selectedBatchTagIds = state.selectedBatchTagIds.filter(function (tagId) {
                    return state.availableTags.some(function (tag) {
                        return tag.Id === tagId;
                    });
                });
                state.selectedFilterTagIds = state.selectedFilterTagIds.filter(function (tagId) {
                    return state.availableTags.some(function (tag) {
                        return tag.Id === tagId;
                    });
                });
                ManagePageTagHelper.renderPanel(page);
                ManagePageTagHelper.renderFilters(page);
                return state.availableTags;
            }).catch(function () {
                state.availableTags = [];
                state.tagsLoaded = true;
                state.selectedBatchTagIds = [];
                state.selectedFilterTagIds = [];
                ManagePageTagHelper.renderPanel(page);
                ManagePageTagHelper.renderFilters(page);
                return state.availableTags;
            }).finally(function () {
                state.isTagLoading = false;
                state.tagLoadingPromise = null;
            });

            return state.tagLoadingPromise;
        },

        togglePanel: function (page) {
            var state = ManagePage.getState(page);
            state.isTagPanelOpen = !state.isTagPanelOpen;
            this.renderPanel(page);

            if (state.isTagPanelOpen) {
                this.loadAvailableTags(page, false);
            }
        },

        toggleTagSelection: function (page, tagId) {
            if (!tagId || Number.isNaN(tagId)) {
                return;
            }

            var state = ManagePage.getState(page);
            var index = state.selectedBatchTagIds.indexOf(tagId);
            if (index >= 0) {
                state.selectedBatchTagIds.splice(index, 1);
            } else {
                state.selectedBatchTagIds.push(tagId);
            }

            this.renderPanel(page);
        },

        toggleFilterTagSelection: function (page, tagId) {
            if (!tagId || Number.isNaN(tagId)) {
                return;
            }

            var state = ManagePage.getState(page);
            var index = state.selectedFilterTagIds.indexOf(tagId);
            if (index >= 0) {
                state.selectedFilterTagIds.splice(index, 1);
            } else {
                state.selectedFilterTagIds.push(tagId);
            }

            state.pageNumber = 1;
            this.renderFilters(page);
            ManagePage.safeLoad(page);
        },

        clearFilterTags: function (page) {
            var state = ManagePage.getState(page);
            if (!state.selectedFilterTagIds.length) {
                return;
            }

            state.selectedFilterTagIds = [];
            state.pageNumber = 1;
            this.renderFilters(page);
            ManagePage.safeLoad(page);
        },

        applyFilterTagMatchMode: function (page, value) {
            var state = ManagePage.getState(page);
            state.tagMatchMode = value === 'all' ? 'all' : 'any';
            state.pageNumber = 1;
            this.renderFilters(page);
            ManagePage.safeLoad(page);
        },

        runBatchUpdate: function (page, operation) {
            var selectedItemIds = ManagePage.getSelectedItemIds(page);
            var selectedTagIds = this.getSelectedTagIds(page);
            if (selectedItemIds.length === 0) {
                ManagePage.setStatus(page, '请先在当前页勾选至少一个条目，再执行批量标签操作。', 'error');
                return;
            }

            if (selectedTagIds.length === 0) {
                ManagePage.setStatus(page, '请先选择至少一个要操作的标签。', 'error');
                return;
            }

            var path = operation === 'add'
                ? 'Plugins/PersonalRatings/ratings/batch/add-tags'
                : 'Plugins/PersonalRatings/ratings/batch/remove-tags';
            var selectedTagNames = this.getSelectedTagNames(page).join(' / ');
            var request;
            ManagePage.setStatus(page, operation === 'add' ? '正在批量添加标签...' : '正在批量移除标签...', 'loading');

            try {
                request = ManagePage.postJson(path, {
                    itemIds: selectedItemIds,
                    tagIds: selectedTagIds
                });
            } catch (error) {
                ManagePage.handleRequestError(
                    page,
                    error,
                    operation === 'add'
                        ? '批量添加标签失败。请确认标签仍然可用，且当前条目仍可访问。'
                        : '批量移除标签失败。请确认标签仍然可用，且当前条目仍可访问。');
                return;
            }

            request.then(function (result) {
                var affectedCount = result && typeof result.AffectedCount === 'number' ? result.AffectedCount : 0;
                var verb = operation === 'add' ? '添加' : '移除';
                ManagePage.setStatus(page, '已为 ' + affectedCount + ' 条记录批量' + verb + '标签：' + selectedTagNames + '。', 'success');
                ManagePage.getState(page).selectedItemIds = {};
                ManagePage.safeLoad(page);
                ManagePageTagHelper.renderPanel(page);
            }).catch(function (error) {
                ManagePage.handleRequestError(
                    page,
                    error,
                    operation === 'add'
                        ? '批量添加标签失败。请确认标签仍然可用，且当前条目仍可访问。'
                        : '批量移除标签失败。请确认标签仍然可用，且当前条目仍可访问。');
            });
        },

        renderPanel: function (page) {
            var state = ManagePage.getState(page);
            var panel = page.querySelector('.personalRatingsBatchTagPanel');
            var list = page.querySelector('.personalRatingsBatchTagList');
            var empty = page.querySelector('.personalRatingsBatchTagEmpty');
            var targetText = page.querySelector('.personalRatingsBatchTagTargetText');
            var selectionText = page.querySelector('.personalRatingsBatchTagSelectionText');
            var selectedItemCount = ManagePage.getSelectedItemIds(page).length;
            var selectedTagIds = state.selectedBatchTagIds || [];
            var selectedTagNames = this.getSelectedTagNames(page);
            var toggleButton = page.querySelector('.personalRatingsToggleTagPanelButton');

            panel.hidden = !state.isTagPanelOpen;
            if (toggleButton) {
                toggleButton.classList.toggle('is-active', state.isTagPanelOpen);
                toggleButton.textContent = state.isTagPanelOpen ? '收起标签' : '批量标签';
            }

            if (!state.isTagPanelOpen) {
                return;
            }

            if (state.isTagLoading && !state.tagsLoaded) {
                list.innerHTML = '<span class="personalRatingsTag">正在加载标签...</span>';
                empty.hidden = true;
            } else if (!state.availableTags.length) {
                list.innerHTML = '';
                empty.hidden = false;
            } else {
                empty.hidden = true;
                list.innerHTML = state.availableTags.map(function (tag) {
                    var isActive = selectedTagIds.indexOf(tag.Id) >= 0;
                    return '<button is="emby-button" type="button" class="button-flat personalRatingsTag personalRatingsChipButton personalRatingsBatchTagButton'
                        + (isActive ? ' is-active' : '')
                        + '" data-batch-tag-id="' + tag.Id + '" style="' + ManagePage.buildTagToneStyle(tag.Color || '#d88b2f', 0.14, 0.3) + '">'
                        + ManagePage.escapeHtml(tag.Name)
                        + '</button>';
                }).join('');
            }

            if (selectedItemCount > 0) {
                targetText.textContent = '当前已选 ' + selectedItemCount + ' 个条目，可直接批量处理标签。';
            } else {
                targetText.textContent = '当前还没有选中条目。请先在列表里勾选后再处理标签。';
            }

            if (!state.availableTags.length) {
                selectionText.textContent = '当前没有可操作标签。先到标签管理页创建并启用标签。';
            } else if (selectedTagNames.length > 0) {
                selectionText.textContent = '将要操作的标签：' + selectedTagNames.join(' / ');
            } else {
                selectionText.textContent = '当前未选择标签。请先点选下方标签，再执行添加或移除。';
            }

            page.querySelectorAll('.personalRatingsBatchTagApplyButton').forEach(function (button) {
                var disabledReason = '';
                if (selectedItemCount === 0) {
                    disabledReason = '请先选择条目';
                } else if (state.availableTags.length === 0) {
                    disabledReason = '当前没有可用标签';
                } else if (selectedTagIds.length === 0) {
                    disabledReason = '请先选择标签';
                }

                button.disabled = disabledReason.length > 0;
                button.title = disabledReason;
            });
        },

        renderFilters: function (page) {
            var state = ManagePage.getState(page);
            var list = page.querySelector('.personalRatingsFilterTagList');
            var empty = page.querySelector('.personalRatingsTagFilterEmpty');
            var summary = page.querySelector('.personalRatingsTagFilterSummaryText');
            var stateList = page.querySelector('.personalRatingsFilterStateList');
            var matchField = page.querySelector('.personalRatingsFilterTagMatchField');
            var selectedTagIds = state.selectedFilterTagIds || [];
            var selectedTagNames = this.getSelectedFilterTagNames(page);
            var otherActiveFilters = ManagePage.getOtherActiveFilterLabels(state);
            var clearButton = page.querySelector('.personalRatingsClearTagFiltersButton');

            if (state.isTagLoading && !state.tagsLoaded) {
                list.innerHTML = '<span class="personalRatingsTag">正在加载标签...</span>';
                stateList.innerHTML = '';
                empty.hidden = true;
            } else if (!state.availableTags.length) {
                list.innerHTML = '';
                stateList.innerHTML = otherActiveFilters.map(function (text) {
                    return '<span class="personalRatingsTag personalRatingsFilterStateChip">' + ManagePage.escapeHtml(text) + '</span>';
                }).join('');
                empty.hidden = false;
            } else {
                empty.hidden = true;
                list.innerHTML = state.availableTags.map(function (tag) {
                    var isActive = selectedTagIds.indexOf(tag.Id) >= 0;
                    return '<button is="emby-button" type="button" class="button-flat personalRatingsTag personalRatingsChipButton personalRatingsFilterTagButton'
                        + (isActive ? ' is-active' : '')
                        + '" data-filter-tag-id="' + tag.Id + '" style="' + ManagePage.buildTagToneStyle(tag.Color || '#d88b2f', 0.14, 0.3) + '">'
                        + ManagePage.escapeHtml(tag.Name)
                        + '</button>';
                }).join('');

                stateList.innerHTML = ManagePage.buildActiveFilterBadges(selectedTagNames, state.tagMatchMode, otherActiveFilters);
            }

            if (state.isTagLoading && !state.tagsLoaded) {
                summary.textContent = '正在加载可用标签...';
            } else if (!state.availableTags.length) {
                summary.textContent = '当前还没有启用标签。请先到标签管理页创建并启用后再筛选。';
            } else if (selectedTagNames.length > 0) {
                summary.textContent = '已启用 ' + selectedTagNames.length + ' 个标签筛选'
                    + (otherActiveFilters.length > 0 ? '，同时还有 ' + otherActiveFilters.length + ' 项其它筛选条件。' : '。');
            } else if (otherActiveFilters.length > 0) {
                summary.textContent = '当前未启用标签筛选，另有 ' + otherActiveFilters.length + ' 项其它筛选条件正在生效。';
            } else {
                summary.textContent = '可直接点击标签缩小列表范围。';
            }

            matchField.hidden = selectedTagIds.length <= 1;
            page.querySelector('.selectFilterTagMatch').value = state.tagMatchMode || 'any';
            clearButton.disabled = selectedTagIds.length === 0;
            clearButton.title = selectedTagIds.length === 0 ? '当前没有标签筛选可清空' : '';
            clearButton.querySelector('span').textContent = selectedTagIds.length > 0
                ? '清空标签筛选（' + selectedTagIds.length + '）'
                : '清空标签筛选';
        },

        renderAssignedTags: function (tags) {
            var safeTags = Array.isArray(tags) ? tags : [];
            if (!safeTags.length) {
                return '';
            }

            var visibleTags = safeTags.slice(0, 3);
            var overflowTagCount = Math.max(0, safeTags.length - visibleTags.length);
            var renderedTags = visibleTags.map(function (tag) {
                return '<span class="personalRatingsTag personalRatingsDisplayTag" style="' + ManagePage.buildTagToneStyle(tag.Color || '#d88b2f', 0.14, 0.26) + '">'
                    + ManagePage.escapeHtml(tag.Name)
                    + '</span>';
            });

            if (overflowTagCount > 0) {
                renderedTags.push('<span class="personalRatingsTag">+' + overflowTagCount + '</span>');
            }

            return '<div class="personalRatingsTagList personalRatingsAssignedTagList">' + renderedTags.join('') + '</div>';
        },

        getSelectedTagIds: function (page) {
            return ManagePage.getState(page).selectedBatchTagIds || [];
        },

        getSelectedTagNames: function (page) {
            var state = ManagePage.getState(page);
            return state.availableTags.filter(function (tag) {
                return state.selectedBatchTagIds.indexOf(tag.Id) >= 0;
            }).map(function (tag) {
                return tag.Name;
            });
        },

        getSelectedFilterTagNames: function (page) {
            var state = ManagePage.getState(page);
            return state.availableTags.filter(function (tag) {
                return state.selectedFilterTagIds.indexOf(tag.Id) >= 0;
            }).map(function (tag) {
                return tag.Name;
            });
        }
    };

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
                features: {
                    deleteEnabled: true,
                    manageEnabled: true
                },
                isAdministrator: false,
                adminContextLoaded: false,
                selectedItemIds: {},
                selectedBatchTagIds: [],
                selectedFilterTagIds: [],
                availableTags: [],
                tagsLoaded: false,
                isTagLoading: false,
                tagLoadingPromise: null,
                isTagPanelOpen: false,
                tagMatchMode: 'any',
                lastResult: null,
                isLoading: false,
                requestVersion: 0
            };

            this.bindEvents(page);
            this.loadUserContext(page);
            this.loadFeatureState(page).finally(function () {
                ManagePage.renderAdminControls(page);
                ManagePageTagHelper.loadAvailableTags(page, false);
                ManagePage.safeLoad(page);
            });

            page.addEventListener('pageshow', function () {
                ManagePageTagHelper.loadAvailableTags(page, true);
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

                if (button.classList.contains('personalRatingsToggleTagPanelButton')) {
                    event.preventDefault();
                    ManagePageTagHelper.togglePanel(page);
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

                if (button.hasAttribute('data-tag-batch-action')) {
                    event.preventDefault();
                    ManagePageTagHelper.runBatchUpdate(page, button.getAttribute('data-tag-batch-action'));
                    return;
                }

                if (button.hasAttribute('data-batch-tag-id')) {
                    event.preventDefault();
                    ManagePageTagHelper.toggleTagSelection(page, parseInt(button.getAttribute('data-batch-tag-id'), 10));
                    return;
                }

                if (button.hasAttribute('data-filter-tag-id')) {
                    event.preventDefault();
                    ManagePageTagHelper.toggleFilterTagSelection(page, parseInt(button.getAttribute('data-filter-tag-id'), 10));
                    return;
                }

                if (button.classList.contains('personalRatingsClearTagFiltersButton')) {
                    event.preventDefault();
                    ManagePageTagHelper.clearFilterTags(page);
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
                    return;
                }

                if (target.classList.contains('selectFilterTagMatch')) {
                    ManagePageTagHelper.applyFilterTagMatchMode(page, target.value || 'any');
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
            var successMessage = '批量操作完成。';

            if (action === 'setScore') {
                path = 'Plugins/PersonalRatings/ratings/batch/set-score';
                payload.score = parseInt(value, 10);
                successMessage = '批量评分已保存。';
            } else if (action === 'clear') {
                path = 'Plugins/PersonalRatings/ratings/batch/clear-score';
                successMessage = '批量清分已完成。';
            } else if (action === 'pendingOn') {
                path = 'Plugins/PersonalRatings/ratings/batch/set-pending-delete';
                successMessage = '待删除标记已批量更新。';
            } else if (action === 'pendingOff') {
                path = 'Plugins/PersonalRatings/ratings/batch/unset-pending-delete';
                successMessage = '待删除标记已批量取消。';
            } else if (action === 'deletePhysical') {
                if (!this.getState(page).isAdministrator) {
                    this.setStatus(page, '只有管理员可以执行物理删除。', 'error');
                    return;
                }

                if (!window.confirm('物理删除会直接删除 Jellyfin 条目及其底层文件位置，且会写入审计日志。确定继续吗？')) {
                    this.setStatus(page, '已取消物理删除。', 'success');
                    return;
                }

                path = 'Plugins/PersonalRatings/ratings/batch/delete-physical';
                payload.confirmDelete = true;
            }

            if (!path) {
                return;
            }

            this.setStatus(page, '正在提交批量操作...', 'loading');

            try {
                this.postJson(path, payload).then(function (result) {
                    if (action === 'deletePhysical') {
                        var deletedCount = result && typeof result.DeletedCount === 'number' ? result.DeletedCount : 0;
                        var failedCount = result && typeof result.FailedCount === 'number' ? result.FailedCount : 0;
                        var attentionCount = result && typeof result.AttentionCount === 'number' ? result.AttentionCount : 0;
                        var statusMessage = '物理删除已执行，成功 ' + deletedCount + ' 条，失败 ' + failedCount + ' 条。';
                        var attentionItem = ManagePage.getFirstPhysicalDeleteAttentionItem(result);
                        if (attentionItem) {
                            statusMessage += ' ' + ManagePage.buildPhysicalDeleteAttentionMessage(attentionItem);
                        }

                        ManagePage.setStatus(page, statusMessage, failedCount > 0 || attentionCount > 0 ? 'error' : 'success');
                    } else {
                        var affectedCount = result && typeof result.AffectedCount === 'number' ? result.AffectedCount : 0;
                        ManagePage.setStatus(page, successMessage + ' 已影响 ' + affectedCount + ' 条记录。', 'success');
                    }

                    ManagePage.getState(page).selectedItemIds = {};
                    ManagePage.safeLoad(page);
                }).catch(function (error) {
                    if (action === 'deletePhysical' && error && error.status === 409) {
                        ManagePage.setStatus(page, '物理删除功能当前已被插件配置禁用。', 'error');
                        return;
                    }

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
            var state = this.getState(page);
            if (!state.features.manageEnabled) {
                this.renderFeatureDisabled(page, '“我的评分库”功能当前已被插件配置禁用。');
                return;
            }

            try {
                this.load(page);
            } catch (error) {
                this.handleRequestError(page, error, '当前页面未取得 Jellyfin Web 的登录上下文。');
            }
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
                var state = ManagePage.getState(page);
                state.features.deleteEnabled = !!(result && result.IsDeleteFeatureEnabled);
                state.features.manageEnabled = !!(result && result.IsManagePageEnabled);
                ManagePage.renderAdminControls(page);
            }).catch(function () {
                var state = ManagePage.getState(page);
                state.features.deleteEnabled = true;
                state.features.manageEnabled = true;
                ManagePage.renderAdminControls(page);
            });
        },

        loadUserContext: function (page) {
            var state = this.getState(page);

            try {
                this.getApiClient().getCurrentUser().then(function (user) {
                    state.isAdministrator = !!(user && user.Policy && user.Policy.IsAdministrator);
                    state.adminContextLoaded = true;
                    ManagePage.renderAdminControls(page);
                }).catch(function () {
                    state.isAdministrator = false;
                    state.adminContextLoaded = true;
                    ManagePage.renderAdminControls(page);
                });
            } catch (error) {
                state.isAdministrator = false;
                state.adminContextLoaded = true;
                ManagePage.renderAdminControls(page);
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

            this.renderAdminControls(page);
            page.querySelectorAll('.personalRatingsPresetButton').forEach(function (button) {
                button.classList.toggle('is-active', button.getAttribute('data-preset') === state.preset);
            });

            if (items.length === 0) {
                rowsContainer.innerHTML = '<tr><td colspan="7" class="personalRatingsEmptyState">' + ManagePage.escapeHtml(ManagePage.buildEmptyStateMessage(state)) + '</td></tr>';
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
                        + '<td class="personalRatingsCheckboxColumn">'
                        + '<label class="personalRatingsCheckboxControl" title="选择 ' + itemName + '">'
                        + '<input type="checkbox" class="personalRatingsRowCheckbox" data-item-id="' + ManagePage.escapeHtml(item.ItemId) + '"' + (isSelected ? ' checked="checked"' : '') + ' />'
                        + '<span class="personalRatingsCheckboxMark" aria-hidden="true"></span>'
                        + '<span class="personalRatingsVisuallyHidden">选择 ' + itemName + '</span>'
                        + '</label>'
                        + '</td>'
                        + '<td>'
                        + '<a class="personalRatingsItemName" href="' + detailsUrl + '">' + itemName + '</a>'
                        + '<div class="personalRatingsItemMeta">' + itemType + yearText + '</div>'
                        + ManagePageTagHelper.renderAssignedTags(item.Tags)
                        + '</td>'
                        + '<td><span class="personalRatingsScoreBadge' + (item.Score > 0 ? ' is-rated' : '') + '">' + scoreText + '</span></td>'
                        + '<td><div class="personalRatingsTagList">' + statusTags.join('') + '</div></td>'
                        + '<td>' + ManagePage.formatDate(item.RatedAt) + '</td>'
                        + '<td>' + ManagePage.formatDate(item.UpdatedAt) + '</td>'
                        + '<td><div class="personalRatingsRowActions">'
                        + '<a is="emby-linkbutton" class="button-flat personalRatingsButton personalRatingsActionButton" href="' + detailsUrl + '">打开详情</a>'
                        + '<button is="emby-button" type="button" class="button-flat personalRatingsButton personalRatingsActionButton" data-item-id="' + ManagePage.escapeHtml(item.ItemId) + '" data-row-pending="' + (!item.IsPendingDelete ? 'true' : 'false') + '">' + (item.IsPendingDelete ? '取消待删除' : '标记待删除') + '</button>'
                        + '</div></td>'
                        + '</tr>';
                }).join('');
            }

            this.renderSelectionState(page);
            this.renderPagination(page, result);
            this.renderSummary(page, result);
            ManagePageTagHelper.renderFilters(page);
            ManagePageTagHelper.renderPanel(page);
        },

        renderSelectionState: function (page) {
            var selectedItemIds = this.getSelectedItemIds(page);
            page.querySelector('.selectedCountText').textContent = '已选 ' + selectedItemIds.length + ' 项';

            var result = this.getState(page).lastResult;
            var items = result && result.Items ? result.Items : [];
            var hasItems = items.length > 0;
            var isAllSelected = hasItems && selectedItemIds.length === items.length;
            var selectAllCheckbox = page.querySelector('.checkSelectAll');
            selectAllCheckbox.checked = isAllSelected;
            selectAllCheckbox.indeterminate = selectedItemIds.length > 0 && !isAllSelected;
            ManagePageTagHelper.renderFilters(page);
            ManagePageTagHelper.renderPanel(page);
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
            if (totalCount === 0) {
                page.querySelector('.personalRatingsSummaryText').textContent = this.buildEmptyStateMessage(this.getState(page));
                return;
            }

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

            if (state.selectedFilterTagIds && state.selectedFilterTagIds.length > 0) {
                request.tagIds = state.selectedFilterTagIds.slice();
                request.tagMatchMode = state.tagMatchMode || 'any';
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

        renderAdminControls: function (page) {
            var state = this.getState(page);
            page.querySelectorAll('.personalRatingsAdminOnly').forEach(function (element) {
                element.hidden = !state.isAdministrator;
            });

            page.querySelectorAll('.personalRatingsDeleteFeatureOnly').forEach(function (element) {
                element.hidden = !state.isAdministrator || !state.features.deleteEnabled;
            });
        },

        getFirstPhysicalDeleteAttentionItem: function (result) {
            if (!result || !result.Items || !result.Items.length) {
                return null;
            }

            return result.Items.find(function (item) {
                return !!(item && (item.SuggestedAction || item.Result !== 'deleted'));
            }) || null;
        },

        buildPhysicalDeleteAttentionMessage: function (item) {
            if (!item) {
                return '';
            }

            var parts = [];
            if (item.Message) {
                parts.push(item.Message);
            }

            if (item.SuggestedAction) {
                parts.push(item.SuggestedAction);
            }

            if (!parts.length) {
                return '';
            }

            return '需关注：' + parts.join(' ');
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
            var detail = this.extractRequestErrorMessage(error);

            if (detail) {
                message += ' ' + detail;
            } else if (error && typeof error.status === 'number') {
                message += ' HTTP ' + error.status + '.';
            } else if (error && error.message) {
                message += ' ' + error.message;
            }

            this.setStatus(page, message, 'error');
        },

        renderFeatureDisabled: function (page, message) {
            page.querySelector('.personalRatingsRows').innerHTML = '<tr><td colspan="7" class="personalRatingsEmptyState">' + this.escapeHtml(message) + '</td></tr>';
            page.querySelector('.personalRatingsSummaryText').textContent = message;
            this.setStatus(page, message, 'error');
            page.querySelector('.personalRatingsPrevPageButton').disabled = true;
            page.querySelector('.personalRatingsNextPageButton').disabled = true;
            ManagePageTagHelper.renderFilters(page);
            ManagePageTagHelper.renderPanel(page);
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

        getJson: function (path) {
            var apiClient = this.getApiClient();
            return apiClient.ajax({
                type: 'GET',
                url: apiClient.getUrl(path),
                dataType: 'json'
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
        },

        hexToTransparent: function (hex, alpha) {
            var value = String(hex || '').replace('#', '');
            if (value.length === 3) {
                value = value.charAt(0) + value.charAt(0)
                    + value.charAt(1) + value.charAt(1)
                    + value.charAt(2) + value.charAt(2);
            }

            if (value.length !== 6) {
                return 'rgba(216, 139, 47, ' + alpha + ')';
            }

            var red = parseInt(value.substring(0, 2), 16);
            var green = parseInt(value.substring(2, 4), 16);
            var blue = parseInt(value.substring(4, 6), 16);
            return 'rgba(' + red + ', ' + green + ', ' + blue + ', ' + alpha + ')';
        },

        buildTagToneStyle: function (hex, backgroundAlpha, borderAlpha) {
            var color = this.escapeHtml(hex || '#d88b2f');
            return '--pr-tag-accent:' + color + ';'
                + '--pr-tag-text:' + color + ';'
                + '--pr-tag-bg:' + this.hexToTransparent(hex || '#d88b2f', backgroundAlpha || 0.14) + ';'
                + '--pr-tag-border:' + this.hexToTransparent(hex || '#d88b2f', borderAlpha || 0.28) + ';';
        },

        buildActiveFilterBadges: function (selectedTagNames, tagMatchMode, otherActiveFilters) {
            var badges = [];
            selectedTagNames.forEach(function (tagName) {
                badges.push('标签：' + tagName);
            });

            if (selectedTagNames.length > 1) {
                badges.push('标签匹配：' + (tagMatchMode === 'all' ? '全部命中' : '命中任一'));
            }

            otherActiveFilters.forEach(function (text) {
                badges.push(text);
            });

            return badges.map(function (text) {
                return '<span class="personalRatingsTag personalRatingsFilterStateChip">' + ManagePage.escapeHtml(text) + '</span>';
            }).join('');
        },

        getOtherActiveFilterLabels: function (state) {
            var labels = [];
            if (state.keyword) {
                labels.push('关键词：' + state.keyword);
            }

            if (state.preset && state.preset !== 'ratedAll') {
                labels.push('视图：' + this.getPresetLabel(state.preset));
            }

            return labels;
        },

        getPresetLabel: function (preset) {
            switch (preset) {
                case 'score5':
                    return '5分';
                case 'score4':
                    return '4分';
                case 'score3':
                    return '3分';
                case 'score2':
                    return '2分';
                case 'score1':
                    return '1分';
                case 'unrated':
                    return '未评分';
                case 'pendingDelete':
                    return '待删除';
                case 'recent':
                    return '最近评分';
                case 'playedUnrated':
                    return '已播放未评分';
                case 'ratedAll':
                default:
                    return '全部已评分';
            }
        },

        hasActiveFilters: function (state) {
            return !!(state.keyword
                || (state.selectedFilterTagIds && state.selectedFilterTagIds.length > 0)
                || (state.preset && state.preset !== 'ratedAll'));
        },

        buildEmptyStateMessage: function (state) {
            if (this.hasActiveFilters(state)) {
                return '当前筛选条件没有命中记录。可以清空标签筛选、调整关键词或切换预设后重试。';
            }

            return '当前还没有评分记录。先在前台“打分库”或详情页打分后，这里会显示条目。';
        },

        extractRequestErrorMessage: function (error) {
            if (!error) {
                return '';
            }

            if (error.responseJSON) {
                if (typeof error.responseJSON === 'string') {
                    return error.responseJSON;
                }

                if (error.responseJSON.detail) {
                    return error.responseJSON.detail;
                }

                if (error.responseJSON.title) {
                    return error.responseJSON.title;
                }

                if (error.responseJSON.errors) {
                    var firstKey = Object.keys(error.responseJSON.errors)[0];
                    if (firstKey && Array.isArray(error.responseJSON.errors[firstKey]) && error.responseJSON.errors[firstKey].length > 0) {
                        return error.responseJSON.errors[firstKey][0];
                    }
                }
            }

            if (typeof error.responseText === 'string' && error.responseText.trim().length > 0) {
                return error.responseText.replace(/^"+|"+$/g, '').trim();
            }

            return '';
        }
    };

    window.PersonalRatingsManagePage = ManagePage;
})();

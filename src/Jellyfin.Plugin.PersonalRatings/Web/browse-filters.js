(function () {
    'use strict';

    if (window.PersonalRatingsBrowseFilters) {
        return;
    }

    /**
     * Owns browse-page toolbar wiring, tag-filter UI and header action syncing.
     * Network calls are delegated back to shell callbacks.
     */
    window.PersonalRatingsBrowseFilters = {
        bindPageEvents: function (page, state, handlers) {
            page.addEventListener('click', function (event) {
                var target = event.target;
                if (!target) {
                    return;
                }

                var button = target.closest('button, a');
                if (!button) {
                    return;
                }

                if (button.classList.contains('personalRatingsBrowsePrevButton')) {
                    event.preventDefault();
                    handlers.onChangePage(-1);
                    return;
                }

                if (button.classList.contains('personalRatingsBrowseNextButton')) {
                    event.preventDefault();
                    handlers.onChangePage(1);
                    return;
                }

                if (button.classList.contains('personalRatingsBrowseClearButton')) {
                    event.preventDefault();
                    handlers.onClearSearch();
                    return;
                }

                if (button.classList.contains('personalRatingsBrowseClearFiltersButton')) {
                    event.preventDefault();
                    handlers.onClearFilters();
                    return;
                }

                if (button.classList.contains('personalRatingsBrowseSheetCloseButton')) {
                    event.preventDefault();
                    handlers.onClosePanel();
                    return;
                }

                if (button.classList.contains('personalRatingsBrowseViewButton')) {
                    event.preventDefault();
                    handlers.onViewMode(button.getAttribute('data-view-mode') || 'poster');
                    return;
                }

                if (button.hasAttribute('data-panel-mode')) {
                    event.preventDefault();
                    handlers.onOpenPanel(button.getAttribute('data-panel-mode'));
                    return;
                }

                if (button.classList.contains('personalRatingsBrowseTagChip')) {
                    event.preventDefault();
                    handlers.onToggleTag(parseInt(button.getAttribute('data-tag-id'), 10));
                    return;
                }

                if (button.classList.contains('personalRatingsOpenBackendButton')) {
                    event.preventDefault();
                    handlers.onOpenBackend();
                    return;
                }

                if (button.classList.contains('personalRatingsOpenAuditButton')) {
                    event.preventDefault();
                    handlers.onOpenAudit();
                }
            });

            page.addEventListener('change', function (event) {
                var target = event.target;
                if (!target) {
                    return;
                }

                if (target.classList.contains('selectBrowseScore')) {
                    handlers.onScoreFilter(target.value);
                    return;
                }

                if (target.classList.contains('selectBrowsePlayed')) {
                    handlers.onPlayedFilter(target.value);
                    return;
                }

                if (target.classList.contains('selectBrowseType')) {
                    handlers.onMediaType(target.value);
                    return;
                }

                if (target.classList.contains('selectBrowseSort')) {
                    handlers.onSort(target.value);
                    return;
                }

                if (target.classList.contains('selectBrowseTagMatch')) {
                    handlers.onTagMatchMode(target.value || 'any');
                }
            });

            page.querySelector('.personalRatingsBrowseSearchForm').addEventListener('submit', function (event) {
                event.preventDefault();
                handlers.onSearch(page.querySelector('.txtBrowseSearch').value.trim());
            });
        },

        renderTagFilters: function (page, state) {
            var container = page.querySelector('.personalRatingsBrowseTagFilters');
            var matchField = page.querySelector('.personalRatingsBrowseTagMatchField');

            if (!state.tags.length) {
                container.innerHTML = '<div class="personalRatingsEmptyTag">标签筛选已预留，当前还没有可用标签。</div>';
                matchField.hidden = true;
                return;
            }

            container.innerHTML = state.tags.map(function (tag) {
                var isActive = state.tagIds.indexOf(tag.Id) >= 0;
                var color = window.PersonalRatingsBrowseRenderer.escapeHtml(tag.Color || '#d88b2f');
                var style = 'border-color:' + color + ';';
                if (isActive) {
                    style += ' background:' + window.PersonalRatingsBrowseRenderer.hexToTransparent(tag.Color || '#d88b2f', 0.18) + ';';
                }

                return ''
                    + '<button type="button" class="button-flat personalRatingsBrowseTagChip'
                    + (isActive ? ' is-active' : '')
                    + '" data-tag-id="' + tag.Id + '" style="' + style + '">'
                    + window.PersonalRatingsBrowseRenderer.escapeHtml(tag.Name)
                    + '</button>';
            }).join('');

            matchField.hidden = state.tagIds.length <= 1;
            page.querySelector('.selectBrowseTagMatch').value = state.tagMatchMode;
        },

        renderToolbarState: function (page, state) {
            page.querySelectorAll('.personalRatingsBrowseViewButton').forEach(function (button) {
                var isActive = button.getAttribute('data-view-mode') === state.viewMode;
                button.classList.toggle('is-active', isActive);
                button.setAttribute('aria-pressed', isActive ? 'true' : 'false');
            });

            page.querySelectorAll('.personalRatingsBrowseModeButton').forEach(function (button) {
                var isActive = button.getAttribute('data-panel-mode') === (state.activePanelMode || '');
                button.classList.toggle('is-active', isActive);
                button.setAttribute('aria-pressed', isActive ? 'true' : 'false');
            });

            page.querySelector('.txtBrowseSearch').value = state.search || '';
            page.querySelector('.selectBrowseScore').value = state.scoreFilter || 'rated';
            page.querySelector('.selectBrowsePlayed').value = state.playedFilter || 'all';
            page.querySelector('.selectBrowseType').value = state.mediaType || 'all';
            page.querySelector('.selectBrowseSort').value = state.sortValue || 'ratedAt:desc';
        },

        renderFilterTray: function (page, state) {
            var tray = page.querySelector('.personalRatingsBrowseFilterTray');
            var title = page.querySelector('.personalRatingsBrowseFilterSheetTitle');
            var summary = page.querySelector('.personalRatingsBrowseFilterSheetSummary');
            var isOpen = !!state.activePanelMode;
            var isSearchMode = state.activePanelMode === 'search';
            var isSortMode = state.activePanelMode === 'sort';
            var isFilterMode = state.activePanelMode === 'filter';

            tray.hidden = !isOpen;

            if (!isOpen) {
                return;
            }

            this.toggleSection(page, '.personalRatingsBrowsePanelSection-search', isSearchMode);
            this.toggleSection(page, '.personalRatingsBrowsePanelSection-sort', isSortMode);
            this.toggleSection(page, '.personalRatingsBrowsePanelSection-filterGrid', isFilterMode);
            this.toggleSection(page, '.personalRatingsBrowsePanelSection-filterTags', isFilterMode);
            this.toggleSection(page, '.personalRatingsBrowsePanelSection-filterActions', isFilterMode);

            if (isSearchMode) {
                title.textContent = '搜索';
                summary.textContent = state.search
                    ? '正在按关键词 “' + state.search + '” 搜索。'
                    : '输入片名、剧名或条目名后再查询。';
            } else if (isSortMode) {
                title.textContent = '排序与视图';
                summary.textContent = state.viewMode === 'list'
                    ? '当前为列表视图，可在这里调整排序。'
                    : '当前为海报视图，可在这里调整排序。';
            } else {
                title.textContent = '筛选';
                summary.textContent = this.buildFilterSummary(state);
            }
        },

        renderActiveFilters: function (page, state) {
            var container = page.querySelector('.personalRatingsBrowseActiveFilters');
            var chips = [];
            var tagNames = state.tags.filter(function (tag) {
                return state.tagIds.indexOf(tag.Id) >= 0;
            }).map(function (tag) {
                return tag.Name;
            });

            if (state.scoreFilter !== 'rated') {
                if (state.scoreFilter === 'all') {
                    chips.push('全部交互');
                } else if (state.scoreFilter === 'unrated') {
                    chips.push('未评分');
                } else {
                    chips.push(state.scoreFilter + '分');
                }
            }

            if (state.playedFilter === 'played') {
                chips.push('已播放');
            } else if (state.playedFilter === 'unplayed') {
                chips.push('未播放');
            }

            if (state.mediaType !== 'all') {
                chips.push(state.mediaType);
            }

            if (state.search) {
                chips.push('搜索：' + state.search);
            }

            if (tagNames.length) {
                chips.push((state.tagMatchMode === 'all' ? '全部标签' : '任一标签') + '：' + tagNames.join(' / '));
            }

            if (state.sortValue !== 'ratedAt:desc') {
                chips.push('排序已修改');
            }

            if (!chips.length) {
                chips.push('当前显示全部已评分条目');
            }

            container.innerHTML = chips.map(function (text) {
                return '<span class="personalRatingsBrowseActiveChip">' + window.PersonalRatingsBrowseRenderer.escapeHtml(text) + '</span>';
            }).join('');
        },

        syncHeaderActions: function (page, state) {
            var auditButton = page.querySelector('.personalRatingsOpenAuditButton');
            if (auditButton) {
                auditButton.hidden = !state.isAdministrator;
            }

            this.renderToolbarState(page, state);
            this.renderFilterTray(page, state);
            this.renderActiveFilters(page, state);
        },

        buildFilterSummary: function (state) {
            var parts = [];
            if (state.scoreFilter === 'rated') {
                parts.push('全部已评分');
            } else if (state.scoreFilter === 'all') {
                parts.push('全部交互');
            } else if (state.scoreFilter === 'unrated') {
                parts.push('未评分');
            } else {
                parts.push(state.scoreFilter + '分');
            }

            if (state.playedFilter === 'played') {
                parts.push('已播放');
            } else if (state.playedFilter === 'unplayed') {
                parts.push('未播放');
            }

            if (state.mediaType !== 'all') {
                parts.push(state.mediaType);
            }

            if (state.tagIds.length) {
                parts.push(state.tagMatchMode === 'all' ? '全部命中标签' : '任意命中标签');
            }

            return parts.join(' · ') || '可按评分、标签、播放状态和类型缩小结果范围。';
        },

        toggleSection: function (page, selector, isVisible) {
            page.querySelectorAll(selector).forEach(function (element) {
                element.hidden = !isVisible;
            });
        }
    };
})();

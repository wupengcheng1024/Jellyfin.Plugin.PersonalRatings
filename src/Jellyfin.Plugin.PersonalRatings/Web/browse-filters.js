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

                if (button.classList.contains('personalRatingsBrowseViewButton')) {
                    event.preventDefault();
                    handlers.onViewMode(button.getAttribute('data-view-mode') || 'poster');
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

        syncHeaderActions: function (page, state) {
            var auditButton = page.querySelector('.personalRatingsOpenAuditButton');
            if (auditButton) {
                auditButton.hidden = !state.isAdministrator;
            }

            page.querySelectorAll('.personalRatingsBrowseViewButton').forEach(function (button) {
                button.classList.toggle('is-active', button.getAttribute('data-view-mode') === state.viewMode);
            });
        }
    };
})();

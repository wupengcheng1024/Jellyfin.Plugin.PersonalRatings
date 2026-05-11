(function () {
    'use strict';

    if (window.PersonalRatingsBrowseRenderer) {
        return;
    }

    /**
     * Renders browse-page result cards, summary text and lightweight status UI.
     * This module only reads state and writes DOM.
     */
    window.PersonalRatingsBrowseRenderer = {
        renderResults: function (page, state) {
            var result = state.lastResult || {
                Items: [],
                TotalCount: 0,
                PageNumber: state.pageNumber,
                PageSize: state.pageSize
            };
            var resultsNode = page.querySelector('.personalRatingsBrowseResults');
            var cardsNode = page.querySelector('.personalRatingsBrowseCards');
            var items = Array.isArray(result.Items) ? result.Items : [];

            resultsNode.classList.toggle('is-list', state.viewMode === 'list');

            if (!items.length) {
                cardsNode.innerHTML = '<div class="personalRatingsBrowseEmpty">'
                    + this.escapeHtml(state.lastLoadFailed
                        ? '打分库暂时无法加载，请稍后刷新重试。'
                        : (this.hasActiveFilters(state)
                            ? '当前筛选条件下没有条目。'
                            : '当前还没有个人评分记录。'))
                    + '</div>';
            } else {
                cardsNode.innerHTML = items.map(function (item) {
                    return window.PersonalRatingsBrowseRenderer.renderItemCard(item, state.viewMode);
                }).join('');
            }

            this.renderSummary(page, result, state);
            this.renderPagination(page, result, state);
        },

        renderItemCard: function (item, viewMode) {
            var itemId = this.escapeHtml(item.ItemId);
            var itemName = this.escapeHtml(item.ItemName || '未命名条目');
            var imageUrl = window.PersonalRatingsBrowseApi.buildImageUrl(item.ItemId);
            var detailUrl = '#/details?id=' + encodeURIComponent(item.ItemId)
                + '&serverId=' + encodeURIComponent(window.PersonalRatingsBrowseApi.getServerId());
            var metaParts = [];
            var secondaryParts = [];
            var tags = Array.isArray(item.Tags) ? item.Tags : [];
            var visibleTags = tags.slice(0, 2);
            var overflowTagCount = Math.max(0, tags.length - visibleTags.length);
            var listBadges = [];

            if (item.ProductionYear) {
                metaParts.push(item.ProductionYear);
            }

            if (item.ItemType) {
                metaParts.push(item.ItemType);
            } else if (item.MediaType) {
                metaParts.push(item.MediaType);
            }

            if (item.IsPlayed) {
                secondaryParts.push('已播放');
            }

            if (item.RatedAt) {
                secondaryParts.push('最近评分');
            }

            listBadges.push('<span class="personalRatingsScoreBadge">' + this.buildScoreText(item.Score) + '</span>');
            if (item.IsPendingDelete) {
                listBadges.push('<span class="personalRatingsStateBadge personalRatingsStateBadge-subtle">待删除</span>');
            }

            if (viewMode === 'list') {
                return ''
                    + '<a class="personalRatingsCardLink personalRatingsCardLink-list" href="' + detailUrl + '" data-item-id="' + itemId + '">'
                    + '  <article class="personalRatingsListItem listItem">'
                    + '    <div class="personalRatingsListItemImage" style="background-image:url(\'' + imageUrl + '\')"></div>'
                    + '    <div class="personalRatingsListItemBody">'
                    + '      <div class="personalRatingsCardTitleRow">'
                    + '        <h3 class="personalRatingsCardTitle">' + itemName + '</h3>'
                    + '        <div class="personalRatingsListBadges">' + listBadges.join('') + '</div>'
                    + '      </div>'
                    + '      <div class="personalRatingsCardMeta">' + this.escapeHtml(metaParts.join(' · ') || '未标注类型') + '</div>'
                    + '      <div class="personalRatingsCardSubMeta">' + this.escapeHtml(secondaryParts.join(' · ') || '打开详情页继续操作') + '</div>'
                    + '      <div class="personalRatingsCardTags">' + this.renderTagChips(visibleTags, overflowTagCount) + '</div>'
                    + '    </div>'
                    + '  </article>'
                    + '</a>';
            }

            return ''
                + '<a class="personalRatingsCardLink personalRatingsCardLink-poster" href="' + detailUrl + '" data-item-id="' + itemId + '">'
                + '  <article class="card personalRatingsMediaCard card-hoverable card-withuserdata">'
                + '    <div class="cardBox cardBox-bottompadded">'
                + '      <div class="cardScalable">'
                + '        <div class="cardPadder personalRatingsCardPadder"></div>'
                + '        <div class="personalRatingsPoster">'
                + '          <div class="personalRatingsPosterImage" style="background-image:url(\'' + imageUrl + '\')"></div>'
                + '          <div class="personalRatingsPosterOverlay"></div>'
                + '          <div class="personalRatingsPosterBadges">'
                + '            <span class="personalRatingsScoreBadge">' + this.buildScoreText(item.Score) + '</span>'
                + '            <div class="personalRatingsBadgeStack">'
                + (item.IsPendingDelete ? '<span class="personalRatingsStateBadge personalRatingsStateBadge-subtle">待删除</span>' : '')
                + '            </div>'
                + '          </div>'
                + '        </div>'
                + '      </div>'
                + '      <div class="cardText cardText-first personalRatingsCardTextPrimary">' + itemName + '</div>'
                + '      <div class="cardText cardText-secondary personalRatingsCardTextSecondary">' + this.escapeHtml(metaParts.join(' · ') || '未标注类型') + '</div>'
                + '      <div class="personalRatingsCardSubMeta personalRatingsCardSubMeta-poster">' + this.escapeHtml(secondaryParts.join(' · ') || '打开详情页继续操作') + '</div>'
                + '      <div class="personalRatingsCardTags personalRatingsCardTags-poster">' + this.renderTagChips(visibleTags, overflowTagCount) + '</div>'
                + '    </div>'
                + '  </article>'
                + '</a>';
        },

        renderTagChips: function (tags, overflowTagCount) {
            if (!tags.length && !overflowTagCount) {
                return '<span class="personalRatingsEmptyTag">暂无标签</span>';
            }

            var chips = tags.map(function (tag) {
                return '<span class="personalRatingsCardTag" style="' + window.PersonalRatingsBrowseRenderer.buildTagToneStyle(tag.Color || '#d88b2f', 0.14, 0.26) + '">'
                    + window.PersonalRatingsBrowseRenderer.escapeHtml(tag.Name)
                    + '</span>';
            });

            if (overflowTagCount > 0) {
                chips.push('<span class="personalRatingsCardTag personalRatingsCardTag-overflow">+' + overflowTagCount + '</span>');
            }

            return chips.join('');
        },

        renderSummary: function (page, result, state) {
            var totalCount = result.TotalCount || 0;
            var pageNumber = result.PageNumber || 1;
            var pageSize = result.PageSize || state.pageSize || 36;
            var startIndex = totalCount === 0 ? 0 : ((pageNumber - 1) * pageSize) + 1;
            var endIndex = Math.min(totalCount, pageNumber * pageSize);
            var manageModeHint = page.querySelector('.personalRatingsBrowseModeHint');

            if (state.lastLoadFailed) {
                page.querySelector('.personalRatingsBrowseSummaryText').textContent = '打分库暂时无法加载，请稍后刷新重试。';
            } else if (totalCount === 0 && this.hasActiveFilters(state)) {
                page.querySelector('.personalRatingsBrowseSummaryText').textContent = '当前筛选条件没有命中条目。';
            } else if (totalCount === 0) {
                page.querySelector('.personalRatingsBrowseSummaryText').textContent = '当前还没有个人评分记录。';
            } else {
                page.querySelector('.personalRatingsBrowseSummaryText').textContent =
                    '共 ' + totalCount + ' 条，当前显示 ' + startIndex + '-' + endIndex + '。';
            }

            if (manageModeHint) {
                manageModeHint.textContent = state.search || state.tagIds.length || state.playedFilter !== 'all' || state.mediaType !== 'all'
                    ? '当前结果已叠加个人评分、标签与播放状态筛选。'
                    : '这里沿用 Jellyfin 媒体库的浏览方式，仅轻量叠加个人评分维度。';
            }
        },

        renderPagination: function (page, result, state) {
            var totalCount = result.TotalCount || 0;
            var pageSize = result.PageSize || state.pageSize || 36;
            var pageNumber = result.PageNumber || state.pageNumber || 1;
            var totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

            page.querySelector('.personalRatingsBrowsePageText').textContent = '第 ' + pageNumber + ' / ' + totalPages + ' 页';
            page.querySelector('.personalRatingsBrowsePrevButton').disabled = pageNumber <= 1;
            page.querySelector('.personalRatingsBrowseNextButton').disabled = pageNumber >= totalPages;
        },

        setStatus: function (page, message, type) {
            var statusNode = page.querySelector('.personalRatingsBrowseStatusText');
            statusNode.textContent = message;
            statusNode.classList.remove('is-error', 'is-success', 'is-loading');
            if (type) {
                statusNode.classList.add('is-' + type);
            }
        },

        buildScoreText: function (score) {
            return score > 0 ? (score + ' 分') : '未评分';
        },

        hasActiveFilters: function (state) {
            return !!(state.search
                || (Array.isArray(state.tagIds) && state.tagIds.length)
                || state.playedFilter !== 'all'
                || state.mediaType !== 'all'
                || state.scoreFilter !== 'rated'
                || state.sortValue !== 'ratedAt:desc');
        },

        escapeHtml: function (value) {
            return String(value || '')
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
        }
    };
})();

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
                cardsNode.innerHTML = '<div class="personalRatingsBrowseEmpty">当前筛选条件下没有条目。</div>';
            } else {
                cardsNode.innerHTML = items.map(function (item) {
                    return window.PersonalRatingsBrowseRenderer.renderItemCard(item);
                }).join('');
            }

            this.renderSummary(page, result, state);
            this.renderPagination(page, result, state);
        },

        renderItemCard: function (item) {
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

            return ''
                + '<a class="personalRatingsCardLink" href="' + detailUrl + '" data-item-id="' + itemId + '">'
                + '  <article class="personalRatingsCard">'
                + '    <div class="personalRatingsPoster">'
                + '      <div class="personalRatingsPosterImage" style="background-image:url(\'' + imageUrl + '\')"></div>'
                + '      <div class="personalRatingsPosterOverlay"></div>'
                + '      <div class="personalRatingsPosterBadges">'
                + '        <span class="personalRatingsScoreBadge">' + this.buildScoreText(item.Score) + '</span>'
                + '        <div class="personalRatingsBadgeStack">'
                + (item.IsPendingDelete ? '<span class="personalRatingsStateBadge personalRatingsStateBadge-subtle">待删除</span>' : '')
                + '        </div>'
                + '      </div>'
                + '    </div>'
                + '    <div class="personalRatingsCardBody">'
                + '      <h3 class="personalRatingsCardTitle">' + itemName + '</h3>'
                + '      <div class="personalRatingsCardMeta">' + this.escapeHtml(metaParts.join(' · ') || '未标注类型') + '</div>'
                + '      <div class="personalRatingsCardSubMeta">' + this.escapeHtml(secondaryParts.join(' · ') || '打开详情页继续操作') + '</div>'
                + '      <div class="personalRatingsCardTags">' + this.renderTagChips(visibleTags, overflowTagCount) + '</div>'
                + '    </div>'
                + '  </article>'
                + '</a>';
        },

        renderTagChips: function (tags, overflowTagCount) {
            if (!tags.length && !overflowTagCount) {
                return '<span class="personalRatingsEmptyTag">暂无标签</span>';
            }

            var chips = tags.map(function (tag) {
                var background = window.PersonalRatingsBrowseRenderer.hexToTransparent(tag.Color || '#d88b2f', 0.16);
                return '<span class="personalRatingsCardTag" style="background:' + background + '; border-color:' + window.PersonalRatingsBrowseRenderer.escapeHtml(tag.Color || '#d88b2f') + ';">'
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

            page.querySelector('.personalRatingsBrowseSummaryText').textContent =
                '共 ' + totalCount + ' 条，当前显示 ' + startIndex + '-' + endIndex + '。';

            if (manageModeHint) {
                manageModeHint.textContent = '批量操作仍保留在评分后台里，前台页面先专注浏览与快速筛选。';
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
        }
    };
})();

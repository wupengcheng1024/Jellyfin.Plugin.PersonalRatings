(function () {
    'use strict';

    if (window.PersonalRatingsDetailPanel) {
        return;
    }

    var launcherId = 'personalRatingsLauncher';
    var panelClassName = 'personalRatingsDetailPanel';
    var styleId = 'personalRatingsInjectedStyles';

    /**
     * Owns the detail operation area's DOM structure, styles and view rendering.
     * Business rules and requests stay in the bootstrap shell / API module.
     */
    window.PersonalRatingsDetailPanel = {
        launcherId: launcherId,
        panelClassName: panelClassName,

        injectStyles: function () {
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
                '.personalRatingsTagSection {',
                '  margin-top: 16px;',
                '}',
                '.personalRatingsTagSectionHeader {',
                '  font-size: 12px;',
                '  letter-spacing: 0.04em;',
                '  color: rgba(255, 255, 255, 0.68);',
                '}',
                '.personalRatingsTagPicker {',
                '  display: flex;',
                '  flex-wrap: wrap;',
                '  gap: 10px;',
                '  margin-top: 10px;',
                '}',
                '.personalRatingsTagButton.is-active {',
                '  border-color: rgba(216, 139, 47, 0.42);',
                '  color: #fff8ef;',
                '}',
                '.personalRatingsTagHint {',
                '  color: rgba(255, 255, 255, 0.58);',
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
        },

        ensureLauncher: function (onOpenManagePage) {
            var launcher = document.getElementById(launcherId);
            if (!launcher) {
                launcher = document.createElement('button');
                launcher.id = launcherId;
                launcher.type = 'button';
                launcher.className = 'button-flat';
                launcher.textContent = '打分库';
                launcher.addEventListener('click', onOpenManagePage);
                document.body.appendChild(launcher);
            }

            return launcher;
        },

        updateLauncherVisibility: function (managePageEnabled, isBrowsePage) {
            var launcher = document.getElementById(launcherId);
            if (!launcher) {
                return;
            }

            launcher.classList.toggle('is-hidden', isBrowsePage || !managePageEnabled);
        },

        hideLauncher: function () {
            var launcher = document.getElementById(launcherId);
            if (launcher) {
                launcher.classList.add('is-hidden');
            }
        },

        ensureDetailPanel: function (detailsPage, itemId, callbacks) {
            var panel = detailsPage.querySelector('.' + panelClassName);
            var buttonRow = detailsPage.querySelector('.mainDetailButtons');
            if (!buttonRow) {
                return null;
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
                    '  <button type="button" class="button-flat personalRatingsManageButton">打开打分库</button>',
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
                    '<div class="personalRatingsTagSection">',
                    '  <div class="personalRatingsTagSectionHeader">标签</div>',
                    '  <div class="personalRatingsTagPicker">正在读取标签...</div>',
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
                        callbacks.onApplyScore(panel.dataset.itemId, parseInt(scoreButton.getAttribute('data-score'), 10));
                        return;
                    }

                    if (target.closest('.personalRatingsClearButton')) {
                        callbacks.onClearScore(panel.dataset.itemId);
                        return;
                    }

                    if (target.closest('.personalRatingsPendingButton')) {
                        callbacks.onTogglePendingDelete(panel.dataset.itemId, panel.dataset.isPendingDelete === 'true');
                        return;
                    }

                    var tagButton = target.closest('.personalRatingsTagButton');
                    if (tagButton) {
                        callbacks.onToggleTag(panel.dataset.itemId, parseInt(tagButton.getAttribute('data-tag-id'), 10));
                        return;
                    }

                    if (target.closest('.personalRatingsDeleteButton')) {
                        callbacks.onDeletePhysical(panel.dataset.itemId);
                        return;
                    }

                    if (target.closest('.personalRatingsManageButton')) {
                        callbacks.onOpenManagePage();
                    }
                });

                buttonRow.insertAdjacentElement('afterend', panel);
            }

            if (panel.dataset.itemId !== itemId) {
                panel.dataset.itemId = itemId;
                panel.dataset.isPendingDelete = 'false';
                panel.dataset.tagIds = '[]';
                panel._personalRatingsRating = null;
                this.renderSummary(panel, null, '正在读取当前评分...');
                this.renderTagPickerLoading(panel);
                this.syncScoreButtons(panel, 0);
            }

            return panel;
        },

        removeDetailPanel: function () {
            var panel = document.querySelector('.' + panelClassName);
            if (panel) {
                panel.remove();
            }
        },

        getActivePanel: function (itemId) {
            var panel = document.querySelector('.' + panelClassName);
            if (!panel || panel.dataset.itemId !== itemId) {
                return null;
            }

            return panel;
        },

        renderAdminControls: function (panel, isAdministrator, deleteFeatureEnabled, managePageEnabled) {
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
        },

        renderTagPickerLoading: function (panel) {
            var container = panel.querySelector('.personalRatingsTagPicker');
            if (container) {
                container.textContent = '正在读取标签...';
            }
        },

        renderTagPickerError: function (panel) {
            var container = panel.querySelector('.personalRatingsTagPicker');
            if (container) {
                container.textContent = '标签读取失败。';
            }
        },

        renderTagPicker: function (panel, availableTags, selectedTags) {
            var container = panel.querySelector('.personalRatingsTagPicker');
            if (!container) {
                return;
            }

            if (!Array.isArray(availableTags) || availableTags.length === 0) {
                container.innerHTML = '<span class="personalRatingsTagHint">当前还没有可用标签。</span>';
                return;
            }

            var selectedTagIds = {};
            (selectedTags || []).forEach(function (tag) {
                selectedTagIds[tag.Id] = true;
            });

            container.innerHTML = availableTags.map(function (tag) {
                var isActive = !!selectedTagIds[tag.Id];
                var color = window.PersonalRatingsDetailPanel.escapeHtml(tag.Color || '#d88b2f');
                var style = 'border-color:' + color + ';';
                if (isActive) {
                    style += ' background:' + window.PersonalRatingsDetailPanel.hexToTransparent(tag.Color || '#d88b2f', 0.22) + ';';
                }

                return '<button type="button" class="button-flat personalRatingsTagButton'
                    + (isActive ? ' is-active' : '')
                    + '" data-tag-id="' + tag.Id + '" style="' + style + '">'
                    + window.PersonalRatingsDetailPanel.escapeHtml(tag.Name)
                    + '</button>';
            }).join('');
        },

        updatePanelState: function (panel, rating, selectedTags, availableTags) {
            panel._personalRatingsRating = rating;
            panel.dataset.isPendingDelete = rating && rating.IsPendingDelete ? 'true' : 'false';
            panel.dataset.tagIds = JSON.stringify((selectedTags || []).map(function (tag) {
                return tag.Id;
            }));
            this.renderTagPicker(panel, availableTags, selectedTags);
            this.renderSummary(panel, rating, this.buildSummary(rating, selectedTags));
            this.syncScoreButtons(panel, rating && rating.Score ? rating.Score : 0);
        },

        renderSummary: function (panel, result, message) {
            var summaryNode = panel.querySelector('.personalRatingsDetailSummary');
            var pendingButton = panel.querySelector('.personalRatingsPendingButton');

            summaryNode.textContent = message;

            if (result && result.IsPendingDelete) {
                pendingButton.textContent = '取消待删除';
            } else {
                pendingButton.textContent = '标记待删除';
            }
        },

        syncScoreButtons: function (panel, score) {
            panel.querySelectorAll('.personalRatingsScoreButton').forEach(function (button) {
                var buttonScore = parseInt(button.getAttribute('data-score'), 10);
                button.classList.toggle('is-active', buttonScore === score);
            });
        },

        updateActivePanelMessage: function (itemId, message) {
            var panel = this.getActivePanel(itemId);
            if (!panel) {
                return;
            }

            panel.querySelector('.personalRatingsDetailSummary').textContent = message;
        },

        getSelectedTagIds: function (panel) {
            if (!panel || !panel.dataset.tagIds) {
                return [];
            }

            try {
                var tagIds = JSON.parse(panel.dataset.tagIds);
                return Array.isArray(tagIds) ? tagIds : [];
            } catch (error) {
                return [];
            }
        },

        getSelectedTags: function (panel, availableTags) {
            var tagIds = this.getSelectedTagIds(panel);
            if (!Array.isArray(availableTags)) {
                return [];
            }

            return availableTags.filter(function (tag) {
                return tagIds.indexOf(tag.Id) >= 0;
            });
        },

        buildSummary: function (result, tags) {
            var summaryTags = [];
            var safeResult = result || {
                Score: 0,
                IsPlayed: false,
                IsPendingDelete: false,
                RatedAt: null
            };
            var selectedTags = Array.isArray(tags) ? tags : [];
            var scoreText = safeResult.Score > 0 ? ('当前评分：' + safeResult.Score + ' 分') : '当前评分：未评分';

            if (safeResult.IsPlayed) {
                summaryTags.push('已播放');
            }

            if (safeResult.IsPendingDelete) {
                summaryTags.push('待删除');
            }

            if (safeResult.RatedAt) {
                summaryTags.push('最近评分 ' + this.formatDate(safeResult.RatedAt));
            }

            if (selectedTags.length > 0) {
                summaryTags.push('标签 ' + selectedTags.map(function (tag) {
                    return tag.Name;
                }).join(' / '));
            }

            if (summaryTags.length === 0) {
                summaryTags.push('可以直接在此处完成打分、标签和待删除切换。');
            }

            return scoreText + ' · ' + summaryTags.join(' · ');
        },

        formatDate: function (value) {
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

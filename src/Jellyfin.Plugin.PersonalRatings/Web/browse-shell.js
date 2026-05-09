(function () {
    'use strict';

    if (window.PersonalRatingsBrowseShell) {
        return;
    }

    window.PersonalRatingsBrowseShell = true;

    var auditRoute = 'configurationpage?name=PersonalRatingsAuditPage';
    var backendRoute = 'configurationpage?name=PersonalRatingsManagePage';
    var navClassName = 'personalRatingsNavTab';
    var pageClassName = 'personalRatingsBrowsePage';
    var pageId = 'personalRatingsBrowsePage';
    var route = 'personalratings';
    var stylesheetId = 'personalRatingsBrowseStylesheet';

    var state = {
        features: {
            manageEnabled: true
        },
        isAdministrator: false,
        isLoading: false,
        isFeatureLoading: false,
        isUserLoading: false,
        pageNumber: 1,
        pageSize: 36,
        scoreFilter: 'rated',
        tagIds: [],
        tagMatchMode: 'any',
        playedFilter: 'all',
        mediaType: 'all',
        sortValue: 'ratedAt:desc',
        search: '',
        viewMode: 'poster',
        lastResult: null,
        tags: [],
        requestVersion: 0,
        tagsLoaded: false
    };

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
            hidePage();
            removeActiveNavState();
            return;
        }

        ensureFeatureState();
        ensureUserContext();
        ensureNavEntry();
        updateNavState();

        if (!state.features.manageEnabled) {
            hidePage();
            return;
        }

        if (!isBrowseRoute()) {
            hidePage();
            return;
        }

        ensureStylesheet();
        var page = ensurePage();
        if (!page) {
            return;
        }

        showPage(page);
        safeLoad(page);
    }

    function isBrowseRoute() {
        var hash = window.location.hash || '';
        return hash === '#/' + route || hash.indexOf('#/' + route + '?') === 0;
    }

    function ensureFeatureState() {
        if (state.isFeatureLoading) {
            return;
        }

        state.isFeatureLoading = true;

        window.fetch('/Plugins/PersonalRatings/features', {
            credentials: 'same-origin'
        }).then(function (response) {
            if (!response.ok) {
                throw new Error('Failed to load plugin feature state.');
            }

            return response.json();
        }).then(function (result) {
            state.features.manageEnabled = !!(result && result.IsManagePageEnabled);
            state.isFeatureLoading = false;
            updateNavState();
        }).catch(function () {
            state.features.manageEnabled = true;
            state.isFeatureLoading = false;
            updateNavState();
        });
    }

    function ensureUserContext() {
        if (state.isUserLoading || !window.ApiClient || typeof window.ApiClient.getCurrentUser !== 'function') {
            return;
        }

        state.isUserLoading = true;
        window.ApiClient.getCurrentUser().then(function (user) {
            state.isAdministrator = !!(user && user.Policy && user.Policy.IsAdministrator);
            state.isUserLoading = false;
            syncHeaderActions();
        }).catch(function () {
            state.isAdministrator = false;
            state.isUserLoading = false;
            syncHeaderActions();
        });
    }

    function ensureNavEntry() {
        var containers = document.querySelectorAll('.headerTabs .emby-tabs, .headerTabs');
        if (!containers.length) {
            return;
        }

        containers.forEach(function (container) {
            if (container.querySelector('.' + navClassName)) {
                return;
            }

            var template = container.querySelector('a, button, .emby-tab-button');
            var link = document.createElement('a');
            link.className = template && template.className ? template.className : 'emby-tab-button';
            link.classList.add(navClassName);
            link.href = '#/' + route;
            link.textContent = '打分库';
            link.addEventListener('click', function (event) {
                event.preventDefault();
                navigateTo(route);
            });
            container.appendChild(link);
        });
    }

    function updateNavState() {
        var isActive = isBrowseRoute();
        document.querySelectorAll('.' + navClassName).forEach(function (element) {
            element.hidden = !state.features.manageEnabled;
            element.classList.toggle('is-active', isActive);
            element.classList.toggle('emby-tab-button-active', isActive);
            element.setAttribute('aria-current', isActive ? 'page' : 'false');
        });
    }

    function removeActiveNavState() {
        document.querySelectorAll('.' + navClassName).forEach(function (element) {
            element.classList.remove('is-active');
            element.classList.remove('emby-tab-button-active');
        });
    }

    function ensureStylesheet() {
        if (document.getElementById(stylesheetId)) {
            return;
        }

        var stylesheet = document.createElement('link');
        stylesheet.id = stylesheetId;
        stylesheet.rel = 'stylesheet';
        stylesheet.href = '/Plugins/PersonalRatings/web/browse-page.css';
        document.head.appendChild(stylesheet);
    }

    function ensurePage() {
        var page = document.getElementById(pageId);
        if (page) {
            return page;
        }

        var host = document.querySelector('.mainAnimatedPages');
        if (!host) {
            return null;
        }

        page = document.createElement('section');
        page.id = pageId;
        page.className = 'page type-interior ' + pageClassName;
        page.innerHTML = [
            '<div class="personalRatingsBrowseLayout">',
            '  <section class="personalRatingsBrowseHero">',
            '    <div>',
            '      <p class="personalRatingsBrowseEyebrow">Jellyfin.PersonalRatings</p>',
            '      <h1>打分库</h1>',
            '      <p>把个人评分、待删除和标签收口成一个更接近日常浏览习惯的前台入口。点击卡片仍会回到 Jellyfin 原始详情页。</p>',
            '    </div>',
            '    <div class="personalRatingsBrowseHeroActions">',
            '      <button type="button" class="button-flat personalRatingsBrowseViewButton is-active" data-view-mode="poster">海报视图</button>',
            '      <button type="button" class="button-flat personalRatingsBrowseViewButton" data-view-mode="list">列表视图</button>',
            '      <button type="button" class="button-flat personalRatingsOpenBackendButton">后台管理</button>',
            '      <button type="button" class="button-flat personalRatingsOpenAuditButton" hidden="hidden">删除审计</button>',
            '    </div>',
            '  </section>',
            '  <section class="personalRatingsBrowsePanel">',
            '    <div class="personalRatingsBrowseToolbar">',
            '      <div class="personalRatingsBrowseToolbarGroup">',
            '        <label class="personalRatingsBrowseField">',
            '          <span>评分</span>',
            '          <select is="emby-select" class="selectBrowseScore">',
            '            <option value="rated">全部已评分</option>',
            '            <option value="all">全部交互</option>',
            '            <option value="5">5分</option>',
            '            <option value="4">4分</option>',
            '            <option value="3">3分</option>',
            '            <option value="2">2分</option>',
            '            <option value="1">1分</option>',
            '            <option value="unrated">未评分</option>',
            '          </select>',
            '        </label>',
            '        <label class="personalRatingsBrowseField">',
            '          <span>播放状态</span>',
            '          <select is="emby-select" class="selectBrowsePlayed">',
            '            <option value="all">全部</option>',
            '            <option value="played">已播放</option>',
            '            <option value="unplayed">未播放</option>',
            '          </select>',
            '        </label>',
            '        <label class="personalRatingsBrowseField">',
            '          <span>类型</span>',
            '          <select is="emby-select" class="selectBrowseType">',
            '            <option value="all">全部类型</option>',
            '            <option value="Movie">电影</option>',
            '            <option value="Series">剧集</option>',
            '            <option value="Episode">单集</option>',
            '            <option value="BoxSet">合集</option>',
            '            <option value="Video">视频</option>',
            '          </select>',
            '        </label>',
            '        <label class="personalRatingsBrowseField">',
            '          <span>排序</span>',
            '          <select is="emby-select" class="selectBrowseSort">',
            '            <option value="ratedAt:desc">最近评分</option>',
            '            <option value="updatedAt:desc">最近更新</option>',
            '            <option value="lastPlayedAt:desc">最近播放</option>',
            '            <option value="name:asc">名称 A-Z</option>',
            '            <option value="year:desc">年份新到旧</option>',
            '          </select>',
            '        </label>',
            '      </div>',
            '      <form class="personalRatingsBrowseSearchForm">',
            '        <label class="personalRatingsBrowseField">',
            '          <span>搜索</span>',
            '          <input is="emby-input" type="text" class="txtBrowseSearch" placeholder="片名 / 剧名 / 条目名" />',
            '        </label>',
            '        <button type="submit" class="raised button-submit">查询</button>',
            '        <button type="button" class="button-flat personalRatingsBrowseClearButton">清空</button>',
            '      </form>',
            '    </div>',
            '    <div class="personalRatingsBrowseToolbar" style="margin-top: 16px;">',
            '      <div class="personalRatingsBrowseField" style="min-width: 100%;">',
            '        <div class="personalRatingsBrowseTagHeader">标签</div>',
            '        <div class="personalRatingsBrowseTagFilters"></div>',
            '      </div>',
            '      <label class="personalRatingsBrowseField personalRatingsBrowseTagMatchField" hidden="hidden">',
            '        <span>标签匹配</span>',
            '        <select is="emby-select" class="selectBrowseTagMatch">',
            '          <option value="any">任意命中</option>',
            '          <option value="all">全部命中</option>',
            '        </select>',
            '      </label>',
            '    </div>',
            '  </section>',
            '  <section class="personalRatingsBrowsePanel">',
            '    <div class="personalRatingsBrowseStatus">',
            '      <div class="personalRatingsBrowseSummaryText">正在准备打分库...</div>',
            '      <div class="personalRatingsBrowseStatusText" aria-live="polite"></div>',
            '    </div>',
            '    <div class="personalRatingsBrowseResults is-poster">',
            '      <div class="personalRatingsBrowseCards"></div>',
            '    </div>',
            '    <div class="personalRatingsBrowsePagination">',
            '      <button type="button" class="button-flat personalRatingsBrowsePrevButton">上一页</button>',
            '      <div class="personalRatingsBrowsePageText">第 1 页</div>',
            '      <button type="button" class="button-flat personalRatingsBrowseNextButton">下一页</button>',
            '    </div>',
            '  </section>',
            '</div>'
        ].join('');

        bindPageEvents(page);
        host.appendChild(page);
        syncHeaderActions();
        return page;
    }

    function bindPageEvents(page) {
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
                changePage(-1);
                return;
            }

            if (button.classList.contains('personalRatingsBrowseNextButton')) {
                event.preventDefault();
                changePage(1);
                return;
            }

            if (button.classList.contains('personalRatingsBrowseClearButton')) {
                event.preventDefault();
                clearSearch(page);
                return;
            }

            if (button.classList.contains('personalRatingsBrowseViewButton')) {
                event.preventDefault();
                setViewMode(button.getAttribute('data-view-mode') || 'poster');
                return;
            }

            if (button.classList.contains('personalRatingsBrowseTagChip')) {
                event.preventDefault();
                toggleTagFilter(parseInt(button.getAttribute('data-tag-id'), 10));
                return;
            }

            if (button.classList.contains('personalRatingsOpenBackendButton')) {
                event.preventDefault();
                navigateTo(backendRoute);
                return;
            }

            if (button.classList.contains('personalRatingsOpenAuditButton')) {
                event.preventDefault();
                navigateTo(auditRoute);
            }
        });

        page.addEventListener('change', function (event) {
            var target = event.target;
            if (!target) {
                return;
            }

            if (target.classList.contains('selectBrowseScore')) {
                state.scoreFilter = target.value;
                state.pageNumber = 1;
                safeLoad(page);
                return;
            }

            if (target.classList.contains('selectBrowsePlayed')) {
                state.playedFilter = target.value;
                state.pageNumber = 1;
                safeLoad(page);
                return;
            }

            if (target.classList.contains('selectBrowseType')) {
                state.mediaType = target.value;
                state.pageNumber = 1;
                safeLoad(page);
                return;
            }

            if (target.classList.contains('selectBrowseSort')) {
                state.sortValue = target.value;
                state.pageNumber = 1;
                safeLoad(page);
                return;
            }

            if (target.classList.contains('selectBrowseTagMatch')) {
                state.tagMatchMode = target.value || 'any';
                state.pageNumber = 1;
                safeLoad(page);
            }
        });

        page.querySelector('.personalRatingsBrowseSearchForm').addEventListener('submit', function (event) {
            event.preventDefault();
            state.search = page.querySelector('.txtBrowseSearch').value.trim();
            state.pageNumber = 1;
            safeLoad(page);
        });
    }

    function syncHeaderActions() {
        var page = document.getElementById(pageId);
        if (!page) {
            return;
        }

        var auditButton = page.querySelector('.personalRatingsOpenAuditButton');
        if (auditButton) {
            auditButton.hidden = !state.isAdministrator;
        }

        page.querySelectorAll('.personalRatingsBrowseViewButton').forEach(function (button) {
            button.classList.toggle('is-active', button.getAttribute('data-view-mode') === state.viewMode);
        });
    }

    function showPage(page) {
        page.classList.add('is-active');
    }

    function hidePage() {
        var page = document.getElementById(pageId);
        if (page) {
            page.classList.remove('is-active');
        }
    }

    function safeLoad(page) {
        if (state.isLoading) {
            return;
        }

        if (!state.tagsLoaded) {
            loadTags(page).finally(function () {
                loadResults(page);
            });
            return;
        }

        loadResults(page);
    }

    function loadTags(page) {
        return apiGetJson('Plugins/PersonalRatings/tags').then(function (result) {
            state.tags = Array.isArray(result) ? result : [];
            state.tagsLoaded = true;
            renderTagFilters(page);
        }).catch(function () {
            state.tags = [];
            state.tagsLoaded = true;
            renderTagFilters(page);
        });
    }

    function loadResults(page) {
        if (!isBrowseRoute()) {
            return;
        }

        state.isLoading = true;
        state.requestVersion += 1;
        var requestVersion = state.requestVersion;
        setStatus(page, '正在加载打分库...', 'loading');

        postJson('Plugins/PersonalRatings/ratings/query', buildQueryRequest()).then(function (result) {
            if (requestVersion !== state.requestVersion) {
                return;
            }

            state.lastResult = result;
            renderResults(page);
            setStatus(page, '打分库已刷新。', 'success');
        }).catch(function () {
            if (requestVersion !== state.requestVersion) {
                return;
            }

            state.lastResult = {
                Items: [],
                TotalCount: 0,
                PageNumber: state.pageNumber,
                PageSize: state.pageSize
            };
            renderResults(page);
            setStatus(page, '加载打分库失败。', 'error');
        }).finally(function () {
            if (requestVersion === state.requestVersion) {
                state.isLoading = false;
            }
        });
    }

    function buildQueryRequest() {
        var sortParts = state.sortValue.split(':');
        var request = {
            keyword: state.search || null,
            tagIds: state.tagIds.slice(),
            tagMatchMode: state.tagMatchMode,
            sortBy: sortParts[0] || 'ratedAt',
            sortOrder: sortParts[1] || 'desc',
            pageNumber: state.pageNumber,
            pageSize: state.pageSize
        };

        if (state.scoreFilter === 'rated') {
            request.isRated = true;
        } else if (state.scoreFilter === 'all') {
            request.isRated = null;
        } else if (state.scoreFilter === 'unrated') {
            request.isRated = false;
        } else {
            request.isRated = true;
            request.score = parseInt(state.scoreFilter, 10);
        }

        if (state.playedFilter === 'played') {
            request.isPlayed = true;
        } else if (state.playedFilter === 'unplayed') {
            request.isPlayed = false;
        }

        if (state.mediaType !== 'all') {
            request.mediaTypes = [state.mediaType];
        }

        return request;
    }

    function renderTagFilters(page) {
        var container = page.querySelector('.personalRatingsBrowseTagFilters');
        var matchField = page.querySelector('.personalRatingsBrowseTagMatchField');

        if (!state.tags.length) {
            container.innerHTML = '<div class="personalRatingsEmptyTag">标签筛选已预留，当前还没有可用标签。</div>';
            matchField.hidden = true;
            return;
        }

        container.innerHTML = state.tags.map(function (tag) {
            var isActive = state.tagIds.indexOf(tag.Id) >= 0;
            var style = 'border-color:' + escapeHtml(tag.Color || '#d88b2f') + ';';
            if (isActive) {
                style += ' background:' + hexToTransparent(tag.Color || '#d88b2f', 0.22) + ';';
            }

            return ''
                + '<button type="button" class="button-flat personalRatingsBrowseTagChip'
                + (isActive ? ' is-active' : '')
                + '" data-tag-id="' + tag.Id + '" style="' + style + '">'
                + escapeHtml(tag.Name)
                + '</button>';
        }).join('');

        matchField.hidden = state.tagIds.length <= 1;
    }

    function renderResults(page) {
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
                return renderItemCard(item);
            }).join('');
        }

        renderSummary(page, result);
        renderPagination(page, result);
        syncHeaderActions();
    }

    function renderItemCard(item) {
        var itemId = escapeHtml(item.ItemId);
        var itemName = escapeHtml(item.ItemName || '未命名条目');
        var imageUrl = buildImageUrl(item.ItemId);
        var detailUrl = '#/details?id=' + encodeURIComponent(item.ItemId) + '&serverId=' + encodeURIComponent(getApiClient().serverId());
        var metaParts = [];
        var tags = Array.isArray(item.Tags) ? item.Tags : [];

        if (item.ProductionYear) {
            metaParts.push(item.ProductionYear);
        }

        if (item.ItemType) {
            metaParts.push(item.ItemType);
        } else if (item.MediaType) {
            metaParts.push(item.MediaType);
        }

        return ''
            + '<a class="personalRatingsCardLink" href="' + detailUrl + '" data-item-id="' + itemId + '">'
            + '  <article class="personalRatingsCard">'
            + '    <div class="personalRatingsPoster">'
            + '      <div class="personalRatingsPosterImage" style="background-image:url(\'' + imageUrl + '\')"></div>'
            + '      <div class="personalRatingsPosterOverlay"></div>'
            + '      <div class="personalRatingsPosterBadges">'
            + '        <span class="personalRatingsScoreBadge">' + buildScoreText(item.Score) + '</span>'
            + '        <div class="personalRatingsBadgeStack">'
            + (item.IsPendingDelete ? '<span class="personalRatingsStateBadge">待删除</span>' : '')
            + (item.IsPlayed ? '<span class="personalRatingsCardTag">已播放</span>' : '')
            + '        </div>'
            + '      </div>'
            + '    </div>'
            + '    <div class="personalRatingsCardBody">'
            + '      <h3 class="personalRatingsCardTitle">' + itemName + '</h3>'
            + '      <div class="personalRatingsCardMeta">' + escapeHtml(metaParts.join(' · ') || '未标注类型') + '</div>'
            + '      <div class="personalRatingsCardTags">' + renderTagChips(tags) + '</div>'
            + '    </div>'
            + '  </article>'
            + '</a>';
    }

    function renderTagChips(tags) {
        if (!tags.length) {
            return '<span class="personalRatingsEmptyTag">暂无标签</span>';
        }

        return tags.map(function (tag) {
            var background = hexToTransparent(tag.Color || '#d88b2f', 0.18);
            return '<span class="personalRatingsCardTag" style="background:' + background + '; border:1px solid ' + escapeHtml(tag.Color || '#d88b2f') + ';">'
                + escapeHtml(tag.Name)
                + '</span>';
        }).join('');
    }

    function renderSummary(page, result) {
        var totalCount = result.TotalCount || 0;
        var pageNumber = result.PageNumber || 1;
        var pageSize = result.PageSize || state.pageSize || 36;
        var startIndex = totalCount === 0 ? 0 : ((pageNumber - 1) * pageSize) + 1;
        var endIndex = Math.min(totalCount, pageNumber * pageSize);

        page.querySelector('.personalRatingsBrowseSummaryText').textContent =
            '共 ' + totalCount + ' 条，当前显示 ' + startIndex + '-' + endIndex + '。';
    }

    function renderPagination(page, result) {
        var totalCount = result.TotalCount || 0;
        var pageSize = result.PageSize || state.pageSize || 36;
        var pageNumber = result.PageNumber || state.pageNumber || 1;
        var totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

        page.querySelector('.personalRatingsBrowsePageText').textContent = '第 ' + pageNumber + ' / ' + totalPages + ' 页';
        page.querySelector('.personalRatingsBrowsePrevButton').disabled = pageNumber <= 1;
        page.querySelector('.personalRatingsBrowseNextButton').disabled = pageNumber >= totalPages;
    }

    function setStatus(page, message, type) {
        var statusNode = page.querySelector('.personalRatingsBrowseStatusText');
        statusNode.textContent = message;
        statusNode.classList.remove('is-error', 'is-success', 'is-loading');
        if (type) {
            statusNode.classList.add('is-' + type);
        }
    }

    function setViewMode(viewMode) {
        state.viewMode = viewMode === 'list' ? 'list' : 'poster';
        state.pageSize = state.viewMode === 'list' ? 24 : 36;
        state.pageNumber = 1;
        var page = document.getElementById(pageId);
        if (page) {
            syncHeaderActions();
            safeLoad(page);
        }
    }

    function changePage(delta) {
        var nextPage = state.pageNumber + delta;
        if (nextPage < 1) {
            return;
        }

        state.pageNumber = nextPage;
        var page = document.getElementById(pageId);
        if (page) {
            safeLoad(page);
        }
    }

    function clearSearch(page) {
        page.querySelector('.txtBrowseSearch').value = '';
        state.search = '';
        state.pageNumber = 1;
        safeLoad(page);
    }

    function toggleTagFilter(tagId) {
        if (!tagId || Number.isNaN(tagId)) {
            return;
        }

        var index = state.tagIds.indexOf(tagId);
        if (index >= 0) {
            state.tagIds.splice(index, 1);
        } else {
            state.tagIds.push(tagId);
        }

        state.pageNumber = 1;
        var page = document.getElementById(pageId);
        if (page) {
            renderTagFilters(page);
            safeLoad(page);
        }
    }

    function buildImageUrl(itemId) {
        return getApiClient().getUrl('Items/' + itemId + '/Images/Primary', {
            fillHeight: 420,
            fillWidth: 280,
            quality: 90
        });
    }

    function buildScoreText(score) {
        return score > 0 ? (score + ' 分') : '未评分';
    }

    function navigateTo(targetRoute) {
        if (window.Dashboard && typeof window.Dashboard.navigate === 'function') {
            window.Dashboard.navigate(targetRoute);
            return;
        }

        window.location.hash = '#/' + targetRoute;
    }

    function apiGetJson(path) {
        return getApiClient().ajax({
            type: 'GET',
            url: getApiClient().getUrl(path),
            dataType: 'json'
        });
    }

    function postJson(path, payload) {
        return getApiClient().ajax({
            type: 'POST',
            url: getApiClient().getUrl(path),
            contentType: 'application/json',
            dataType: 'json',
            data: JSON.stringify(payload)
        });
    }

    function getApiClient() {
        return window.ApiClient;
    }

    function escapeHtml(value) {
        return String(value || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function hexToTransparent(hex, alpha) {
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
})();

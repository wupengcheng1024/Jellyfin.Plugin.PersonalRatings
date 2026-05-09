(function () {
    'use strict';

    if (window.PersonalRatingsBrowseShell) {
        return;
    }

    if (!window.PersonalRatingsBrowseState
        || !window.PersonalRatingsBrowseApi
        || !window.PersonalRatingsBrowseRenderer
        || !window.PersonalRatingsBrowseFilters) {
        return;
    }

    /**
     * Bootstraps the front browse route, shell lifecycle and progressive loading.
     * Rendering, API access and state mutation are delegated to dedicated modules.
     */
    window.PersonalRatingsBrowseShell = true;

    var auditRoute = 'configurationpage?name=PersonalRatingsAuditPage';
    var backendRoute = 'configurationpage?name=PersonalRatingsManagePage';
    var navClassName = 'personalRatingsNavTab';
    var pageClassName = 'personalRatingsBrowsePage';
    var pageId = 'personalRatingsBrowsePage';
    var route = 'personalratings';
    var stylesheetId = 'personalRatingsBrowseStylesheet';
    var state = window.PersonalRatingsBrowseState.create();

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

        if (!state.features.manageEnabled || !isBrowseRoute()) {
            hidePage();
            return;
        }

        ensureStylesheet();
        var page = ensurePage();
        if (!page) {
            return;
        }

        showPage(page);
        window.PersonalRatingsBrowseFilters.syncHeaderActions(page, state);
        safeLoad(page);
    }

    function ensureFeatureState() {
        if (state.isFeatureLoading) {
            return;
        }

        state.isFeatureLoading = true;
        window.PersonalRatingsBrowseApi.getFeatureState().then(function (result) {
            window.PersonalRatingsBrowseState.setFeatureState(state, result && result.IsManagePageEnabled);
        }).catch(function () {
            window.PersonalRatingsBrowseState.setFeatureState(state, true);
        }).finally(function () {
            state.isFeatureLoading = false;
            updateNavState();
        });
    }

    function ensureUserContext() {
        if (state.isUserLoading || !window.ApiClient || typeof window.ApiClient.getCurrentUser !== 'function') {
            return;
        }

        state.isUserLoading = true;
        window.PersonalRatingsBrowseApi.getCurrentUser().then(function (user) {
            window.PersonalRatingsBrowseState.setUserAdministrator(state, !!(user && user.Policy && user.Policy.IsAdministrator));
        }).catch(function () {
            window.PersonalRatingsBrowseState.setUserAdministrator(state, false);
        }).finally(function () {
            state.isUserLoading = false;
            syncHeaderActions();
        });
    }

    function isBrowseRoute() {
        var hash = window.location.hash || '';
        return hash === '#/' + route || hash.indexOf('#/' + route + '?') === 0;
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
                window.PersonalRatingsBrowseApi.navigateTo(route);
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
        page.innerHTML = buildPageMarkup();
        bindPageEvents(page);
        host.appendChild(page);
        syncHeaderActions();
        return page;
    }

    function buildPageMarkup() {
        return [
            '<div class="personalRatingsBrowseLayout">',
            '  <section class="personalRatingsBrowseHero">',
            '    <div>',
            '      <p class="personalRatingsBrowseEyebrow">Jellyfin.PersonalRatings</p>',
            '      <h1>打分库</h1>',
            '      <p>把评分、待删除和标签收口成一个更接近日常浏览的前台入口。点击任意卡片，仍会回到 Jellyfin 原始详情页。</p>',
            '      <div class="personalRatingsBrowseModeHint"></div>',
            '    </div>',
            '    <div class="personalRatingsBrowseHeroActions">',
            '      <button type="button" class="button-flat personalRatingsBrowseViewButton is-active" data-view-mode="poster">海报视图</button>',
            '      <button type="button" class="button-flat personalRatingsBrowseViewButton" data-view-mode="list">列表视图</button>',
            '      <button type="button" class="button-flat personalRatingsOpenBackendButton">管理模式</button>',
            '      <button type="button" class="button-flat personalRatingsOpenAuditButton" hidden="hidden">删除审计</button>',
            '    </div>',
            '  </section>',
            '  <section class="personalRatingsBrowsePanel personalRatingsBrowsePanel-toolbar">',
            '    <div class="personalRatingsBrowseToolbar">',
            '      <div class="personalRatingsBrowseToolbarGroup">',
            '        <label class="personalRatingsBrowseField personalRatingsBrowseField-compact">',
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
            '        <label class="personalRatingsBrowseField personalRatingsBrowseField-compact">',
            '          <span>播放状态</span>',
            '          <select is="emby-select" class="selectBrowsePlayed">',
            '            <option value="all">全部</option>',
            '            <option value="played">已播放</option>',
            '            <option value="unplayed">未播放</option>',
            '          </select>',
            '        </label>',
            '        <label class="personalRatingsBrowseField personalRatingsBrowseField-compact">',
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
            '        <label class="personalRatingsBrowseField personalRatingsBrowseField-compact">',
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
            '        <label class="personalRatingsBrowseField personalRatingsBrowseField-search">',
            '          <span>搜索</span>',
            '          <input is="emby-input" type="text" class="txtBrowseSearch" placeholder="片名 / 剧名 / 条目名" />',
            '        </label>',
            '        <button type="submit" class="raised button-submit">查询</button>',
            '        <button type="button" class="button-flat personalRatingsBrowseClearButton">清空</button>',
            '      </form>',
            '    </div>',
            '    <div class="personalRatingsBrowseTagRow">',
            '      <div class="personalRatingsBrowseField personalRatingsBrowseField-tags">',
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
    }

    function bindPageEvents(page) {
        window.PersonalRatingsBrowseFilters.bindPageEvents(page, state, {
            onChangePage: function (delta) {
                if (window.PersonalRatingsBrowseState.changePage(state, delta)) {
                    safeLoad(page);
                }
            },
            onClearSearch: function () {
                page.querySelector('.txtBrowseSearch').value = '';
                window.PersonalRatingsBrowseState.clearSearch(state);
                safeLoad(page);
            },
            onViewMode: function (viewMode) {
                window.PersonalRatingsBrowseState.setViewMode(state, viewMode);
                syncHeaderActions();
                safeLoad(page);
            },
            onToggleTag: function (tagId) {
                window.PersonalRatingsBrowseState.toggleTagFilter(state, tagId);
                window.PersonalRatingsBrowseFilters.renderTagFilters(page, state);
                safeLoad(page);
            },
            onOpenBackend: function () {
                window.PersonalRatingsBrowseApi.navigateTo(backendRoute);
            },
            onOpenAudit: function () {
                window.PersonalRatingsBrowseApi.navigateTo(auditRoute);
            },
            onScoreFilter: function (value) {
                window.PersonalRatingsBrowseState.setScoreFilter(state, value);
                safeLoad(page);
            },
            onPlayedFilter: function (value) {
                window.PersonalRatingsBrowseState.setPlayedFilter(state, value);
                safeLoad(page);
            },
            onMediaType: function (value) {
                window.PersonalRatingsBrowseState.setMediaType(state, value);
                safeLoad(page);
            },
            onSort: function (value) {
                window.PersonalRatingsBrowseState.setSortValue(state, value);
                safeLoad(page);
            },
            onTagMatchMode: function (value) {
                window.PersonalRatingsBrowseState.setTagMatchMode(state, value);
                safeLoad(page);
            },
            onSearch: function (value) {
                window.PersonalRatingsBrowseState.setSearch(state, value);
                safeLoad(page);
            }
        });
    }

    function syncHeaderActions() {
        var page = document.getElementById(pageId);
        if (!page) {
            return;
        }

        window.PersonalRatingsBrowseFilters.syncHeaderActions(page, state);
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
        return window.PersonalRatingsBrowseApi.getTags().then(function (result) {
            window.PersonalRatingsBrowseState.setTags(state, Array.isArray(result) ? result : []);
        }).catch(function () {
            window.PersonalRatingsBrowseState.setTags(state, []);
        }).finally(function () {
            window.PersonalRatingsBrowseFilters.renderTagFilters(page, state);
        });
    }

    function loadResults(page) {
        if (!isBrowseRoute()) {
            return;
        }

        state.isLoading = true;
        state.requestVersion += 1;
        var requestVersion = state.requestVersion;
        window.PersonalRatingsBrowseRenderer.setStatus(page, '正在加载打分库...', 'loading');

        window.PersonalRatingsBrowseApi.queryRatings(window.PersonalRatingsBrowseState.buildQueryRequest(state)).then(function (result) {
            if (requestVersion !== state.requestVersion) {
                return;
            }

            window.PersonalRatingsBrowseState.setResult(state, result);
            window.PersonalRatingsBrowseRenderer.renderResults(page, state);
            window.PersonalRatingsBrowseRenderer.setStatus(page, '打分库已刷新。', 'success');
            syncHeaderActions();
        }).catch(function () {
            if (requestVersion !== state.requestVersion) {
                return;
            }

            window.PersonalRatingsBrowseState.setResult(state, {
                Items: [],
                TotalCount: 0,
                PageNumber: state.pageNumber,
                PageSize: state.pageSize
            });
            window.PersonalRatingsBrowseRenderer.renderResults(page, state);
            window.PersonalRatingsBrowseRenderer.setStatus(page, '加载打分库失败。', 'error');
            syncHeaderActions();
        }).finally(function () {
            if (requestVersion === state.requestVersion) {
                state.isLoading = false;
            }
        });
    }
})();

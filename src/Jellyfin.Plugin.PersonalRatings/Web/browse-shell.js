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
     * Boots the front browse route with conservative lifecycle rules so the
     * custom page cannot leak into Jellyfin's native home / details pages.
     */
    window.PersonalRatingsBrowseShell = true;

    var auditRoute = 'configurationpage?name=PersonalRatingsAuditPage';
    var backendRoute = 'configurationpage?name=PersonalRatingsManagePage';
    var assetVersion = '20260510-native-browse-v6';
    var navClassName = 'personalRatingsNavTab';
    var pageClassName = 'personalRatingsBrowsePage';
    var pageId = 'personalRatingsBrowsePage';
    var pendingNativeTabTarget = null;
    var nativeHomeRoute = 'home.html';
    var nativeRouteBrowseQueryKey = 'personalratings';
    var posterCardGap = 16;
    var posterCardMinWidth = 160;
    var posterRowsPerPage = 4;
    var route = 'personalratings';
    var stylesheetId = 'personalRatingsBrowseStylesheet';
    var headerObserver = null;
    var observedHeaderTabsHost = null;
    var cachedHeaderTabsMarkup = '';
    var syncTimerIds = [];
    var state = window.PersonalRatingsBrowseState.create();

    normalizeInitialRoute();
    bindShell();
    scheduleSyncBurst();

    function bindShell() {
        window.addEventListener('hashchange', scheduleSyncBurst);
        window.addEventListener('popstate', scheduleSyncBurst);
        window.addEventListener('pageshow', scheduleSyncBurst);
        document.addEventListener('click', handleBrowseNavClick, true);
        window.addEventListener('resize', updateActivePageOffset);
        document.addEventListener('click', handleHeaderTabClick, true);
        document.addEventListener('visibilitychange', function () {
            if (!document.hidden) {
                scheduleSyncBurst();
            }
        });

        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', scheduleSyncBurst, {
                once: true
            });
        }
    }

    function scheduleSyncBurst() {
        clearSyncTimers();
        [0, 80, 220, 480, 900, 1500].forEach(function (delay) {
            var timerId = window.setTimeout(function () {
                sync();
            }, delay);
            syncTimerIds.push(timerId);
        });
    }

    function clearSyncTimers() {
        syncTimerIds.forEach(function (timerId) {
            window.clearTimeout(timerId);
        });
        syncTimerIds = [];
    }

    function sync() {
        if (!window.ApiClient || typeof window.ApiClient.isLoggedIn !== 'function' || !window.ApiClient.isLoggedIn()) {
            setBrowseRouteMode(false);
            cleanupDuplicateNavEntries(null);
            destroyPage();
            return;
        }

        var browseRouteActive = isBrowseRoute();
        rememberHeaderTabsMarkup();
        restoreHeaderTabsMarkupIfNeeded(browseRouteActive);
        var headerTabsHost = findPrimaryHeaderTabsHost();
        ensureHeaderObserver(headerTabsHost);

        if (!browseRouteActive && tryRestorePendingBrowseRoute(headerTabsHost)) {
            browseRouteActive = true;
        }

        if (!state.featuresLoaded && !state.isFeatureLoading && (browseRouteActive || !!headerTabsHost)) {
            ensureFeatureState();
        }

        if (state.featuresLoaded && !state.features.manageEnabled) {
            cleanupDuplicateNavEntries(null);
        } else if (state.featuresLoaded && state.features.manageEnabled && headerTabsHost && ensureNavEntry(headerTabsHost)) {
            cleanupDuplicateNavEntries(headerTabsHost);
            updateNavState();
        }

        if (!browseRouteActive) {
            tryActivatePendingNativeTab(headerTabsHost);
            setBrowseRouteMode(false);
            destroyPage();
            return;
        }

        ensureStylesheet();
        setBrowseRouteMode(true);
        var page = ensurePage();
        if (!page) {
            return;
        }

        showPage(page);
        updatePageOffset(page);
        updateResponsivePageSize(page);

        if (!state.featuresLoaded) {
            renderMessageState(page, '正在准备打分库...', '正在准备打分库...', 'loading');
            return;
        }

        if (!state.features.manageEnabled) {
            renderMessageState(page, '打分库入口当前不可用。', '打分库入口当前已被插件配置关闭。', 'error');
            cleanupDuplicateNavEntries(null);
            return;
        }

        if (!state.userContextLoaded && !state.isUserLoading) {
            ensureUserContext();
        }

        syncHeaderActions(page);
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
            scheduleSyncBurst();
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
            syncHeaderActions(document.getElementById(pageId));
        });
    }

    function isBrowseRoute() {
        return isBrowseHash(window.location.hash || '');
    }

    function isBrowseHash(hash) {
        return hash === '#/' + route || hash.indexOf('#/' + route + '?') === 0;
    }

    function normalizeInitialRoute() {
        if (!isBrowseHash(window.location.hash || '')) {
            return;
        }

        redirectToNativeHomeBootstrapRoute();
    }

    function tryRestorePendingBrowseRoute(headerTabsHost) {
        if (!headerTabsHost || !hasPendingBrowseBootstrapRequest()) {
            return false;
        }

        replaceHashWithoutNavigation('#/' + route);
        return true;
    }

    function hasPendingBrowseBootstrapRequest() {
        try {
            var hash = window.location.hash || '';
            if (hash.indexOf('#/' + nativeHomeRoute) !== 0) {
                return false;
            }

            var parsedUrl = new URL(window.location.origin + '/' + hash.substring(2));
            return parsedUrl.searchParams.get(nativeRouteBrowseQueryKey) === '1';
        } catch (error) {
            void error;
            return false;
        }
    }

    function redirectToNativeHomeBootstrapRoute() {
        var bootstrapUrl = buildNativeHomeBootstrapUrl();
        if (!bootstrapUrl) {
            return;
        }

        window.location.replace(bootstrapUrl);
    }

    function buildNativeHomeBootstrapUrl() {
        try {
            var currentUrl = new URL(window.location.href);
            var basePath = currentUrl.pathname;
            if (basePath.endsWith('index.html')) {
                basePath = basePath.substring(0, basePath.length - 'index.html'.length);
            }

            return currentUrl.origin + basePath + '#/' + nativeHomeRoute + '?' + nativeRouteBrowseQueryKey + '=1';
        } catch (error) {
            return null;
        }
    }

    function replaceHashWithoutNavigation(hash) {
        try {
            window.history.replaceState(window.history.state, document.title, hash);
        } catch (error) {
            window.location.hash = hash;
        }
    }

    function getHeaderTabsContainer() {
        return document.querySelector('.skinHeader .headerTabs');
    }

    function rememberHeaderTabsMarkup() {
        var headerTabsContainer = getHeaderTabsContainer();
        if (!headerTabsContainer) {
            return;
        }

        var markup = String(headerTabsContainer.innerHTML || '').trim();
        var text = String(headerTabsContainer.textContent || '').replace(/\s+/g, '');
        if (!markup || !text) {
            return;
        }

        cachedHeaderTabsMarkup = markup;
    }

    function restoreHeaderTabsMarkupIfNeeded(browseRouteActive) {
        if (!browseRouteActive || !cachedHeaderTabsMarkup) {
            return;
        }

        var headerTabsContainer = getHeaderTabsContainer();
        if (!headerTabsContainer) {
            return;
        }

        var currentText = String(headerTabsContainer.textContent || '').replace(/\s+/g, '');
        if (currentText) {
            return;
        }

        headerTabsContainer.innerHTML = cachedHeaderTabsMarkup;
    }

    function ensureHeaderObserver(headerTabsHost) {
        var observerTarget = findHeaderObserverTarget(headerTabsHost);
        if (!observerTarget || observedHeaderTabsHost === observerTarget) {
            return;
        }

        if (headerObserver) {
            headerObserver.disconnect();
        }

        observedHeaderTabsHost = observerTarget;
        headerObserver = new MutationObserver(function () {
            scheduleSyncBurst();
        });
        headerObserver.observe(observerTarget, {
            childList: true,
            subtree: true
        });
    }

    function findHeaderObserverTarget(headerTabsHost) {
        if (!headerTabsHost) {
            return document.querySelector('.skinHeader .headerTabs');
        }

        if (headerTabsHost.classList.contains('emby-tabs-slider')) {
            return headerTabsHost.parentElement || headerTabsHost;
        }

        return headerTabsHost;
    }

    function handleBrowseNavClick(event) {
        var target = event.target;
        if (!target || typeof target.closest !== 'function') {
            return;
        }

        var navEntry = target.closest('.' + navClassName);
        if (!navEntry) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        window.PersonalRatingsBrowseApi.navigateTo(route);
    }

    function handleHeaderTabClick(event) {
        if (!isBrowseRoute()) {
            return;
        }

        var target = event.target;
        if (!target || typeof target.closest !== 'function') {
            return;
        }

        var tabButton = target.closest('.skinHeader .headerTabs a, .skinHeader .headerTabs button, .skinHeader .headerTabs .emby-tab-button');
        if (!tabButton || tabButton.classList.contains(navClassName)) {
            return;
        }

        var normalizedText = String(tabButton.textContent || '').replace(/\s+/g, '').toLowerCase();
        if (normalizedText.indexOf('首页') >= 0 || normalizedText.indexOf('home') >= 0) {
            event.preventDefault();
            event.stopPropagation();
            navigateToNativeHeaderTab('home');
            return;
        }

        if (normalizedText.indexOf('我的最爱') < 0
            && normalizedText.indexOf('最爱') < 0
            && normalizedText.indexOf('favorites') < 0) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        navigateToNativeHeaderTab('favorites');
    }

    function navigateToNativeHeaderTab(target) {
        pendingNativeTabTarget = target;
        setBrowseRouteMode(false);
        destroyPage();
        window.location.hash = '#/' + nativeHomeRoute;
    }

    function tryActivatePendingNativeTab(headerTabsHost) {
        if (!pendingNativeTabTarget || !headerTabsHost) {
            return;
        }

        var targetTab = findNativeHeaderTab(headerTabsHost, pendingNativeTabTarget);
        if (!targetTab) {
            return;
        }

        var pendingTarget = pendingNativeTabTarget;
        pendingNativeTabTarget = null;
        window.setTimeout(function () {
            if (pendingTarget === 'home') {
                targetTab.click();
                return;
            }

            targetTab.click();
        }, 0);
    }

    function findNativeHeaderTab(container, target) {
        var candidates = getTabCandidates(container);
        for (var index = 0; index < candidates.length; index += 1) {
            var candidate = candidates[index];
            if (!candidate || !candidate.textContent || (candidate.classList && candidate.classList.contains(navClassName))) {
                continue;
            }

            var normalizedText = String(candidate.textContent || '').replace(/\s+/g, '').toLowerCase();
            if (target === 'home' && (normalizedText.indexOf('首页') >= 0 || normalizedText.indexOf('home') >= 0)) {
                return candidate;
            }

            if (target === 'favorites'
                && (normalizedText.indexOf('我的最爱') >= 0
                    || normalizedText.indexOf('最爱') >= 0
                    || normalizedText.indexOf('favorites') >= 0)) {
                return candidate;
            }
        }

        return null;
    }

    function findPrimaryHeaderTabsHost() {
        var selectors = [
            '.skinHeader .headerTabs .emby-tabs-slider',
            '.skinHeader .headerTabs .emby-tabs',
            '.skinHeader .headerTabs'
        ];

        for (var selectorIndex = 0; selectorIndex < selectors.length; selectorIndex += 1) {
            var candidates = document.querySelectorAll(selectors[selectorIndex]);
            for (var candidateIndex = 0; candidateIndex < candidates.length; candidateIndex += 1) {
                var candidate = candidates[candidateIndex];
                if (!candidate || !isVisibleElement(candidate)) {
                    continue;
                }

                return candidate;
            }
        }

        return null;
    }

    function isVisibleElement(element) {
        if (!element) {
            return false;
        }

        if (element.offsetParent !== null) {
            return true;
        }

        var style = window.getComputedStyle(element);
        return style.display !== 'none' && style.visibility !== 'hidden';
    }

    function findFavoritesTab(container) {
        var candidates = getTabCandidates(container);
        for (var index = 0; index < candidates.length; index += 1) {
            var candidate = candidates[index];
            if (candidate.classList && candidate.classList.contains(navClassName)) {
                continue;
            }

            var text = String(candidate.textContent || '').replace(/\s+/g, '');
            var normalizedText = text.toLowerCase();
            var href = String(candidate.getAttribute('href') || '').toLowerCase();
            if (text.indexOf('我的最爱') >= 0
                || text.indexOf('最爱') >= 0
                || normalizedText.indexOf('favorites') >= 0
                || href.indexOf('favorites') >= 0) {
                return candidate;
            }
        }

        return null;
    }

    function getTabCandidates(container) {
        if (!container) {
            return [];
        }

        if (container.classList.contains('emby-tabs-slider')) {
            return Array.from(container.children);
        }

        var slider = container.querySelector('.emby-tabs-slider');
        if (slider && slider.children.length) {
            return Array.from(slider.children);
        }

        return Array.from(container.querySelectorAll('a, button, .emby-tab-button'));
    }

    function ensureNavEntry(container) {
        var favoritesTab = findFavoritesTab(container);
        if (!favoritesTab) {
            return false;
        }

        var host = favoritesTab.parentElement || container;
        var existing = host.querySelector('.' + navClassName);
        if (existing) {
            ensureNavButtonBehavior(existing);
            if (existing.previousElementSibling !== favoritesTab) {
                favoritesTab.insertAdjacentElement('afterend', existing);
            }
            return true;
        }

        var navButton = buildNavButton(favoritesTab);
        favoritesTab.insertAdjacentElement('afterend', navButton);
        return true;
    }

    function buildNavButton(templateTab) {
        var navButton = document.createElement('button');
        navButton.type = 'button';
        navButton.className = templateTab.className || 'emby-tab-button emby-button';
        navButton.classList.add(navClassName);
        navButton.setAttribute('aria-current', 'false');
        navButton.setAttribute('title', '打分库');

        var isAttribute = templateTab.getAttribute('is');
        if (isAttribute) {
            navButton.setAttribute('is', isAttribute);
        }

        var foregroundTemplate = templateTab.querySelector('.emby-button-foreground');
        if (foregroundTemplate) {
            var foreground = document.createElement('div');
            foreground.className = foregroundTemplate.className;
            foreground.textContent = '打分库';
            navButton.appendChild(foreground);
        } else {
            navButton.textContent = '打分库';
        }

        ensureNavButtonBehavior(navButton);
        return navButton;
    }

    function ensureNavButtonBehavior(navButton) {
        if (!navButton || navButton.dataset.personalRatingsBound === 'true') {
            return;
        }

        navButton.dataset.personalRatingsBound = 'true';
        navButton.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation();
            window.PersonalRatingsBrowseApi.navigateTo(route);
        });
        navButton.addEventListener('keydown', function (event) {
            if (event.key !== 'Enter' && event.key !== ' ') {
                return;
            }

            event.preventDefault();
            event.stopPropagation();
            window.PersonalRatingsBrowseApi.navigateTo(route);
        });
    }

    function cleanupDuplicateNavEntries(primaryHost) {
        var entries = document.querySelectorAll('.' + navClassName);
        var hasKeptPrimaryEntry = false;
        entries.forEach(function (entry) {
            if (!primaryHost || !primaryHost.contains(entry)) {
                entry.remove();
                return;
            }

            if (!hasKeptPrimaryEntry) {
                hasKeptPrimaryEntry = true;
                return;
            }

            entry.remove();
        });
    }

    function updateNavState() {
        var isActive = isBrowseRoute();
        document.querySelectorAll('.' + navClassName).forEach(function (element) {
            element.classList.toggle('is-active', isActive);
            element.classList.toggle('emby-tab-button-active', isActive);
            element.setAttribute('aria-current', isActive ? 'page' : 'false');
        });
    }

    function ensureStylesheet() {
        var expectedHref = '/Plugins/PersonalRatings/web/browse-page.css?v=' + assetVersion;
        var existing = document.getElementById(stylesheetId);
        if (existing) {
            if (existing.getAttribute('href') !== expectedHref) {
                existing.setAttribute('href', expectedHref);
            }

            return;
        }

        var stylesheet = document.createElement('link');
        stylesheet.id = stylesheetId;
        stylesheet.rel = 'stylesheet';
        stylesheet.href = expectedHref;
        document.head.appendChild(stylesheet);
    }

    function setBrowseRouteMode(isActive) {
        if (!document.body || !document.documentElement) {
            return;
        }

        document.body.classList.toggle('personalRatingsBrowseRouteActive', isActive);
        document.documentElement.classList.toggle('personalRatingsBrowseRouteActive', isActive);
    }

    function ensurePage() {
        var page = document.getElementById(pageId);
        if (page) {
            return page;
        }

        if (!document.body) {
            return null;
        }

        page = document.createElement('section');
        page.id = pageId;
        page.className = 'page libraryPage collectionEditorPage type-interior ' + pageClassName;
        page.setAttribute('aria-label', '打分库');
        page.innerHTML = buildPageMarkup();
        bindPageEvents(page);
        document.body.appendChild(page);
        syncHeaderActions(page);
        return page;
    }

    function buildPageMarkup() {
        return [
            '<div class="pageTabContent is-active personalRatingsBrowseTabContent">',
            '  <div class="flex align-items-center justify-content-center flex-wrap-wrap padded-top padded-left padded-right padded-bottom focuscontainer-x personalRatingsBrowseCommandBar">',
            '    <div class="paging personalRatingsBrowsePaging">',
            '      <div class="personalRatingsBrowseCollectionLabel">打分库</div>',
            '      <div class="personalRatingsBrowseSummaryText">正在准备打分库...</div>',
            '      <div class="personalRatingsBrowseModeHint"></div>',
            '    </div>',
            '    <div class="personalRatingsBrowseCommandButtons">',
            '      <button type="button" is="paper-icon-button-light" class="btnSearch autoSize paper-icon-button-light personalRatingsBrowseToolbarButton personalRatingsBrowseModeButton" data-panel-mode="search" title="搜索"><span class="material-icons search" aria-hidden="true"></span></button>',
            '      <button type="button" is="paper-icon-button-light" class="btnSelectView autoSize paper-icon-button-light personalRatingsBrowseToolbarButton personalRatingsBrowseViewButton is-active" data-view-mode="poster" title="海报视图"><span class="material-icons view_module" aria-hidden="true"></span></button>',
            '      <button type="button" is="paper-icon-button-light" class="btnSelectView autoSize paper-icon-button-light personalRatingsBrowseToolbarButton personalRatingsBrowseViewButton" data-view-mode="list" title="列表视图"><span class="material-icons view_agenda" aria-hidden="true"></span></button>',
            '      <button type="button" is="paper-icon-button-light" class="btnSort autoSize paper-icon-button-light personalRatingsBrowseToolbarButton personalRatingsBrowseModeButton" data-panel-mode="sort" title="排序"><span class="material-icons sort_by_alpha" aria-hidden="true"></span></button>',
            '      <div class="btnFilter-wrapper personalRatingsBrowseFilterButtonWrap">',
            '        <button type="button" is="paper-icon-button-light" class="btnFilter autoSize paper-icon-button-light personalRatingsBrowseToolbarButton personalRatingsBrowseModeButton" data-panel-mode="filter" title="筛选"><span class="material-icons filter_list" aria-hidden="true"></span></button>',
            '      </div>',
            '      <button type="button" class="button-flat personalRatingsBrowseUtilityButton personalRatingsOpenBackendButton">评分后台</button>',
            '      <button type="button" class="button-flat personalRatingsBrowseUtilityButton personalRatingsOpenAuditButton" hidden="hidden">删除审计</button>',
            '    </div>',
            '  </div>',
            '  <div class="itemsContainer padded-left padded-right personalRatingsBrowseFilterTray" hidden="hidden">',
            '    <section class="personalRatingsBrowseFilterSheet">',
            '      <div class="personalRatingsBrowseFilterSheetHeader">',
            '        <div>',
            '          <div class="personalRatingsBrowseFilterSheetTitle">筛选与搜索</div>',
            '          <div class="personalRatingsBrowseFilterSheetSummary">可按评分、标签、播放状态和类型缩小结果范围。</div>',
            '        </div>',
            '        <button type="button" class="personalRatingsBrowseSheetCloseButton" title="关闭" aria-label="关闭">',
            '          <span class="material-icons" aria-hidden="true">close</span>',
            '        </button>',
            '      </div>',
            '      <form class="personalRatingsBrowseSearchForm personalRatingsBrowseFilterRow personalRatingsBrowsePanelSection personalRatingsBrowsePanelSection-search">',
            '        <label class="personalRatingsBrowseField personalRatingsBrowseField-search">',
            '          <span>搜索</span>',
            '          <input is="emby-input" type="text" class="txtBrowseSearch" placeholder="片名 / 剧名 / 条目名" />',
            '        </label>',
            '        <button type="submit" class="raised button-submit">查询</button>',
            '        <button type="button" class="button-flat personalRatingsBrowseClearButton">清空搜索</button>',
            '      </form>',
            '      <div class="personalRatingsBrowseFilterRow personalRatingsBrowsePanelSection personalRatingsBrowsePanelSection-sort">',
            '        <label class="personalRatingsBrowseField personalRatingsBrowseField-compact personalRatingsBrowseField-sortOnly">',
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
            '      <div class="personalRatingsBrowseFilterRow personalRatingsBrowseFilterRow-grid personalRatingsBrowsePanelSection personalRatingsBrowsePanelSection-filterGrid">',
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
            '      </div>',
            '      <div class="personalRatingsBrowseFilterRow personalRatingsBrowseFilterRow-tags personalRatingsBrowsePanelSection personalRatingsBrowsePanelSection-filterTags">',
            '        <div class="personalRatingsBrowseField personalRatingsBrowseField-tags">',
            '          <div class="personalRatingsBrowseTagHeader">标签</div>',
            '          <div class="personalRatingsBrowseTagFilters"></div>',
            '        </div>',
            '        <label class="personalRatingsBrowseField personalRatingsBrowseTagMatchField" hidden="hidden">',
            '          <span>标签匹配</span>',
            '          <select is="emby-select" class="selectBrowseTagMatch">',
            '            <option value="any">任意命中</option>',
            '            <option value="all">全部命中</option>',
            '          </select>',
            '        </label>',
            '      </div>',
            '      <div class="personalRatingsBrowseFilterActions personalRatingsBrowsePanelSection personalRatingsBrowsePanelSection-filterActions">',
            '        <button type="button" class="button-flat personalRatingsBrowseClearFiltersButton">清空全部筛选</button>',
            '      </div>',
            '    </section>',
            '  </div>',
            '  <div class="itemsContainer padded-left padded-right personalRatingsBrowseActiveBar">',
            '    <div class="personalRatingsBrowseStatusText" aria-live="polite"></div>',
            '    <div class="personalRatingsBrowseActiveFilters"></div>',
            '  </div>',
            '  <div class="itemsContainer padded-left padded-right personalRatingsBrowseResults">',
            '    <div class="personalRatingsBrowseCards"></div>',
            '  </div>',
            '  <div class="flex align-items-center justify-content-center flex-wrap-wrap padded-top padded-left padded-right padded-bottom focuscontainer-x personalRatingsBrowseFooterBar">',
            '    <button type="button" class="button-flat personalRatingsBrowsePrevButton">上一页</button>',
            '    <div class="paging personalRatingsBrowsePageText">第 1 / 1 页</div>',
            '    <button type="button" class="button-flat personalRatingsBrowseNextButton">下一页</button>',
            '  </div>',
            '</div>'
        ].join('');
    }

    function bindPageEvents(page) {
        window.PersonalRatingsBrowseFilters.bindPageEvents(page, state, {
            onChangePage: function (delta) {
                if (window.PersonalRatingsBrowseState.changePage(state, delta)) {
                    state.needsReload = true;
                    safeLoad(page);
                }
            },
            onClearSearch: function () {
                page.querySelector('.txtBrowseSearch').value = '';
                window.PersonalRatingsBrowseState.clearSearch(state);
                state.needsReload = true;
                safeLoad(page);
            },
            onViewMode: function (viewMode) {
                window.PersonalRatingsBrowseState.setViewMode(state, viewMode);
                syncHeaderActions(page);
                if (updateResponsivePageSize(page)) {
                    safeLoad(page);
                } else if (state.lastResult) {
                    window.PersonalRatingsBrowseRenderer.renderResults(page, state);
                    restorePageScroll(page);
                } else {
                    safeLoad(page);
                }
            },
            onToggleTag: function (tagId) {
                window.PersonalRatingsBrowseState.toggleTagFilter(state, tagId);
                state.needsReload = true;
                window.PersonalRatingsBrowseFilters.renderTagFilters(page, state);
                safeLoad(page);
            },
            onOpenPanel: function (mode) {
                window.PersonalRatingsBrowseState.setActivePanelMode(state, state.activePanelMode === mode ? null : mode);
                syncHeaderActions(page);
                if (state.activePanelMode === 'search') {
                    window.setTimeout(function () {
                        var input = page.querySelector('.txtBrowseSearch');
                        if (input) {
                            input.focus();
                        }
                    }, 0);
                }
            },
            onClosePanel: function () {
                window.PersonalRatingsBrowseState.setActivePanelMode(state, null);
                syncHeaderActions(page);
            },
            onOpenBackend: function () {
                openPluginAdminRoute(backendRoute);
            },
            onOpenAudit: function () {
                openPluginAdminRoute(auditRoute);
            },
            onScoreFilter: function (value) {
                window.PersonalRatingsBrowseState.setScoreFilter(state, value);
                state.needsReload = true;
                safeLoad(page);
            },
            onPlayedFilter: function (value) {
                window.PersonalRatingsBrowseState.setPlayedFilter(state, value);
                state.needsReload = true;
                safeLoad(page);
            },
            onMediaType: function (value) {
                window.PersonalRatingsBrowseState.setMediaType(state, value);
                state.needsReload = true;
                safeLoad(page);
            },
            onSort: function (value) {
                window.PersonalRatingsBrowseState.setSortValue(state, value);
                state.needsReload = true;
                safeLoad(page);
            },
            onTagMatchMode: function (value) {
                window.PersonalRatingsBrowseState.setTagMatchMode(state, value);
                state.needsReload = true;
                safeLoad(page);
            },
            onSearch: function (value) {
                window.PersonalRatingsBrowseState.setSearch(state, value);
                state.needsReload = true;
                safeLoad(page);
            },
            onClearFilters: function () {
                window.PersonalRatingsBrowseState.setScoreFilter(state, 'rated');
                window.PersonalRatingsBrowseState.setPlayedFilter(state, 'all');
                window.PersonalRatingsBrowseState.setMediaType(state, 'all');
                window.PersonalRatingsBrowseState.setSortValue(state, 'ratedAt:desc');
                window.PersonalRatingsBrowseState.clearSearch(state);
                state.tagIds = [];
                window.PersonalRatingsBrowseState.setTagMatchMode(state, 'any');
                state.pageNumber = 1;
                state.needsReload = true;
                window.PersonalRatingsBrowseState.persist(state);
                window.PersonalRatingsBrowseFilters.renderTagFilters(page, state);
                syncHeaderActions(page);
                safeLoad(page);
            }
        });
    }

    function syncHeaderActions(page) {
        if (!page) {
            return;
        }

        window.PersonalRatingsBrowseFilters.syncHeaderActions(page, state);
    }

    function openPluginAdminRoute(targetRoute) {
        clearPendingNativeTabTarget();
        clearSyncTimers();
        setBrowseRouteMode(false);
        destroyPage();
        restoreHeaderTabsMarkupIfNeeded(false);
        cleanupDuplicateNavEntries(findPrimaryHeaderTabsHost());
        updateNavState();
        window.PersonalRatingsBrowseApi.navigateTo(targetRoute);
        scheduleSyncBurst();
    }

    function updateActivePageOffset() {
        var page = document.getElementById(pageId);
        if (page) {
            updatePageOffset(page);
            if (updateResponsivePageSize(page)) {
                safeLoad(page);
            }
        }
    }

    function updatePageOffset(page) {
        var header = document.querySelector('.skinHeader');
        var topOffset = 64;
        if (header && typeof header.getBoundingClientRect === 'function') {
            var bounds = header.getBoundingClientRect();
            if (bounds && bounds.bottom > 0) {
                topOffset = Math.ceil(bounds.bottom);
            }
        }

        page.style.top = topOffset + 'px';
    }

    function showPage(page) {
        page.classList.add('is-active');
        page.setAttribute('aria-hidden', 'false');
        restorePageScroll(page);
    }

    function destroyPage() {
        var page = document.getElementById(pageId);
        if (!page) {
            return;
        }

        window.PersonalRatingsBrowseState.setScrollTop(state, page.scrollTop || 0);
        state.requestVersion += 1;
        state.isLoading = false;
        state.isTagLoading = false;
        state.needsReload = true;
        page.remove();
    }

    function clearPendingNativeTabTarget() {
        pendingNativeTabTarget = null;
    }

    function renderMessageState(page, summaryText, message, type) {
        page.querySelector('.personalRatingsBrowseSummaryText').textContent = summaryText;
        page.querySelector('.personalRatingsBrowseCards').innerHTML = '<div class="personalRatingsBrowseEmpty">' + window.PersonalRatingsBrowseRenderer.escapeHtml(message) + '</div>';
        page.querySelector('.personalRatingsBrowsePageText').textContent = '第 1 / 1 页';
        page.querySelector('.personalRatingsBrowsePrevButton').disabled = true;
        page.querySelector('.personalRatingsBrowseNextButton').disabled = true;
        window.PersonalRatingsBrowseRenderer.setStatus(page, message, type);
        syncHeaderActions(page);
    }

    function safeLoad(page) {
        if (!isBrowseRoute()) {
            return;
        }

        if (state.isLoading || state.isTagLoading) {
            return;
        }

        if (!state.tagsLoaded) {
            renderMessageState(page, '正在准备标签筛选...', '正在准备标签筛选...', 'loading');
            loadTags(page).finally(function () {
                if (isBrowseRoute()) {
                    safeLoad(page);
                }
            });
            return;
        }

        if (!state.needsReload && state.lastResult && !state.lastLoadFailed) {
            window.PersonalRatingsBrowseRenderer.renderResults(page, state);
            window.PersonalRatingsBrowseRenderer.setStatus(page, '', null);
            syncHeaderActions(page);
            restorePageScroll(page);
            return;
        }

        if (state.lastResult && !state.lastLoadFailed) {
            window.PersonalRatingsBrowseRenderer.renderResults(page, state);
            window.PersonalRatingsBrowseRenderer.setStatus(page, '正在同步最新结果...', 'loading');
            syncHeaderActions(page);
            restorePageScroll(page);
            loadResults(page, true);
            return;
        }

        loadResults(page, false);
    }

    function loadTags(page) {
        if (state.isTagLoading) {
            return Promise.resolve();
        }

        state.isTagLoading = true;
        return window.PersonalRatingsBrowseApi.getTags().then(function (result) {
            window.PersonalRatingsBrowseState.setTags(state, Array.isArray(result) ? result : []);
        }).catch(function () {
            window.PersonalRatingsBrowseState.setTags(state, []);
        }).finally(function () {
            state.isTagLoading = false;
            window.PersonalRatingsBrowseFilters.renderTagFilters(page, state);
        });
    }

    function loadResults(page, preserveVisibleState) {
        if (!isBrowseRoute()) {
            return;
        }

        state.isLoading = true;
        state.requestVersion += 1;
        var requestVersion = state.requestVersion;
        if (!preserveVisibleState) {
            renderMessageState(page, '正在加载打分库...', '正在加载打分库...', 'loading');
        }

        window.PersonalRatingsBrowseApi.queryRatings(window.PersonalRatingsBrowseState.buildQueryRequest(state)).then(function (result) {
            if (requestVersion !== state.requestVersion) {
                return;
            }

            state.lastLoadFailed = false;
            state.needsReload = false;
            window.PersonalRatingsBrowseState.setResult(state, result);
            window.PersonalRatingsBrowseRenderer.renderResults(page, state);
            window.PersonalRatingsBrowseRenderer.setStatus(page, '打分库已刷新。', 'success');
            syncHeaderActions(page);
            restorePageScroll(page);
        }).catch(function () {
            if (requestVersion !== state.requestVersion) {
                return;
            }

            state.lastLoadFailed = true;
            state.needsReload = false;
            window.PersonalRatingsBrowseState.setResult(state, {
                Items: [],
                TotalCount: 0,
                PageNumber: state.pageNumber,
                PageSize: state.pageSize
            });
            window.PersonalRatingsBrowseRenderer.renderResults(page, state);
            window.PersonalRatingsBrowseRenderer.setStatus(page, '加载打分库失败。', 'error');
            syncHeaderActions(page);
            restorePageScroll(page);
        }).finally(function () {
            if (requestVersion === state.requestVersion) {
                state.isLoading = false;
            }
        });
    }

    function updateResponsivePageSize(page) {
        if (!page) {
            return false;
        }

        var desiredPageSize = calculateResponsivePageSize(page);
        if (!desiredPageSize || desiredPageSize === state.pageSize) {
            return false;
        }

        var firstVisibleIndex = Math.max(0, (Math.max(state.pageNumber, 1) - 1) * Math.max(state.pageSize, 1));
        state.pageSize = desiredPageSize;
        state.pageNumber = Math.max(1, Math.floor(firstVisibleIndex / desiredPageSize) + 1);
        state.needsReload = true;
        return true;
    }

    function calculateResponsivePageSize(page) {
        if (state.viewMode === 'list') {
            return 20;
        }

        var results = page.querySelector('.personalRatingsBrowseResults');
        if (!results) {
            return state.pageSize;
        }

        var styles = window.getComputedStyle(results);
        var horizontalPadding = (parseFloat(styles.paddingLeft) || 0) + (parseFloat(styles.paddingRight) || 0);
        var contentWidth = results.clientWidth - horizontalPadding;
        if (!(contentWidth > 0)) {
            return state.pageSize;
        }

        var columns = Math.max(1, Math.floor((contentWidth + posterCardGap) / (posterCardMinWidth + posterCardGap)));
        return columns * posterRowsPerPage;
    }

    function restorePageScroll(page) {
        if (!page) {
            return;
        }

        window.requestAnimationFrame(function () {
            page.scrollTop = state.scrollTop || 0;
        });
    }
})();

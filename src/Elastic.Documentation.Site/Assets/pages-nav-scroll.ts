/** Latest pages-nav aside / scrollport for viewport clamp + edge fades. */
let scrollViewportAside: HTMLElement | null = null
let scrollViewportScrollEl: HTMLElement | null = null
let scrollViewportWindowBound = false

function getNavScrollOverflow(scrollEl: HTMLElement) {
    const { scrollTop, scrollHeight, clientHeight } = scrollEl
    const maxScroll = scrollHeight - clientHeight
    const eps = 1
    const canScroll = maxScroll > eps
    return {
        canScrollUp: canScroll && scrollTop > eps,
        canScrollDown: canScroll && scrollTop < maxScroll - eps,
    }
}

function findNavScrollButtons(scrollEl: HTMLElement) {
    const menu =
        scrollEl.closest<HTMLElement>('.pages-nav-v2__menu') ??
        scrollEl.parentElement
    const upBtn =
        menu?.querySelector<HTMLButtonElement>(
            ':scope > .pages-nav-v2__scroll-btn--up'
        ) ?? null
    const downBtn =
        menu?.querySelector<HTMLButtonElement>(
            ':scope > .pages-nav-v2__scroll-btn--down'
        ) ?? null
    return { upBtn, downBtn }
}

function updateNavScrollFades(scrollEl: HTMLElement) {
    const { canScrollUp, canScrollDown } = getNavScrollOverflow(scrollEl)
    scrollEl.dataset.navFadeTop = canScrollUp ? 'true' : 'false'
    scrollEl.dataset.navFadeBottom = canScrollDown ? 'true' : 'false'

    const { upBtn, downBtn } = findNavScrollButtons(scrollEl)
    if (upBtn) {
        upBtn.dataset.visible = canScrollUp ? 'true' : 'false'
    }
    if (downBtn) {
        downBtn.dataset.visible = canScrollDown ? 'true' : 'false'
    }
}

function scrollNavByPage(scrollEl: HTMLElement, direction: 'up' | 'down') {
    const delta = Math.max(120, Math.round(scrollEl.clientHeight * 0.75))
    scrollEl.scrollBy({
        top: direction === 'up' ? -delta : delta,
        behavior: 'smooth',
    })
}

function findSiteFooter(): HTMLElement | null {
    return (
        document.querySelector<HTMLElement>('footer.bg-ink-dark') ??
        document.querySelector<HTMLElement>('body > footer:last-of-type')
    )
}

function getOffsetTopPx() {
    const raw = getComputedStyle(document.documentElement)
        .getPropertyValue('--offset-top')
        .trim()
    const parsed = Number.parseFloat(raw)
    return Number.isFinite(parsed) ? parsed : 48
}

/**
 * Clamp the sticky host to the visible strip under the topbar → viewport
 * bottom or footer. Sticky top is --offset-top only; the 24px inset is padding
 * inside the host so it does not get added to the offset.
 */
function updatePagesNavAsideViewportHeight(aside: HTMLElement) {
    if (!window.matchMedia('(width >= 768px)').matches) {
        aside.style.removeProperty('--pages-nav-aside-height')
        return
    }

    const stickyTop = getOffsetTopPx()
    const layoutTop = aside.getBoundingClientRect().top
    const top = Number.isFinite(layoutTop)
        ? Math.max(stickyTop, Math.round(layoutTop))
        : stickyTop
    let bottom = window.innerHeight
    const footer = findSiteFooter()
    if (footer) {
        const footerTop = footer.getBoundingClientRect().top
        if (footerTop < bottom) {
            bottom = footerTop
        }
    }

    const height = Math.max(0, Math.round(bottom - top))
    aside.style.setProperty('--pages-nav-aside-height', `${height}px`)
}

function refreshNavScrollViewport() {
    const aside = scrollViewportAside
    const scrollEl = scrollViewportScrollEl
    if (!aside || !scrollEl) {
        return
    }

    updatePagesNavAsideViewportHeight(aside)
    updateNavScrollFades(scrollEl)
}

/**
 * Fades and optional scroll buttons on `.pages-nav-v2-shell`.
 * Does not require `data-nav-v2`.
 */
export function initPagesNavScroll(nav: HTMLElement) {
    const shell =
        nav.querySelector('.pages-nav-v2-shell') ??
        nav.closest('.pages-nav-v2-shell')
    const scrollEl = shell?.querySelector<HTMLElement>('.pages-nav-v2__scroll')
    const aside =
        nav.closest<HTMLElement>('aside.sidebar') ??
        document.querySelector<HTMLElement>('aside.sidebar:has(#pages-nav)')
    if (!scrollEl || !aside) {
        return
    }

    scrollViewportAside = aside
    scrollViewportScrollEl = scrollEl

    if (!scrollViewportWindowBound) {
        scrollViewportWindowBound = true
        window.addEventListener('scroll', refreshNavScrollViewport, {
            passive: true,
        })
        window.addEventListener('resize', refreshNavScrollViewport, {
            passive: true,
        })
    }

    if (scrollEl.dataset.navScrollInit !== 'true') {
        scrollEl.dataset.navScrollInit = 'true'
        scrollEl.addEventListener(
            'scroll',
            () => updateNavScrollFades(scrollEl),
            { passive: true }
        )
        shell?.addEventListener('change', refreshNavScrollViewport)

        const { upBtn, downBtn } = findNavScrollButtons(scrollEl)
        if (upBtn && upBtn.dataset.navScrollBound !== 'true') {
            upBtn.dataset.navScrollBound = 'true'
            upBtn.addEventListener('click', () =>
                scrollNavByPage(scrollEl, 'up')
            )
        }
        if (downBtn && downBtn.dataset.navScrollBound !== 'true') {
            downBtn.dataset.navScrollBound = 'true'
            downBtn.addEventListener('click', () =>
                scrollNavByPage(scrollEl, 'down')
            )
        }

        const content = scrollEl.querySelector('.pages-nav-v2__content')
        if (content) {
            const mo = new MutationObserver(refreshNavScrollViewport)
            mo.observe(content, {
                childList: true,
                subtree: true,
                attributes: true,
                attributeFilter: ['class', 'style', 'open'],
            })
        }

        const ro = new ResizeObserver(refreshNavScrollViewport)
        ro.observe(aside)
        ro.observe(scrollEl)
        const elasticNav =
            document.querySelector<HTMLElement>('#elastic-nav') ??
            document.querySelector<HTMLElement>('#elastic-nav-wrapper')
        if (elasticNav) {
            ro.observe(elasticNav)
        }
    }

    refreshNavScrollViewport()
    requestAnimationFrame(refreshNavScrollViewport)
}

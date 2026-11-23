// Scroll-spy for the "On this page" outline: marks the link of the heading in view with aria-current="location".
// The outline works as plain anchor links without this.
const activationOffset = 0.25
// Within this many pixels of the page bottom the last heading is current: a short last section can never reach the line.
const bottomSlack = 4
// A sticky outline marks a heading once it is this far below the outline's own bottom edge, which is where a click lands it.
const stickySlack = 32

function isInView(element: Element): boolean {
    const top = element.getBoundingClientRect().top
    return top >= 0 && top < window.innerHeight
}

// A heading's slug can hold a literal percent sign ("100% coverage"), which decodeURIComponent rejects. The raw text is the id then.
export function fragmentId(hash: string): string {
    const raw = hash.slice(1)
    try {
        return decodeURIComponent(raw)
    } catch {
        return raw
    }
}

// `selector` narrows which outlines to bind: the production bundle owns only the operation page's section links.
export function initToc(scope: ParentNode = document, selector = '[data-toc]') {
    scope
        .querySelectorAll<HTMLElement>(`${selector}:not([data-toc-bound])`)
        .forEach((toc) => {
            toc.dataset.tocBound = ''
            const links = Array.from(
                toc.querySelectorAll<HTMLAnchorElement>('[data-toc-link]')
            )
            const targets = links.map((link) => ({
                link,
                heading: document.getElementById(fragmentId(link.hash)),
            }))

            const listeners = new AbortController()
            let queued = false
            const update = () => {
                queued = false
                // An htmx swap replaced the page: stop tracking its outline
                if (!toc.isConnected) {
                    listeners.abort()
                    return
                }
                const sticky = toc.hasAttribute('data-toc-sticky')
                const rect = sticky ? toc.getBoundingClientRect() : null
                // A sticky outline at its sticky top has pinned: the page can show more in it then.
                const pinned =
                    rect !== null &&
                    rect.top <= parseFloat(getComputedStyle(toc).top) + 1
                const line = rect
                    ? rect.bottom + stickySlack
                    : window.innerHeight * activationOffset
                let active: HTMLAnchorElement | null = null
                for (const { link, heading } of targets) {
                    if (
                        heading &&
                        heading.getBoundingClientRect().top <= line
                    ) {
                        active = link
                    }
                }
                const atBottom =
                    window.innerHeight + window.scrollY >=
                    document.documentElement.scrollHeight - bottomSlack
                if (atBottom) {
                    // A page too short to scroll cannot bring a clicked section to the line: the section the address
                    // names, if it is in view, is the one the reader chose; otherwise the last one is.
                    const chosen = targets.find(
                        ({ link, heading }) =>
                            heading &&
                            link.hash === window.location.hash &&
                            isInView(heading)
                    )
                    for (const { link, heading } of targets) {
                        if (heading) active = chosen?.link ?? link
                    }
                }
                // A sticky bar can be on screen above every section, so until one reaches the line none is current.
                if (!sticky) active ??= targets[0]?.link ?? null
                // Written after every read, so the frame lays the page out once.
                if (sticky) toc.toggleAttribute('data-pinned', pinned)
                for (const { link } of targets) {
                    if (link === active) {
                        link.setAttribute('aria-current', 'location')
                    } else {
                        link.removeAttribute('aria-current')
                    }
                }
            }
            const schedule = () => {
                if (queued) return
                queued = true
                window.requestAnimationFrame(update)
            }

            window.addEventListener('scroll', schedule, {
                passive: true,
                signal: listeners.signal,
            })
            window.addEventListener('resize', schedule, {
                signal: listeners.signal,
            })
            window.addEventListener('hashchange', schedule, {
                signal: listeners.signal,
            })
            // A link click that pushes the address emits no hashchange, and a page too short to scroll emits no scroll
            // either. The update waits a frame, so the click handlers have set the address by then.
            toc.addEventListener('click', schedule, {
                signal: listeners.signal,
            })
            window.addEventListener('popstate', schedule, {
                signal: listeners.signal,
            })
            update()
        })
}

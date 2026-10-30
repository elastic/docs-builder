// Scroll-spy for the "On this page" outline: marks the link of the heading in view with aria-current="location".
// The outline works as plain anchor links without this.
const activationOffset = 0.25
// Within this many pixels of the page bottom the last heading is current: a short last section can never reach the line.
const bottomSlack = 4

export function initToc(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLElement>('[data-toc]:not([data-toc-bound])')
        .forEach((toc) => {
            toc.dataset.tocBound = ''
            const links = Array.from(
                toc.querySelectorAll<HTMLAnchorElement>('[data-toc-link]')
            )
            const targets = links.map((link) => ({
                link,
                heading: document.getElementById(
                    decodeURIComponent(link.hash.slice(1))
                ),
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
                const line = window.innerHeight * activationOffset
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
                    for (const { link, heading } of targets) {
                        if (heading) active = link
                    }
                }
                active ??= targets[0]?.link ?? null
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
            update()
        })
}

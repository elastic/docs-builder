// Previous and next controls for a scroll-snap carousel. The slides scroll with touch, wheel and keyboard on their own;
// the controls are rendered hidden and revealed here because they only add convenience.
export function initCarousels(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLElement>(
            '[data-carousel]:not([data-carousel-bound])'
        )
        .forEach((root) => {
            root.dataset.carouselBound = ''
            const track = root.querySelector<HTMLElement>(
                '[data-carousel-track]'
            )
            const controls = root.querySelector<HTMLElement>(
                '[data-carousel-controls]'
            )
            if (!track || !controls) return

            const dots = Array.from(
                root.querySelectorAll<HTMLElement>('[data-carousel-dot]')
            )
            const slideWidth = () => track.clientWidth || 1
            const current = () =>
                Math.min(
                    dots.length - 1,
                    Math.max(0, Math.round(track.scrollLeft / slideWidth()))
                )
            const goTo = (index: number) => {
                const clamped = Math.min(dots.length - 1, Math.max(0, index))
                track.scrollTo({ left: clamped * slideWidth() })
            }
            const sync = () =>
                dots.forEach((dot, i) =>
                    dot.setAttribute('aria-current', String(i === current()))
                )

            root.querySelector('[data-carousel-prev]')?.addEventListener(
                'click',
                () => goTo(current() - 1)
            )
            root.querySelector('[data-carousel-next]')?.addEventListener(
                'click',
                () => goTo(current() + 1)
            )
            dots.forEach((dot, i) =>
                dot.addEventListener('click', () => goTo(i))
            )
            track.addEventListener('scroll', sync, { passive: true })
            controls.hidden = false
        })
}

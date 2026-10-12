const STACK_WIDTH = 720
const observers = new WeakMap<HTMLElement, ResizeObserver>()

// Draws the wrap line from the point of one chevron row into the next.
// The line is a position calculation, so it stays out of the markup.
export function initGetStarted() {
    document
        .querySelectorAll<HTMLElement>('.hub-get-started-track')
        .forEach((track) => {
            draw(track)
            if (observers.has(track)) return

            const observer = new ResizeObserver(() => {
                if (!track.isConnected) {
                    observer.disconnect()
                    observers.delete(track)
                    return
                }
                draw(track)
            })
            observer.observe(track)
            observers.set(track, observer)
        })
}

function draw(track: HTMLElement) {
    track.querySelector(':scope > .hub-get-started-track-svg')?.remove()
    if (track.clientWidth <= STACK_WIDTH) return

    const rows = [
        ...track.querySelectorAll<HTMLElement>(':scope > .hub-get-started-row'),
    ]
    if (rows.length < 2) return

    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg')
    svg.classList.add('hub-get-started-track-svg')
    svg.setAttribute('aria-hidden', 'true')
    const box = track.getBoundingClientRect()
    const stroke =
        getComputedStyle(document.documentElement)
            .getPropertyValue('--color-blue-midnight')
            .trim() || '#20377d'

    rows.slice(0, -1).forEach((row, index) => {
        const from = row.querySelector<HTMLElement>(
            ':scope > .hub-get-started-step:last-child'
        )
        const to = rows[index + 1].querySelector<HTMLElement>(
            ':scope > .hub-get-started-step'
        )
        if (!from || !to) return

        const fromBox = from.getBoundingClientRect()
        const toBox = to.getBoundingClientRect()
        const x1 = fromBox.right - box.left
        const y1 = fromBox.top + fromBox.height / 2 - box.top
        const x2 = toBox.left - box.left
        const y2 = toBox.top + toBox.height / 2 - box.top
        const right = x1 + 22
        const left = x2 - 36
        const gapY = (fromBox.bottom + toBox.top) / 2 - box.top

        const path = document.createElementNS(
            'http://www.w3.org/2000/svg',
            'path'
        )
        path.setAttribute(
            'd',
            `M ${x1} ${y1} H ${right} V ${gapY} H ${left} V ${y2} H ${x2 - 16}`
        )
        path.setAttribute('fill', 'none')
        path.setAttribute('stroke', stroke)
        path.setAttribute('stroke-width', '1.5')
        path.setAttribute('stroke-linejoin', 'miter')
        svg.append(path)

        const cx = x2 - 8
        const circle = document.createElementNS(
            'http://www.w3.org/2000/svg',
            'circle'
        )
        circle.setAttribute('cx', String(cx))
        circle.setAttribute('cy', String(y2))
        circle.setAttribute('r', '11')
        circle.setAttribute('fill', '#ffffff')
        circle.setAttribute('stroke', stroke)
        circle.setAttribute('stroke-width', '1.5')
        svg.append(circle)

        const arrow = document.createElementNS(
            'http://www.w3.org/2000/svg',
            'path'
        )
        arrow.setAttribute(
            'd',
            `M ${cx - 4} ${y2 - 4} L ${cx + 3} ${y2} L ${cx - 4} ${y2 + 4}`
        )
        arrow.setAttribute('fill', 'none')
        arrow.setAttribute('stroke', stroke)
        arrow.setAttribute('stroke-width', '1.5')
        arrow.setAttribute('stroke-linejoin', 'miter')
        arrow.setAttribute('stroke-linecap', 'round')
        svg.append(arrow)
    })

    track.prepend(svg)
}

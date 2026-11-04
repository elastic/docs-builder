/**
 * Beside the article the examples rail is as tall as the window. The chips, headers and captions keep their
 * height, and the room left goes to the request and response code: the shorter one shows in full, the longer one
 * gets the rest, and two long ones split the room evenly.
 */
import {
    stripChrome,
    syncStripHeight,
    visibleCarousel,
    visiblePanel,
} from './api-examples-carousel'

const wide = '(min-width: 1024px)'
// Out of the 16 lines the request cap holds: no sample gets less than this, unless it is shorter.
const minLines = 5
const capLines = 16

export interface CodeHeights {
    request: number
    response: number
}

/** `floor` is the least either sample gets, unless it is shorter than that. */
export function balanceCodeHeights(
    room: number,
    natural: CodeHeights,
    floor: number
): CodeHeights {
    const { request, response } = natural
    const half = Math.max(room, 0) / 2
    const shared =
        request + response <= room
            ? natural
            : request <= half
              ? { request, response: room - request }
              : response <= half
                ? { request: room - response, response }
                : { request: half, response: half }
    return {
        request: Math.round(Math.max(shared.request, Math.min(request, floor))),
        response: Math.round(
            Math.max(shared.response, Math.min(response, floor))
        ),
    }
}

function height(element: HTMLElement | null | undefined) {
    return element?.getBoundingClientRect().height ?? 0
}

function balance(panel: HTMLElement) {
    if (!window.matchMedia?.(wide).matches) {
        panel.style.removeProperty('--api-request-code-max')
        panel.style.removeProperty('--api-response-code-max')
        return
    }
    const examples = panel.querySelector<HTMLElement>('[data-api-examples]')
    if (!examples) return
    const carousel = visibleCarousel(examples)
    const strip = carousel?.querySelector<HTMLElement>('[data-carousel-strip]')
    const card = carousel?.querySelector<HTMLElement>(
        '.api-code-carousel-card.is-active'
    )
    const requestBody = card?.querySelector<HTMLElement>(
        '.api-code-carousel-card-body'
    )
    const responseBody = (
        visiblePanel(examples) ?? examples
    ).querySelector<HTMLElement>('.example-block--response .example-block-body')
    if (!carousel || !strip || !card || !requestBody) return

    const cap = parseFloat(
        getComputedStyle(panel).getPropertyValue('--api-code-cap')
    )
    const natural = {
        request: Number.isFinite(cap)
            ? Math.min(requestBody.scrollHeight, cap)
            : requestBody.scrollHeight,
        response: responseBody?.scrollHeight ?? 0,
    }
    const panelTop = panel.getBoundingClientRect().top - panel.scrollTop
    const content = examples.getBoundingClientRect().bottom - panelTop
    // Measured around the strip, not its code: the strip animates its height, so its code can be mid-change.
    const chrome =
        content -
        height(strip) +
        stripChrome(strip, card) -
        height(responseBody)
    const room = panel.clientHeight - chrome
    const floor = Number.isFinite(cap) ? (cap * minLines) / capLines : 0
    const next = balanceCodeHeights(room, natural, floor)

    panel.style.setProperty('--api-request-code-max', `${next.request}px`)
    panel.style.setProperty('--api-response-code-max', `${next.response}px`)
    syncStripHeight(carousel)
}

/** Balances every examples rail in `root` and keeps it balanced as the window, scenario, tab or caption changes. */
export function initExamplesBalance(root: ParentNode = document): void {
    root.querySelectorAll<HTMLElement>('#api-examples-panel').forEach(
        (panel) => {
            if (panel.dataset.balanced) return
            panel.dataset.balanced = 'true'
            let frame = 0
            const schedule = () => {
                cancelAnimationFrame(frame)
                frame = requestAnimationFrame(() => balance(panel))
            }
            schedule()
            if (typeof ResizeObserver === 'undefined') return
            const resize = new ResizeObserver(schedule)
            resize.observe(panel)
            const examples = panel.querySelector('[data-api-examples]')
            if (examples) resize.observe(examples)
        }
    )
}

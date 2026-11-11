// Drag handles on the API pages' three columns: sidebar | reference | examples rail. Progressive enhancement: the handles are
// created here, so without script the columns keep the widths the stylesheet gives them. Both layouts are covered because they
// size the same two columns through different custom properties.

type Side = 'nav' | 'rail'

interface ColumnSpec {
    side: Side
    cssVar: string
    min: number
    max: number
}

interface Variant {
    navSelector: string
    columns: ColumnSpec[]
}

const designSystem: Variant = {
    navSelector: '.layout__nav',
    columns: [
        { side: 'nav', cssVar: '--layout-nav-width', min: 240, max: 560 },
        { side: 'rail', cssVar: '--layout-rail-width', min: 280, max: 900 },
    ],
}

const legacy: Variant = {
    navSelector: ':scope > div > aside',
    columns: [
        { side: 'nav', cssVar: '--max-sidebar-width', min: 240, max: 560 },
        { side: 'rail', cssVar: '--max-examples-width', min: 280, max: 900 },
    ],
}

const storageKey = 'api-column-widths'
const keyStep = 24
const contentMin = 360
const columnGap = 32

type Widths = Partial<Record<Side, number>>

// Storage can be blocked, and a throw here would stop the initializers that run after this one. The widths then just are not kept.
function readWidths(): Widths {
    try {
        const parsed = JSON.parse(localStorage.getItem(storageKey) ?? '{}')
        return typeof parsed === 'object' && parsed !== null ? parsed : {}
    } catch {
        return {}
    }
}

function writeWidths(widths: Widths) {
    try {
        if (Object.keys(widths).length === 0)
            localStorage.removeItem(storageKey)
        else localStorage.setItem(storageKey, JSON.stringify(widths))
    } catch {
        // Resizing still works for this page.
    }
}

export function clampWidth(width: number, min: number, max: number): number {
    return Math.round(Math.min(Math.max(width, min), max))
}

// Boosted navigation swaps the whole shell, so the window listener and the observer of the shell it replaced must go.
let active: {
    shell: HTMLElement
    place: () => void
    dispose: () => void
} | null = null

export function initApiColumnResize() {
    const shell = document.querySelector<HTMLElement>('.api-page-shell')
    if (!shell) return
    if (active?.shell === shell) {
        active.place()
        return
    }
    active?.dispose()
    const variant = shell.classList.contains('layout--three')
        ? designSystem
        : legacy

    // The rail sits inside the region htmx swaps, so the columns are looked up each time rather than kept.
    const columnEl = (side: Side) =>
        shell.querySelector<HTMLElement>(
            side === 'nav' ? variant.navSelector : '.api-page-rail'
        )

    const widths = readWidths()
    const apply = (spec: ColumnSpec, width: number) => {
        shell.style.setProperty(spec.cssVar, `${width}px`)
        widths[spec.side] = width
    }
    for (const spec of variant.columns) {
        const saved = widths[spec.side]
        if (typeof saved === 'number' && Number.isFinite(saved))
            apply(spec, clampWidth(saved, spec.min, spec.max))
    }

    // How far a column can still grow before the reference would drop under contentMin.
    const room = (): number => {
        const nav = columnEl('nav')?.getBoundingClientRect()
        const rail = columnEl('rail')?.getBoundingClientRect()
        if (!nav || !rail) return 0
        const inner =
            shell.getBoundingClientRect().right -
            (parseFloat(getComputedStyle(shell).paddingRight) || 0)
        const used = nav.width + rail.width + 2 * columnGap + contentMin
        return Math.max(0, inner - nav.left - used)
    }

    const handles = new Map<Side, HTMLElement>()

    const place = () => {
        const origin = shell.getBoundingClientRect().left
        for (const spec of variant.columns) {
            const handle = handles.get(spec.side)!
            const rect = columnEl(spec.side)?.getBoundingClientRect()
            handle.hidden = !rect
            if (!rect) continue
            const edge =
                spec.side === 'nav' ? rect.right : rect.left - columnGap / 2
            handle.style.left = `${Math.round(edge - origin)}px`
            handle.setAttribute('aria-valuenow', String(Math.round(rect.width)))
        }
    }

    const resize = (
        spec: ColumnSpec,
        start: number,
        grow: number,
        limit: number
    ) => {
        const max = Math.min(spec.max, start + limit)
        apply(spec, clampWidth(start + grow, spec.min, max))
        place()
    }

    for (const spec of variant.columns) {
        const handle = document.createElement('div')
        handle.className = 'api-column-handle'
        handle.dataset.apiColumnHandle = spec.side
        handle.setAttribute('role', 'separator')
        handle.setAttribute('aria-orientation', 'vertical')
        handle.setAttribute(
            'aria-label',
            spec.side === 'nav' ? 'Resize sidebar' : 'Resize examples'
        )
        handle.setAttribute('aria-valuemin', String(spec.min))
        handle.setAttribute('aria-valuemax', String(spec.max))
        handle.tabIndex = 0
        handles.set(spec.side, handle)
        shell.append(handle)

        const sign = spec.side === 'nav' ? 1 : -1

        handle.addEventListener('pointerdown', (event) => {
            event.preventDefault()
            const startX = event.clientX
            const start =
                columnEl(spec.side)?.getBoundingClientRect().width ?? 0
            const limit = room()
            handle.setPointerCapture?.(event.pointerId)
            handle.dataset.dragging = ''
            document.body.dataset.apiColumnDragging = ''
            const move = (e: PointerEvent) =>
                resize(spec, start, sign * (e.clientX - startX), limit)
            const stop = () => {
                handle.removeEventListener('pointermove', move)
                handle.removeEventListener('pointerup', stop)
                handle.removeEventListener('pointercancel', stop)
                delete handle.dataset.dragging
                delete document.body.dataset.apiColumnDragging
                writeWidths(widths)
            }
            handle.addEventListener('pointermove', move)
            handle.addEventListener('pointerup', stop)
            handle.addEventListener('pointercancel', stop)
        })

        handle.addEventListener('keydown', (event) => {
            const step =
                event.key === 'ArrowLeft'
                    ? -keyStep
                    : event.key === 'ArrowRight'
                      ? keyStep
                      : 0
            const start = columnEl(spec.side)?.getBoundingClientRect().width
            if (step === 0 || !start) return
            event.preventDefault()
            resize(spec, start, sign * step, room())
            writeWidths(widths)
        })

        // Double click gives the column back to the stylesheet.
        handle.addEventListener('dblclick', () => {
            shell.style.removeProperty(spec.cssVar)
            delete widths[spec.side]
            writeWidths(widths)
            place()
        })
    }

    const observer = new ResizeObserver(place)
    observer.observe(shell)
    window.addEventListener('resize', place)
    active = {
        shell,
        place,
        dispose: () => {
            observer.disconnect()
            window.removeEventListener('resize', place)
        },
    }
    place()
}

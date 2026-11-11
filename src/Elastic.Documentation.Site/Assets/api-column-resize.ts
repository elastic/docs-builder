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
    // Where the stylesheet shows the handles. The saved widths apply only there, so a narrow window never keeps a width it cannot undo.
    wide: string
    columns: ColumnSpec[]
}

const designSystem: Variant = {
    navSelector: '.layout__nav',
    wide: '(min-width: 1101px)',
    columns: [
        { side: 'nav', cssVar: '--layout-nav-width', min: 240, max: 560 },
        { side: 'rail', cssVar: '--layout-rail-width', min: 280, max: 900 },
    ],
}

const legacy: Variant = {
    navSelector: ':scope > div > aside',
    wide: '(min-width: 1280px)',
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
    sync: () => void
    dispose: () => void
} | null = null

export function initApiColumnResize() {
    const shell = document.querySelector<HTMLElement>('.api-page-shell')
    if (!shell) return
    if (active?.shell === shell) {
        active.sync()
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
    const isSaved = (side: Side) =>
        typeof widths[side] === 'number' && Number.isFinite(widths[side])

    // How far the columns can still grow before the reference would drop under contentMin. Negative when they already crowd it.
    const slack = (): number => {
        const nav = columnEl('nav')?.getBoundingClientRect()
        const rail = columnEl('rail')?.getBoundingClientRect()
        if (!nav || !rail) return 0
        const inner =
            shell.getBoundingClientRect().right -
            (parseFloat(getComputedStyle(shell).paddingRight) || 0)
        const used = nav.width + rail.width + 2 * columnGap + contentMin
        return inner - nav.left - used
    }
    const room = () => Math.max(0, slack())

    // Saved widths come from whatever window the reader had then. Show them only where the handles are, and shrink them, rail first,
    // until the reference has its minimum again. The saved values stay as they are for the next wide window.
    const reconcile = () => {
        const wide = window.matchMedia?.(variant.wide).matches ?? true
        for (const spec of variant.columns) {
            if (wide && isSaved(spec.side))
                shell.style.setProperty(
                    spec.cssVar,
                    `${clampWidth(widths[spec.side]!, spec.min, spec.max)}px`
                )
            else shell.style.removeProperty(spec.cssVar)
        }
        let over = wide ? -slack() : 0
        for (const spec of [...variant.columns].reverse()) {
            const width = columnEl(spec.side)?.getBoundingClientRect().width
            if (over <= 0 || !width || !isSaved(spec.side)) continue
            const cut = Math.min(over, Math.max(0, width - spec.min))
            if (cut <= 0) continue
            shell.style.setProperty(spec.cssVar, `${Math.round(width - cut)}px`)
            over -= cut
        }
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

    const sync = () => {
        reconcile()
        place()
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

    const observer = new ResizeObserver(sync)
    observer.observe(shell)
    window.addEventListener('resize', sync)
    active = {
        shell,
        sync,
        dispose: () => {
            observer.disconnect()
            window.removeEventListener('resize', sync)
        },
    }
    sync()
}

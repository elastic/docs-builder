/**
 * Handles the dynamic resizing of the isolated header when scrolling.
 * Expands only at scroll position 0, compacts after scrolling past threshold.
 */
export function initIsolatedHeader() {
    const header = document.getElementById('isolated-header')
    if (!header) return

    // Add class to body for CSS scoping
    document.body.classList.add('has-isolated-header')

    let isCompact = false

    const COMPACT_THRESHOLD = 80 // Scroll down past this to compact
    const EXPANDED_OFFSET = '110px'
    const COMPACT_OFFSET = '48px'

    const updateLayout = (compact: boolean) => {
        const offset = compact ? COMPACT_OFFSET : EXPANDED_OFFSET

        // Update CSS variable - this drives header height and sidebar positioning via CSS
        document.documentElement.style.setProperty('--offset-top', offset)

        // Update header internal elements
        header.querySelectorAll<HTMLImageElement>('img').forEach((img) => {
            img.style.width = compact ? '22px' : '28px'
            img.style.height = compact ? '22px' : '28px'
        })

        header.querySelectorAll<HTMLElement>('.text-lg').forEach((el) => {
            el.style.fontSize = compact ? '0.95rem' : ''
        })
    }

    const onScroll = () => {
        const scrollY = window.scrollY

        // Only expand when at the very top (scrollY === 0)
        // Only compact when scrolled past threshold
        if (!isCompact && scrollY > COMPACT_THRESHOLD) {
            isCompact = true
            updateLayout(true)
        } else if (isCompact && scrollY === 0) {
            isCompact = false
            updateLayout(false)
        }
    }

    window.addEventListener('scroll', onScroll, { passive: true })

    // Set initial state based on current scroll position
    if (window.scrollY > 0) {
        isCompact = true
        updateLayout(true)
    } else {
        // Ensure initial expanded state is set
        updateLayout(false)
    }
}

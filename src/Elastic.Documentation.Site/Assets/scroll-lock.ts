/**
 * Stops the page behind a modal from scrolling. The lock goes on <html>, which owns the viewport scrollbar
 * whatever the page's own overflow rules say (the API pages set overflow-y on <html>, so a lock on <body> would
 * not reach the viewport there). If hiding the scrollbar widens the page, that width is padded back.
 * Returns the function that restores the page.
 */
export function lockPageScroll(): () => void {
    const root = document.documentElement
    const previous = {
        overflow: root.style.overflow,
        paddingRight: root.style.paddingRight,
    }
    const widthBefore = root.clientWidth
    root.style.overflow = 'hidden'
    const widened = root.clientWidth - widthBefore
    if (widened > 0) root.style.paddingRight = `${widened}px`
    return () => {
        root.style.overflow = previous.overflow
        root.style.paddingRight = previous.paddingRight
    }
}

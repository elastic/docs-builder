import katexCssUrl from './katex.css?url'

/**
 * Vite compiles a dynamic stylesheet import to a no-op unless its preload helper injects the
 * stylesheet, which does not happen in our build. Resolve the URL and add the <link> ourselves.
 */
export function loadKatexStylesheet(): Promise<void> {
    const existing = document.querySelector<HTMLLinkElement>(
        'link[data-katex-stylesheet]'
    )
    if (existing) return Promise.resolve()

    return new Promise((resolve) => {
        const link = document.createElement('link')
        link.rel = 'stylesheet'
        link.href = katexCssUrl
        link.setAttribute('data-katex-stylesheet', '')
        // Resolve on error too: math still renders, just unstyled.
        link.onload = () => resolve()
        link.onerror = () => resolve()
        document.head.appendChild(link)
    })
}

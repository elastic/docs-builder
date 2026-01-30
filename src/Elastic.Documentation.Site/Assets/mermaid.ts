// Mermaid.js is loaded from CDN to avoid Parcel bundling issues
// (Mermaid has complex ESM dependencies that don't resolve correctly)
declare const mermaid: {
    initialize: (config: object) => void
    render: (id: string, text: string) => Promise<{ svg: string }>
}

let mermaidLoaded = false
let mermaidLoading: Promise<void> | null = null
let diagramCounter = 0

/**
 * Lazy-load Mermaid.js from CDN only when diagrams exist on the page
 */
async function loadMermaid(): Promise<void> {
    if (mermaidLoaded) return
    if (mermaidLoading) return mermaidLoading

    mermaidLoading = new Promise((resolve, reject) => {
        const script = document.createElement('script')
        script.src =
            'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.min.js'
        script.async = true
        script.onload = () => {
            mermaidLoaded = true
            // Initialize mermaid with settings
            mermaid.initialize({
                startOnLoad: false,
                theme: 'neutral',
                securityLevel: 'strict',
                fontFamily: 'Inter, system-ui, sans-serif',
            })
            resolve()
        }
        script.onerror = () => reject(new Error('Failed to load Mermaid.js'))
        document.head.appendChild(script)
    })

    return mermaidLoading
}

/**
 * Initialize Mermaid diagram rendering for elements with class 'mermaid'
 */
export async function initMermaid() {
    const mermaidElements = document.querySelectorAll(
        'pre.mermaid:not([data-mermaid-processed])'
    )

    if (mermaidElements.length === 0) {
        return
    }

    try {
        // Lazy-load Mermaid.js from CDN only when diagrams exist
        await loadMermaid()

        // Render each diagram individually
        for (let i = 0; i < mermaidElements.length; i++) {
            const element = mermaidElements[i]
            const content = element.textContent?.trim()

            if (!content) continue

            // Mark as processed to prevent double rendering
            element.setAttribute('data-mermaid-processed', 'true')

            try {
                // Generate unique ID for this diagram
                const id = `mermaid-${++diagramCounter}`

                // Render the diagram
                // Note: Mermaid's securityLevel: 'strict' sanitizes the output
                // to prevent XSS (no scripts, event handlers, or dangerous elements)
                const { svg } = await mermaid.render(id, content)

                // Replace the pre element with a div containing the SVG
                const container = document.createElement('div')
                container.className = 'mermaid-rendered'
                container.innerHTML = svg
                element.replaceWith(container)
            } catch (err) {
                console.warn('Mermaid rendering error for diagram:', err)
                // Keep the original content as fallback
                element.classList.add('mermaid-error')
            }
        }
    } catch (error) {
        console.warn('Mermaid initialization error:', error)
    }
}

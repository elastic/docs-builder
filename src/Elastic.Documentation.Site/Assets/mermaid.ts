// Mermaid.js is loaded from local _static/ to avoid client-side CDN calls
// The file is copied from node_modules during build (see package.json copy:mermaid)
declare const mermaid: {
    initialize: (config: object) => void
    render: (id: string, text: string) => Promise<{ svg: string }>
}

let mermaidLoaded = false
let mermaidLoading: Promise<void> | null = null
let diagramCounter = 0

/**
 * Get the base path for _static/ assets by finding main.js script location
 */
function getStaticBasePath(): string {
    // Find the main.js script element to get the correct path prefix
    const scripts = document.querySelectorAll('script[src*="main.js"]')
    for (const script of scripts) {
        const src = script.getAttribute('src')
        if (src) {
            // Extract path up to and including _static/
            const match = src.match(/^(.*\/_static\/)/)
            if (match) {
                return match[1]
            }
        }
    }
    // Fallback for local development
    return '/_static/'
}

/**
 * Lazy-load Mermaid.js from local _static/ only when diagrams exist on the page
 */
async function loadMermaid(): Promise<void> {
    if (mermaidLoaded) return
    if (mermaidLoading) return mermaidLoading

    mermaidLoading = new Promise((resolve, reject) => {
        const script = document.createElement('script')
        script.src = getStaticBasePath() + 'mermaid.min.js'
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
        // Lazy-load Mermaid.js only when diagrams exist
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

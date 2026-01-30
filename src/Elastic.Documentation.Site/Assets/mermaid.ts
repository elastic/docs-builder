// Beautiful Mermaid is loaded from local _static/ to avoid client-side CDN calls
// The file is copied from node_modules during build (see package.json copy:mermaid)

// Type declaration for beautiful-mermaid browser global
declare global {
    interface Window {
        __mermaid: {
            renderMermaid: (
                code: string,
                options?: {
                    bg?: string
                    fg?: string
                    font?: string
                    transparent?: boolean
                    line?: string
                    accent?: string
                    muted?: string
                    surface?: string
                    border?: string
                }
            ) => Promise<string>
        }
    }
}

let mermaidLoaded = false
let mermaidLoading: Promise<void> | null = null

// High-contrast theme configuration
// beautiful-mermaid generates CSS variables that don't resolve correctly in all contexts,
// so we resolve them to actual colors during post-processing
const colors = {
    background: '#FFFFFF',
    foreground: '#000000',
    nodeFill: '#F5F5F5',
    nodeStroke: '#000000',
    line: '#000000',
    innerStroke: '#333333',
}

// Map CSS variables to resolved colors
const variableReplacements: Record<string, string> = {
    '--_text': colors.foreground,
    '--_text-sec': colors.foreground,
    '--_text-muted': colors.foreground,
    '--_text-faint': colors.foreground, // "+ ", ": ", "(no attributes)"
    '--_line': colors.line,
    '--_arrow': colors.foreground,
    '--_node-fill': colors.nodeFill,
    '--_node-stroke': colors.nodeStroke,
    '--_inner-stroke': colors.innerStroke,
    '--bg': colors.background,
}

/**
 * Resolve CSS variables to actual colors in the SVG output
 */
function resolveVariables(svg: string): string {
    let result = svg
    for (const [variable, color] of Object.entries(variableReplacements)) {
        const pattern = new RegExp(
            `(fill|stroke)="var\\(${variable.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}\\)"`,
            'g'
        )
        result = result.replace(pattern, `$1="${color}"`)
    }
    return result
}

/**
 * Remove Google Fonts @import to avoid external network dependency
 */
function removeGoogleFonts(svg: string): string {
    return svg.replace(
        /@import url\('https:\/\/fonts\.googleapis\.com[^']*'\);\s*/g,
        ''
    )
}

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
 * Lazy-load Beautiful Mermaid from local _static/ only when diagrams exist on the page
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
            resolve()
        }
        script.onerror = () =>
            reject(new Error('Failed to load Beautiful Mermaid'))
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
        // Lazy-load Beautiful Mermaid only when diagrams exist
        await loadMermaid()

        // Render each diagram individually
        for (let i = 0; i < mermaidElements.length; i++) {
            const element = mermaidElements[i]
            const content = element.textContent?.trim()

            if (!content) continue

            // Mark as processed to prevent double rendering
            element.setAttribute('data-mermaid-processed', 'true')

            try {
                // Render the diagram using Beautiful Mermaid
                let svg = await window.__mermaid.renderMermaid(content)

                // Post-process the SVG
                svg = resolveVariables(svg)
                svg = removeGoogleFonts(svg)

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

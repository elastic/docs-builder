import { loadKatexStylesheet } from './katex-stylesheet'
import { $$optional } from 'select-dom'

/**
 * Initialize KaTeX math rendering for elements with class 'math'.
 * KaTeX's JS and fonts/CSS are lazy-loaded here so pages without math pay nothing for them.
 */
export async function initMath() {
    const mathElements = $$optional('.math:not([data-katex-processed])')
    if (mathElements.length === 0) return

    const [katex] = await Promise.all([import('katex'), loadKatexStylesheet()])

    // The initial load and htmx:load both start this step; the second must not render over the first's output.
    mathElements.forEach((element) => {
        if (element.hasAttribute('data-katex-processed')) return
        try {
            const content = element.textContent?.trim()
            if (!content) return

            // Determine if this is display math based on content and element type
            const isDisplayMath =
                element.tagName === 'DIV' ||
                content.includes('\\[') ||
                content.includes('$$') ||
                content.includes('\\begin{') ||
                content.includes('\n')

            // Clean up common LaTeX delimiters
            const cleanContent = content
                .replace(/^\$\$|\$\$$/g, '') // Remove $$ delimiters
                .replace(/^\\\[|\\\]$/g, '') // Remove \[ \] delimiters
                .trim()

            // Clear the element content before rendering
            element.innerHTML = ''

            katex.render(cleanContent, element, {
                throwOnError: false,
                displayMode: isDisplayMath,
                strict: false, // Allow some LaTeX extensions
                trust: false, // Security: don't trust arbitrary commands
                output: 'mathml', // Only render MathML, not HTML
                macros: {
                    // Add common macros if needed
                },
            })

            // Mark as processed to prevent double processing
            element.setAttribute('data-katex-processed', 'true')
        } catch (error) {
            console.warn('KaTeX rendering error:', error)
            // Fallback: keep the original content
            element.innerHTML = element.textContent || ''
        }
    })
}

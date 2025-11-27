// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

/**
 * Navigation Tooltip System
 * Dynamically positions tooltips relative to the viewport for navigation items
 */

interface TooltipOptions {
    offsetX: number
    offsetY: number
    delay: number
}

class NavigationTooltip {
    private tooltip: HTMLElement | null = null
    private currentTarget: HTMLElement | null = null
    private showTimer: number | null = null
    private hideTimer: number | null = null
    private readonly options: TooltipOptions

    constructor(options: Partial<TooltipOptions> = {}) {
        this.options = {
            offsetX: options.offsetX ?? 12,
            offsetY: options.offsetY ?? 0,
            delay: options.delay ?? 500,
        }
    }

    private createTooltip(): HTMLElement {
        const tooltip = document.createElement('div')
        tooltip.className = 'nav-tooltip'
        tooltip.setAttribute('role', 'tooltip')
        tooltip.style.position = 'fixed'
        tooltip.style.pointerEvents = 'none'
        tooltip.style.zIndex = '10000'
        document.body.appendChild(tooltip)
        return tooltip
    }

    private getTooltip(): HTMLElement {
        if (!this.tooltip) {
            this.tooltip = this.createTooltip()
        }
        return this.tooltip
    }

    private positionTooltip(target: HTMLElement): void {
        const tooltip = this.getTooltip()
        const rect = target.getBoundingClientRect()

        // Position tooltip to the right of the nav item
        const left = rect.right + this.options.offsetX
        const top = rect.top + rect.height / 2 + this.options.offsetY

        tooltip.style.left = `${left}px`
        tooltip.style.top = `${top}px`
        tooltip.style.transform = 'translateY(-50%)'

        // Check if tooltip goes off the right edge of viewport
        const tooltipRect = tooltip.getBoundingClientRect()
        if (tooltipRect.right > window.innerWidth) {
            // Position to the left instead
            tooltip.style.left = `${rect.left - tooltipRect.width - this.options.offsetX}px`
        }

        // Check if tooltip goes off the bottom edge
        if (tooltipRect.bottom > window.innerHeight) {
            const newTop = window.innerHeight - tooltipRect.height - 8
            tooltip.style.top = `${newTop}px`
            tooltip.style.transform = 'none'
        }

        // Check if tooltip goes off the top edge
        if (tooltipRect.top < 0) {
            tooltip.style.top = '8px'
            tooltip.style.transform = 'none'
        }
    }

    private showTooltip(target: HTMLElement, text: string): void {
        this.currentTarget = target
        const tooltip = this.getTooltip()
        tooltip.textContent = text
        tooltip.classList.add('nav-tooltip--visible')
        this.positionTooltip(target)
    }

    private hideTooltip(): void {
        if (this.tooltip) {
            this.tooltip.classList.remove('nav-tooltip--visible')
        }
        this.currentTarget = null
    }

    private handleMouseEnter = (e: MouseEvent): void => {
        const target = e.currentTarget as HTMLElement
        const tooltipText = target.getAttribute('data-nav-tooltip')

        if (!tooltipText) return

        // Clear any pending hide timer
        if (this.hideTimer !== null) {
            clearTimeout(this.hideTimer)
            this.hideTimer = null
        }

        // Set a timer to show the tooltip after delay
        this.showTimer = window.setTimeout(() => {
            this.showTooltip(target, tooltipText)
        }, this.options.delay)
    }

    private handleMouseLeave = (): void => {
        // Clear show timer if mouse leaves before delay completes
        if (this.showTimer !== null) {
            clearTimeout(this.showTimer)
            this.showTimer = null
        }

        // Hide tooltip after a short delay
        this.hideTimer = window.setTimeout(() => {
            this.hideTooltip()
        }, 100)
    }

    private handleScroll = (): void => {
        // Reposition tooltip if it's visible and target still exists
        if (
            this.currentTarget &&
            this.tooltip?.classList.contains('nav-tooltip--visible')
        ) {
            this.positionTooltip(this.currentTarget)
        }
    }

    public init(): void {
        // Find all navigation items with tooltips
        const navItems =
            document.querySelectorAll<HTMLElement>('[data-nav-tooltip]')

        navItems.forEach((item) => {
            item.addEventListener('mouseenter', this.handleMouseEnter)
            item.addEventListener('mouseleave', this.handleMouseLeave)
        })

        // Update tooltip position on scroll
        window.addEventListener('scroll', this.handleScroll, { passive: true })

        // Update tooltip position on resize
        window.addEventListener('resize', this.handleScroll, { passive: true })
    }

    public destroy(): void {
        const navItems =
            document.querySelectorAll<HTMLElement>('[data-nav-tooltip]')

        navItems.forEach((item) => {
            item.removeEventListener('mouseenter', this.handleMouseEnter)
            item.removeEventListener('mouseleave', this.handleMouseLeave)
        })

        window.removeEventListener('scroll', this.handleScroll)
        window.removeEventListener('resize', this.handleScroll)

        if (this.showTimer !== null) {
            clearTimeout(this.showTimer)
        }

        if (this.hideTimer !== null) {
            clearTimeout(this.hideTimer)
        }

        if (this.tooltip) {
            this.tooltip.remove()
            this.tooltip = null
        }
    }
}

// Store global instance
let globalNavTooltip: NavigationTooltip | null = null

// Initialize navigation tooltips
export function initNavigationTooltips(): void {
    // Clean up previous instance if it exists
    if (globalNavTooltip) {
        globalNavTooltip.destroy()
    }

    globalNavTooltip = new NavigationTooltip()
    globalNavTooltip.init()
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initNavigationTooltips)
} else {
    initNavigationTooltips()
}

export { NavigationTooltip }

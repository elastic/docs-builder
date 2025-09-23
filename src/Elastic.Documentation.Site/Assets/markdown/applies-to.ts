import { $$ } from 'select-dom'
import tippy from 'tippy.js'

document.addEventListener('htmx:load', function () {
    const selector = [
        '.applies [data-tippy-content]:not([data-tippy-content=""])',
        '.applies-inline [data-tippy-content]:not([data-tippy-content=""])',
    ].join(', ')

    const appliesToBadgesWithTooltip = $$(selector)
    appliesToBadgesWithTooltip.forEach((badge) => {
        const content = badge.getAttribute('data-tippy-content')
        if (!content) return
        tippy(badge, {
            content,
            allowHTML: true,
            delay: [400, 100],
            hideOnClick: false,
            ignoreAttributes: true,
            theme: 'applies-to',
        })
    })
})

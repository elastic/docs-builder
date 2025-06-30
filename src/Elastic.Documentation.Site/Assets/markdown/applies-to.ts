import tippy from 'tippy.js'

document.addEventListener('htmx:load', function () {
    tippy(
        [
            '.applies [data-tippy-content]:not([data-tippy-content=""])',
            '.applies-inline [data-tippy-content]:not([data-tippy-content=""])',
        ].join(', '),
        {
            delay: [400, 100],
        }
    )
})

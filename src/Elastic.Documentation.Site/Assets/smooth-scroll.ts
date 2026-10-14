import { $$optional } from 'select-dom'

export function initSmoothScroll() {
    $$optional('#markdown-content a[href^="#"]').forEach((el) => {
        // API pages scroll to a link's target themselves, after opening the rows around it
        if (el.closest('#elastic-api-v3, #schema-definition, #paths')) return
        el.addEventListener('click', (e) => {
            // Using ! because href is guaranteed to be present due to the selector
            const id = el.getAttribute('href')!.slice(1) // remove the '#' character
            const target = document.getElementById(id)
            if (target) {
                e.preventDefault()
                target.scrollIntoView({ behavior: 'smooth' })
                history.pushState(null, '', el.getAttribute('href'))
            }
        })
    })
}

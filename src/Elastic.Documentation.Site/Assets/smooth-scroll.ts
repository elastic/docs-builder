import { $$ } from 'select-dom'

export function initSmoothScroll() {
    $$('#markdown-content a[href^="#"]').forEach((el) => {
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

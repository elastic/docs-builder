import { $$ } from 'select-dom'

export function initSmoothScroll() {
    $$('#markdown-content a[href^="#"]').forEach((el) => {
        el.addEventListener('click', (e) => {
            const target = document.getElementById(
                el.getAttribute('href').slice(1)
            )
            if (target) {
                e.preventDefault()
                target.scrollIntoView({ behavior: 'smooth' })
                history.pushState(null, '', el.getAttribute('href'))
            }
        })
    })
}

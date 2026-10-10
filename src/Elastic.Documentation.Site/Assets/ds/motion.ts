// Large movement (card lifts, slides, shimmer) is switched by data-motion on <html>: "full" turns it on, "reduce" leaves the calm default.
// The head script sets it before first paint from the saved choice, or from the system setting. This module adds the control that lets a
// reader override the system, which is also how a designer previews the animations with reduced motion on at the OS level.
export const motionStorageKey = 'docs-motion'

export type MotionChoice = 'system' | 'reduce' | 'full'

const order: MotionChoice[] = ['system', 'reduce', 'full']
const labels: Record<MotionChoice, string> = {
    system: 'System',
    reduce: 'Reduced',
    full: 'Full',
}

export function readMotion(): MotionChoice {
    try {
        const stored = localStorage.getItem(motionStorageKey)
        return stored === 'reduce' || stored === 'full' ? stored : 'system'
    } catch {
        return 'system'
    }
}

function systemPrefersReduced(): boolean {
    return (
        window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false
    )
}

export function applyMotion(
    choice: MotionChoice,
    root: HTMLElement = document.documentElement
) {
    const resolved =
        choice === 'system'
            ? systemPrefersReduced()
                ? 'reduce'
                : 'full'
            : choice
    root.dataset.motion = resolved
    try {
        if (choice === 'system') localStorage.removeItem(motionStorageKey)
        else localStorage.setItem(motionStorageKey, choice)
    } catch {
        // storage unavailable: the choice still applies for this page view
    }
}

export function nextMotion(choice: MotionChoice): MotionChoice {
    return order[(order.indexOf(choice) + 1) % order.length]
}

let followingSystem = false

export function initMotionToggles(scope: ParentNode = document) {
    if (!followingSystem) {
        followingSystem = true
        // With "system" chosen, the page follows the setting as it changes.
        window
            .matchMedia?.('(prefers-reduced-motion: reduce)')
            .addEventListener?.('change', () => {
                if (readMotion() === 'system') applyMotion('system')
            })
    }

    scope
        .querySelectorAll<HTMLElement>(
            '[data-motion-toggle]:not([data-motion-bound])'
        )
        .forEach((button) => {
            button.dataset.motionBound = ''
            // Rendered hidden: without script it would do nothing.
            button.hidden = false
            const sync = (choice: MotionChoice) => {
                button.dataset.motionChoice = choice
                const label = button.querySelector('[data-motion-label]')
                if (label) label.textContent = labels[choice]
                button.setAttribute(
                    'aria-label',
                    `Motion: ${labels[choice].toLowerCase()}. Activate to change.`
                )
            }
            sync(readMotion())
            button.addEventListener('click', () => {
                const choice = nextMotion(readMotion())
                applyMotion(choice)
                sync(choice)
            })
        })
}

// The same choice as a segmented control: a group of radios under data-motion-select, one per choice, which works from the saved value.
export function initMotionSelects(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLElement>(
            '[data-motion-select]:not([data-motion-bound])'
        )
        .forEach((group) => {
            group.dataset.motionBound = ''
            const radios = group.querySelectorAll<HTMLInputElement>(
                'input[type="radio"]'
            )
            const current = readMotion()
            radios.forEach((radio) => {
                radio.checked = radio.value === current
                radio.addEventListener('change', () => {
                    if (radio.checked) applyMotion(radio.value as MotionChoice)
                })
            })
        })
}

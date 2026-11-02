/** A square icon button for a code card header, with a tooltip and an accessible name. */
export function iconButton(className: string, label: string, icon: string) {
    const button = document.createElement('button')
    button.type = 'button'
    button.className = className
    button.dataset.tippyContent = label
    button.setAttribute('aria-label', label)
    button.innerHTML = icon
    return button
}

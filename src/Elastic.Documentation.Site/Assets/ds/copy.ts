// Adds copy-to-clipboard to code blocks. The button is rendered hidden and revealed here because it cannot work without JavaScript.
// Hooks are data attributes: [data-copy] on the button inside the block that holds the code.
const resetAfterMs = 2000

function codeText(block: Element): string {
    const clone = block.cloneNode(true) as Element
    // Callout markers are decoration, never part of the code.
    clone
        .querySelectorAll('.code-callout, [data-copy], figcaption')
        .forEach((node) => node.remove())
    return Array.from(clone.querySelectorAll('pre code'))
        .map((code) => code.textContent ?? '')
        .join('\n')
        .replace(/\n+$/, '')
}

export function initCopyButtons(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLButtonElement>(
            '[data-copy]:not([data-copy-bound])'
        )
        .forEach((button) => {
            const block = button.closest('figure, [data-code-block]')
            if (!block || !navigator.clipboard) return

            button.dataset.copyBound = ''
            button.hidden = false
            button.addEventListener('click', async () => {
                try {
                    await navigator.clipboard.writeText(codeText(block))
                } catch {
                    return
                }
                button.dataset.copied = ''
                window.setTimeout(
                    () => delete button.dataset.copied,
                    resetAfterMs
                )
            })
        })
}

// Buttons that copy a fixed string, such as an install command: [data-copy-text] holds the text.
export function initCopyText(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLButtonElement>(
            'button[data-copy-text]:not([data-copy-text-bound])'
        )
        .forEach((button) => {
            if (!navigator.clipboard) return
            button.dataset.copyTextBound = ''
            button.addEventListener('click', async () => {
                const text = button.dataset.copyText
                if (!text) return
                try {
                    await navigator.clipboard.writeText(text)
                } catch {
                    return
                }
                const label = button.innerHTML
                button.dataset.copied = ''
                button.textContent = 'Copied!'
                window.setTimeout(() => {
                    delete button.dataset.copied
                    button.innerHTML = label
                }, 1500)
            })
        })
}

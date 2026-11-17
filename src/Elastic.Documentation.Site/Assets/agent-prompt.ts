import { logInfo } from './telemetry/logging'
import {
    ATTR_AGENT_PROMPT_INDEX,
    ATTR_AGENT_PROMPT_LENGTH,
    ATTR_AGENT_PROMPT_LOCATION,
    ATTR_AGENT_PROMPT_TITLE,
    ATTR_ERROR_TYPE,
    ATTR_EVENT_NAME,
    ATTR_URL_FULL,
    ATTR_URL_PATH,
} from './telemetry/semconv'

/** Which control on the page triggered an event. */
export type AgentPromptLocation = 'inline' | 'dialog'

const FEEDBACK_MS = 1500

function promptBlocks() {
    return Array.from(document.querySelectorAll<HTMLElement>('.agent-prompt'))
}

function promptText(root: HTMLElement) {
    return root.querySelector('.agent-prompt-body code')?.textContent ?? ''
}

function promptTitle(root: HTMLElement) {
    return root.querySelector('.agent-prompt-title')?.textContent?.trim() ?? ''
}

// Emit an agent prompt event. The prompt text is never sent, only its length and the block's
// title, so events stay small and can be grouped by block.
function track(
    eventName: string,
    root: HTMLElement,
    location: AgentPromptLocation,
    extra: Record<string, string> = {}
) {
    logInfo(eventName, {
        [ATTR_EVENT_NAME]: eventName,
        [ATTR_AGENT_PROMPT_TITLE]: promptTitle(root),
        [ATTR_AGENT_PROMPT_LOCATION]: location,
        [ATTR_AGENT_PROMPT_LENGTH]: promptText(root).length,
        [ATTR_AGENT_PROMPT_INDEX]: promptBlocks().indexOf(root),
        [ATTR_URL_PATH]: window.location.pathname,
        [ATTR_URL_FULL]: window.location.href,
        ...extra,
    })
}

// Read the name structurally: a DOMException is not always an instance of this realm's Error.
function errorName(error: unknown) {
    return typeof error === 'object' && error !== null && 'name' in error
        ? String(error.name)
        : 'unknown'
}

/**
 * Copy the block's prompt to the clipboard. Emits agent_prompt_copied only when the write
 * succeeds, and agent_prompt_copy_failed when it does not. Returns whether it succeeded, so the
 * caller can show "Copied!" only then.
 */
export async function copyPrompt(
    root: HTMLElement,
    location: AgentPromptLocation
) {
    try {
        await navigator.clipboard.writeText(promptText(root))
    } catch (error) {
        console.error('Failed to copy agent prompt:', error)
        track('agent_prompt_copy_failed', root, location, {
            [ATTR_ERROR_TYPE]: errorName(error),
        })
        return false
    }
    track('agent_prompt_copied', root, location)
    return true
}

/** Open the block's dialog and emit agent_prompt_review_opened. Does nothing if it is open. */
export function openPrompt(root: HTMLElement, location: AgentPromptLocation) {
    const dialog = root.querySelector<HTMLDialogElement>('.agent-prompt-dialog')
    if (!dialog || dialog.open) return
    dialog.showModal()
    track('agent_prompt_review_opened', root, location)
}

function showCopied(button: HTMLButtonElement) {
    const label = button.textContent
    button.textContent = 'Copied!'
    button.setAttribute('data-copied', '')
    setTimeout(() => {
        button.textContent = label
        button.removeAttribute('data-copied')
    }, FEEDBACK_MS)
}

export function initAgentPrompt() {
    document
        .querySelectorAll<HTMLElement>('.agent-prompt')
        .forEach((root, index) => {
            if (root.dataset.initialized) return
            root.dataset.initialized = 'true'
            // Read by initAgentPromptImpressions.
            root.dataset.agentPromptLocation = 'inline'
            root.dataset.agentPromptIndex = String(index)

            const dialog = root.querySelector<HTMLDialogElement>(
                '.agent-prompt-dialog'
            )
            if (!dialog || !root.querySelector('.agent-prompt-body code'))
                return

            root.querySelector('.agent-prompt-open')?.addEventListener(
                'click',
                () => openPrompt(root, 'inline')
            )
            root.querySelector('.agent-prompt-close')?.addEventListener(
                'click',
                () => dialog.close()
            )
            // Clicking the backdrop (the dialog element itself) closes the modal.
            dialog.addEventListener('click', (event) => {
                if (event.target === dialog) dialog.close()
            })

            root.querySelectorAll<HTMLButtonElement>(
                '.agent-prompt-copy'
            ).forEach((button) => {
                button.addEventListener('click', async () => {
                    const location = button.closest('dialog')
                        ? 'dialog'
                        : 'inline'
                    if (await copyPrompt(root, location)) showCopied(button)
                })
            })
        })
}

let impressionObserver: IntersectionObserver | null = null

/**
 * Emits agent_prompt_viewed the first time each inline card is at least half visible, then stops
 * observing it: one impression per card per page view. It is the denominator for copy and review
 * rates. A card that is never scrolled into view never fires.
 */
export function initAgentPromptImpressions() {
    impressionObserver ??= new IntersectionObserver(
        (entries, observer) => {
            for (const entry of entries) {
                if (!entry.isIntersecting) continue
                const target = entry.target as HTMLElement
                const root =
                    promptBlocks()[Number(target.dataset.agentPromptIndex)]
                if (root)
                    track(
                        'agent_prompt_viewed',
                        root,
                        target.dataset
                            .agentPromptLocation as AgentPromptLocation
                    )
                observer.unobserve(target)
            }
        },
        { threshold: 0.5 }
    )
    document
        .querySelectorAll<HTMLElement>('[data-agent-prompt-location]')
        .forEach((el) => impressionObserver?.observe(el))
}

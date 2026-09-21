type IubendaApi = {
    openPreferences?: () => void
}

type IubendaRoot = {
    cs?: {
        api?: IubendaApi
    }
}

declare global {
    interface Window {
        _iub?: IubendaRoot
    }
}

export function iubendaActionFor(
    target: EventTarget | null
): 'preferences' | null {
    if (!(target instanceof Element)) return null
    if (target.closest('.iubenda-cs-preferences-link')) return 'preferences'
    return null
}

function iubendaApi(): IubendaApi | undefined {
    return window._iub?.cs?.api
}

export function handlePrivacyConsentClick(event: Event): void {
    if (iubendaActionFor(event.target) !== 'preferences') return

    event.preventDefault()
    iubendaApi()?.openPreferences?.()
}

export function initPrivacyConsent(): void {
    document.addEventListener('click', handlePrivacyConsentClick, true)
}

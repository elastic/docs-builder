type IubendaApi = {
    openPreferences?: () => void
    showBanner?: () => void
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
): 'preferences' | 'uspr' | null {
    if (!(target instanceof Element)) return null
    if (target.closest('.iubenda-cs-preferences-link')) return 'preferences'
    if (target.closest('.iubenda-cs-uspr-link')) return 'uspr'
    return null
}

function iubendaApi(): IubendaApi | undefined {
    return window._iub?.cs?.api
}

export function handlePrivacyConsentClick(event: Event): void {
    const action = iubendaActionFor(event.target)
    if (!action) return

    event.preventDefault()
    const api = iubendaApi()
    if (!api) return

    if (action === 'preferences') api.openPreferences?.()
    else api.showBanner?.()
}

export function initPrivacyConsent(): void {
    document.addEventListener('click', handlePrivacyConsentClick, true)
}

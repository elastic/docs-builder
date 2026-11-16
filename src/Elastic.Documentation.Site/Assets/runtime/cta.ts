import { logInfo } from '../telemetry/logging'
import {
    ATTR_CTA_NAME,
    ATTR_CTA_URL,
    ATTR_CTA_LABEL,
    ATTR_CTA_LOCATION,
    ATTR_EVENT_NAME,
    ATTR_URL_PATH,
    ATTR_URL_FULL,
} from '../telemetry/semconv'
import { $$optional } from 'select-dom'

// Attributes shared by cta_viewed and cta_clicked so the two are directly comparable (CTR).
function ctaAttributes(cta: HTMLAnchorElement) {
    return {
        [ATTR_CTA_NAME]: cta.dataset.cta ?? '',
        [ATTR_CTA_URL]: cta.dataset.ctaUrl ?? '',
        [ATTR_CTA_LABEL]: cta.dataset.ctaLabel ?? '',
        [ATTR_CTA_LOCATION]: cta.dataset.ctaLocation ?? '',
        [ATTR_URL_PATH]: window.location.pathname,
        [ATTR_URL_FULL]: window.location.href,
    }
}

// Emit a CTA event: event.name mirrors the body so records can be filtered by
// exact keyword (attributes.event.name) instead of a text match on body.
function logCtaEvent(eventName: string, cta: HTMLAnchorElement) {
    logInfo(eventName, {
        [ATTR_EVENT_NAME]: eventName,
        ...ctaAttributes(cta),
    })
}

let ctaImpressionObserver: IntersectionObserver | null = null

// Fires 'cta_viewed' the first time a CTA becomes at least half visible, then stops
// observing it (one impression per page view). The observer is created once and
// reused across htmx swaps; re-observing an already-observed element is a no-op per
// spec, so this only needs to (re-)register CTAs that are new since the last swap.
export function initCtaImpressions() {
    ctaImpressionObserver ??= new IntersectionObserver(
        (entries, observer) => {
            for (const entry of entries) {
                if (!entry.isIntersecting) continue
                logCtaEvent('cta_viewed', entry.target as HTMLAnchorElement)
                observer.unobserve(entry.target)
            }
        },
        { threshold: 0.5 }
    )
    $$optional('a[data-cta]').forEach((cta) =>
        ctaImpressionObserver?.observe(cta)
    )
}

function handleCtaActivation(event: MouseEvent) {
    const cta = (event.target as HTMLElement)?.closest<HTMLAnchorElement>(
        'a[data-cta]'
    )
    if (!cta) return
    logCtaEvent('cta_clicked', cta)
}

// Delegated listeners: they survive htmx swaps without needing re-init, unlike the
// init steps, which bind directly to elements that get replaced.
// logInfo's export survives this same-tab navigation: the batch processor flushes on
// 'pagehide' (registered in initializeOtel) via a keepalive fetch, which browsers keep
// alive past unload.
export function bindCtaClicks() {
    document.addEventListener('click', handleCtaActivation)
    // 'auxclick' with button 1 covers middle-click (open in new tab), which does NOT
    // fire 'click' per the DOM spec - without this those opens went untracked. Button 2
    // (right-click / context menu) also fires auxclick but isn't a real engagement.
    document.addEventListener('auxclick', function (event: MouseEvent) {
        if (event.button === 1) handleCtaActivation(event)
    })
}

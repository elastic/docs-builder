import { initAgentSkillCopy } from './agent-skill'
import { initApiChromeSync } from './api-chrome-sync'
import { initApiDocs } from './api-docs'
import { initApiMenus } from './api-menu'
import { changesPageChrome } from './api-navigation'
import { initAppliesSwitch } from './applies-switch'
import { config } from './config'
import { initCopyButton } from './copybutton'
import { initHighlight, preloadHighlight } from './hljs'
import { initImageCarousel } from './image-carousel'
import { initListing } from './listing'
import { initMath } from './math'
import { initMermaid } from './mermaid'
import { openDetailsWithAnchor } from './open-details-with-anchor'
import { initNav } from './pages-nav'
import { initPrivacyConsent } from './privacy-consent'
import { bindCtaClicks, initCtaImpressions } from './runtime/cta'
import { applyEditParam } from './runtime/edit-param'
import { bindHtmxHooks } from './runtime/htmx-hooks'
import { runInitSteps } from './runtime/init-steps'
import { initTelemetry } from './runtime/telemetry'
import { initSecondaryNav } from './secondary-nav'
import { initSmoothScroll } from './smooth-scroll'
import { initTable } from './table'
import { initTabs } from './tabs'
import { initTocNav } from './toc-nav'
import { loadWebComponents } from './web-components/loadWebComponents'
import 'htmx-ext-head-support'
import 'htmx-ext-preload'

// The OTel SDK must be up before the web components run; the isolated and codex entries load after it.
async function bootstrap() {
    await initTelemetry()

    if (config.buildType === 'isolated' || config.airGapped) {
        import('./isolated')
    } else if (config.buildType === 'codex') {
        import('./codex')
    }
}

const bootstrapPromise = bootstrap()

async function initWebComponents() {
    await bootstrapPromise
    await loadWebComponents()
}

// The languages chunk is the long pole for code pages; fetch it before the init steps get to it.
if (document.querySelector('pre code')) preloadHighlight()

// Initialize on initial page load
document.addEventListener('DOMContentLoaded', function () {
    runInitSteps([
        ['loadWebComponents', initWebComponents],
        ['initMath', initMath],
        ['initMermaid', initMermaid],
        ['initCtaImpressions', initCtaImpressions],
    ])
})

document.addEventListener('htmx:load', function () {
    runInitSteps([
        ['loadWebComponents', initWebComponents],
        ['initTocNav', initTocNav],
        ['initHighlight', initHighlight],
        ['initCopyButton', initCopyButton],
        ['initAgentSkillCopy', initAgentSkillCopy],
        ['initTabs', initTabs],
        ['initAppliesSwitch', initAppliesSwitch],
        ['initMath', initMath],
        ['initMermaid', initMermaid],
        ['initNav', initNav],
        ['initSmoothScroll', initSmoothScroll],
        ['openDetailsWithAnchor', openDetailsWithAnchor],
        ['initImageCarousel', initImageCarousel],
        ['initListing', initListing],
        ['initTable', initTable],
        ['initApiDocs', initApiDocs],
        ['applyEditParam', applyEditParam],
        ['initCtaImpressions', initCtaImpressions],
    ])
})

bindCtaClicks()
initPrivacyConsent()
initSecondaryNav()
initApiMenus()
initApiChromeSync()
bindHtmxHooks({
    forcesFullPageLoad: (targetPath) =>
        // A move between a docs page and an API page, or to another API product: the swap would
        // leave the previous header or context bar in place.
        changesPageChrome(
            targetPath,
            location.pathname,
            config.rootPath.replace(/\/$/, '')
        ),
})

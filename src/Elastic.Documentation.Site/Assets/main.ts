import { initCopyButton } from './copybutton'
import { initDismissibleBanner } from './dismissible-banner'
import { initHighlight } from './hljs'
import './markdown/applies-to'
import { openDetailsWithAnchor } from './open-details-with-anchor'
import { initNav } from './pages-nav'
import { initSmoothScroll } from './smooth-scroll'
import { initTabs } from './tabs'
import { initTocNav } from './toc-nav'
import './web-components/VersionDropdown'
import 'htmx-ext-head-support'
import 'htmx-ext-preload'
import 'htmx.org'
import { $, $$ } from 'select-dom'
import { UAParser } from 'ua-parser-js'

const { getOS } = new UAParser()

document.addEventListener('htmx:load', function (event) {
    console.log('htmx:load')
    console.log(event.detail)
    initTocNav()
    initHighlight()
    initCopyButton()
    initTabs()
    initNav()
    initSmoothScroll()
    openDetailsWithAnchor()
    initDismissibleBanner()
})

// Don't remove style tags because they are used by the elastic global nav.
document.addEventListener('htmx:removingHeadElement', function (event) {
    const tagName = event.detail.headElement.tagName
    if (tagName === 'STYLE') {
        event.preventDefault()
    }
})

document.addEventListener('htmx:beforeRequest', function (event) {
    if (
        event.detail.requestConfig.verb === 'get' &&
        event.detail.requestConfig.triggeringEvent
    ) {
        const { ctrlKey, metaKey, shiftKey }: PointerEvent =
            event.detail.requestConfig.triggeringEvent
        const { name: os } = getOS()
        const modifierKey: boolean = os === 'macOS' ? metaKey : ctrlKey
        if (shiftKey || modifierKey) {
            event.preventDefault()
            window.open(
                event.detail.requestConfig.path,
                '_blank',
                'noopener,noreferrer'
            )
        }
    }
})

document.body.addEventListener('htmx:oobBeforeSwap', function (event) {
    // This is needed to scroll to the top of the page when the content is swapped
    if (
        event.target.id === 'main-container' ||
        event.target.id === 'markdown-content' ||
        event.target.id === 'content-container'
    ) {
        window.scrollTo(0, 0)
    }
})

document.body.addEventListener('htmx:pushedIntoHistory', function (event) {
    const pagesNav = $('#pages-nav')
    const currentNavItem = $$('.current', pagesNav)
    currentNavItem.forEach((el) => {
        el.classList.remove('current')
    })
    const navItems = $$('a[href="' + event.detail.path + '"]', pagesNav)
    navItems.forEach((navItem) => {
        navItem.classList.add('current')
    })
})

document.body.addEventListener('htmx:responseError', function (event) {
    // If you get a 404 error while clicking on a hx-get link, actually open the link
    // This is needed because the browser doesn't update the URL when the response is a 404
    // In production, cloudfront handles serving the 404 page.
    // Locally, the DocumentationWebHost handles it.
    // On previews, a generic 404 page is shown.
    if (event.detail.xhr.status === 404) {
        window.location.assign(event.detail.pathInfo.requestPath)
    }
})

// We add a query string to the get request to make sure the requested page is up to date
const docsBuilderVersion = $('body').dataset.docsBuilderVersion
document.body.addEventListener('htmx:configRequest', function (event) {
    if (event.detail.verb === 'get') {
        event.detail.parameters['v'] = docsBuilderVersion
    }
})

// Here we need to strip the v parameter from the URL so
// that the browser doesn't show the v parameter in the address bar
document.body.addEventListener('htmx:beforeHistoryUpdate', function (event) {
    const params = new URLSearchParams(
        event.detail.history.path.split('?')[1] ?? ''
    )
    params.delete('v')
    const pathWithoutQueryString = event.detail.history.path.split('?')[0]
    if (params.size === 0) {
        event.detail.history.path = pathWithoutQueryString
    } else {
        event.detail.history.path =
            pathWithoutQueryString + '?' + params.toString()
    }
})

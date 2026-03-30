import tippy from 'tippy.js'
import type { Instance } from 'tippy.js'
import { $$ } from 'select-dom'

const navV2CollapsedStorageKey = 'docs-builder-nav-v2-collapsed-ids'

let navV2FolderLinkToggleBound = false
let navV2OutsideCollapseBound = false

let navV2TruncationTippyInstances: Instance[] = []

function readCollapsedFolderIds(): Set<string> {
    try {
        const raw = sessionStorage.getItem(navV2CollapsedStorageKey)
        if (!raw) {
            return new Set()
        }

        const parsed = JSON.parse(raw) as unknown
        if (!Array.isArray(parsed)) {
            return new Set()
        }

        return new Set(parsed.filter((x): x is string => typeof x === 'string'))
    } catch {
        return new Set()
    }
}

function writeCollapsedFolderIds(ids: Set<string>) {
    sessionStorage.setItem(navV2CollapsedStorageKey, JSON.stringify([...ids]))
}

function persistFolderCheckboxCollapsedState(cb: HTMLInputElement) {
    if (!cb.id) {
        return
    }

    const ids = readCollapsedFolderIds()
    if (!cb.checked) {
        ids.add(cb.id)
    } else {
        ids.delete(cb.id)
    }

    writeCollapsedFolderIds(ids)
}

function normalizeDocPathname(pathname: string) {
    const p = pathname.replace(/\/$/, '')
    return p === '' ? '/' : p
}

function linkPathMatchesCurrentPage(anchor: HTMLAnchorElement) {
    const href = anchor.getAttribute('href')
    if (!href) {
        return false
    }

    const linkPath = normalizeDocPathname(
        new URL(href, window.location.href).pathname
    )
    const currentPath = normalizeDocPathname(window.location.pathname)
    return linkPath === currentPath
}

/**
 * Primary click on a folder row link toggles expand/collapse (checkbox) and still follows href.
 * Skips modified clicks (new tab, etc.). Same-URL clicks use preventDefault so HTMX does not
 * re-run expandToCurrentPage and undo the collapse. Collapsed folder ids are stored so
 * expandToCurrentPage can respect user collapse after in-section navigation.
 */
function ensureNavV2FolderLinkToggle() {
    if (navV2FolderLinkToggleBound) {
        return
    }

    navV2FolderLinkToggleBound = true

    document.addEventListener(
        'click',
        (e: MouseEvent) => {
            if (!(e.target instanceof Element)) {
                return
            }

            if (
                e.defaultPrevented ||
                e.button !== 0 ||
                e.metaKey ||
                e.ctrlKey ||
                e.shiftKey ||
                e.altKey
            ) {
                return
            }

            const a = e.target.closest(
                '[data-nav-v2] li.group-navigation > .nav-folder-peer > a.sidebar-link'
            ) as HTMLAnchorElement | null

            if (!a) {
                return
            }

            const peer = a.parentElement
            const cb = peer?.querySelector<HTMLInputElement>(
                ':scope > input[type="checkbox"]'
            )

            if (!cb) {
                return
            }

            cb.checked = !cb.checked
            cb.dispatchEvent(new Event('change', { bubbles: true }))
            persistFolderCheckboxCollapsedState(cb)

            // Same URL: never navigate so expand/collapse toggles reliably (second click closes).
            if (linkPathMatchesCurrentPage(a)) {
                e.preventDefault()
                e.stopPropagation()
            }
        },
        true
    )
}

/**
 * Collapse every expanded folder whose subtree does not contain the click target.
 * Runs in capture phase after {@link ensureNavV2FolderLinkToggle} so folder rows toggle first.
 */
function ensureNavV2CollapseOutsideExpandedFolders() {
    if (navV2OutsideCollapseBound) {
        return
    }

    navV2OutsideCollapseBound = true

    document.addEventListener(
        'click',
        (e: MouseEvent) => {
            if (!(e.target instanceof Node)) {
                return
            }

            if (e.button !== 0) {
                return
            }

            const nav = document.querySelector<HTMLElement>('[data-nav-v2]')
            if (!nav) {
                return
            }

            // Clicks on top-level section titles should not close nested open folders.
            if (
                e.target instanceof Element &&
                e.target.closest(
                    'nav[data-nav-v2] .docs-sidebar-nav-v2__label--top'
                )
            ) {
                return
            }

            const target = e.target
            nav.querySelectorAll<HTMLInputElement>(
                'li.group-navigation input[type="checkbox"]:checked'
            ).forEach((cb) => {
                const li = cb.closest('li.group-navigation')
                if (!li || li.contains(target)) {
                    return
                }

                cb.checked = false
                cb.dispatchEvent(new Event('change', { bubbles: true }))
                persistFolderCheckboxCollapsedState(cb)
            })
        },
        true
    )
}

/**
 * Returns all sibling top-level accordion checkboxes for a given checkbox.
 * Siblings are other checkboxes inside [data-v2-accordion] elements at the
 * same nesting level as the given checkbox's ancestor accordion.
 */
function getSiblingAccordionCheckboxes(
    checkbox: HTMLInputElement
): HTMLInputElement[] {
    const accordion = checkbox.closest('[data-v2-accordion]')
    if (!accordion) {
        return []
    }

    const parent = accordion.parentElement
    if (!parent) {
        return []
    }

    return Array.from(
        parent.querySelectorAll<HTMLInputElement>(
            '[data-v2-accordion] > .peer input[type=checkbox]'
        )
    ).filter((c) => c !== checkbox)
}

/**
 * Accordion behaviour: when a top-level section is opened,
 * collapse all its siblings so only one section is expanded at a time.
 */
function initAccordion(nav: HTMLElement) {
    nav.querySelectorAll<HTMLInputElement>(
        '[data-v2-accordion] > .peer input[type=checkbox]'
    ).forEach((cb) => {
        cb.addEventListener('change', (e) => {
            const target = e.target as HTMLInputElement
            if (target.checked) {
                getSiblingAccordionCheckboxes(target).forEach((sibling) => {
                    sibling.checked = false
                })
            }
        })
    })
}

function clearActiveSubtreeHighlight(nav: HTMLElement) {
    nav.querySelectorAll('.nav-v2-active-subtree, .nav-v2-active-leaf').forEach(
        (el) => {
            el.classList.remove('nav-v2-active-subtree', 'nav-v2-active-leaf')
        }
    )
}

/**
 * Apply #F1F6FF background per design: folder index → whole folder + visible children;
 * nested folder index → that folder + its children only; leaf → that row only.
 */
function applyActiveSubtreeHighlight(nav: HTMLElement) {
    clearActiveSubtreeHighlight(nav)
    const current = nav.querySelector<HTMLAnchorElement>(
        'a.sidebar-link.current'
    )
    if (!current || !nav.contains(current)) {
        return
    }

    const hostLi = current.closest('li')
    if (!hostLi || !nav.contains(hostLi)) {
        return
    }

    const folderRowLink = hostLi.querySelector<HTMLAnchorElement>(
        ':scope > .nav-folder-peer > a.sidebar-link'
    )
    const childUl = hostLi.querySelector(':scope > ul')

    if (
        hostLi.classList.contains('group-navigation') &&
        folderRowLink &&
        folderRowLink === current &&
        childUl
    ) {
        hostLi.classList.add('nav-v2-active-subtree')
        return
    }

    hostLi.classList.add('nav-v2-active-leaf')
}

/**
 * Mark the current page's nav link with the "current" CSS class.
 */
function markCurrentPage(nav: HTMLElement) {
    // Remove stale current markers
    $$('.current', nav).forEach((el) => el.classList.remove('current'))

    const pathname = window.location.pathname.replace(/\/$/, '')
    $$(`a[href="${pathname}"], a[href="${pathname}/"]`, nav).forEach((el) =>
        el.classList.add('current')
    )
}

/**
 * Expand all ancestor collapsible sections that contain the current page link,
 * so that navigating directly to a URL reveals its location in the sidebar.
 * Does not re-open a folder row that the user collapsed while that folder index
 * is the current page (see session storage + folder row link match).
 */
function expandToCurrentPage(nav: HTMLElement) {
    const pathname = window.location.pathname.replace(/\/$/, '')
    const link = nav.querySelector<HTMLElement>(
        `a[href="${pathname}"], a[href="${pathname}/"]`
    )
    if (!link) {
        return
    }

    const collapsedIds = readCollapsedFolderIds()

    let el: Element | null = link.parentElement
    while (el && el !== nav) {
        if (el.matches('li')) {
            const cb = el.querySelector<HTMLInputElement>(
                ':scope > .peer input[type=checkbox]'
            )
            if (cb && cb.id) {
                const rowLink = el.querySelector<HTMLElement>(
                    ':scope > .nav-folder-peer > a.sidebar-link'
                )
                const currentIsThisFolderRow =
                    rowLink !== null && rowLink === link

                if (collapsedIds.has(cb.id)) {
                    if (!currentIsThisFolderRow) {
                        collapsedIds.delete(cb.id)
                        writeCollapsedFolderIds(collapsedIds)
                        cb.checked = true
                    }
                } else {
                    cb.checked = true
                }
            } else if (cb) {
                cb.checked = true
            }
        }

        el = el.parentElement
    }
}

function destroyNavV2TruncationTooltips() {
	for (const instance of navV2TruncationTippyInstances) {
		instance.destroy()
	}

	navV2TruncationTippyInstances = []
}

function measureNavTextEl(ref: HTMLElement, textEl: HTMLElement) {
	return ref === textEl
		? textEl
		: (ref.querySelector<HTMLElement>('.docs-sidebar-nav-v2__nav-text') ?? textEl)
}

/**
 * Tippy tooltips only when text is truncated (single-line ellipsis). onShow returns false to cancel.
 */
function initNavV2TruncationTooltips(nav: HTMLElement) {
	destroyNavV2TruncationTooltips()

	const els = nav.querySelectorAll<HTMLElement>('.docs-sidebar-nav-v2__nav-text')
	for (const el of els) {
		const full = el.textContent?.trim() ?? ''
		if (!full) {
			continue
		}

		const ref: HTMLElement =
			el.parentElement?.matches('a.sidebar-link') === true
				? (el.parentElement as HTMLElement)
				: el

		const instance = tippy(ref, {
			content: full,
			placement: 'right-start',
			offset: [0, 6],
			animation: 'fade',
			duration: [200, 150],
			arrow: true,
			maxWidth: 360,
			appendTo: () => document.body,
			theme: 'nav-v2-truncate',
			trigger: 'mouseenter focusin',
			hideOnClick: true,
			interactive: false,
			touch: ['hold', 500],
			aria: { content: 'describedby' },
			onShow() {
				const textEl = measureNavTextEl(ref, el)
				const label = textEl.textContent?.trim() ?? ''
				if (!label) {
					return false
				}

				instance.setContent(label)
				if (textEl.scrollWidth <= textEl.clientWidth + 1) {
					return false
				}
			},
		})

		navV2TruncationTippyInstances.push(instance)
	}
}

/**
 * Initialize all V2 nav behaviours on the given sidebar element.
 * Call this on every htmx:load when [data-nav-v2] is present.
 */
export function initNavV2(nav: HTMLElement) {
	initAccordion(nav)
	markCurrentPage(nav)
	expandToCurrentPage(nav)
	applyActiveSubtreeHighlight(nav)
	requestAnimationFrame(() => {
		requestAnimationFrame(() => initNavV2TruncationTooltips(nav))
	})
}

ensureNavV2FolderLinkToggle()
ensureNavV2CollapseOutsideExpandedFolders()

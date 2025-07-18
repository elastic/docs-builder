// Localization support
import * as ClipboardJS from 'clipboard'

const DOCUMENTATION_OPTIONS = {
    VERSION: '',
    LANGUAGE: 'en',
    COLLAPSE_INDEX: false,
    BUILDER: 'html',
    FILE_SUFFIX: '.html',
    LINK_SUFFIX: '.html',
    HAS_SOURCE: true,
    SOURCELINK_SUFFIX: '.txt',
    NAVIGATION_WITH_KEYS: false,
    SHOW_SEARCH_SUMMARY: true,
    ENABLE_SEARCH_SHORTCUTS: true,
}

const messages = {
    en: {
        copy: 'Copy',
        copy_to_clipboard: 'Copy to clipboard',
        copy_success: 'Copied!',
        copy_failure: 'Failed to copy',
    },
    es: {
        copy: 'Copiar',
        copy_to_clipboard: 'Copiar al portapapeles',
        copy_success: '¡Copiado!',
        copy_failure: 'Error al copiar',
    },
    de: {
        copy: 'Kopieren',
        copy_to_clipboard: 'In die Zwischenablage kopieren',
        copy_success: 'Kopiert!',
        copy_failure: 'Fehler beim Kopieren',
    },
    fr: {
        copy: 'Copier',
        copy_to_clipboard: 'Copier dans le presse-papier',
        copy_success: 'Copié !',
        copy_failure: 'Échec de la copie',
    },
    ru: {
        copy: 'Скопировать',
        copy_to_clipboard: 'Скопировать в буфер',
        copy_success: 'Скопировано!',
        copy_failure: 'Не удалось скопировать',
    },
    'zh-CN': {
        copy: '复制',
        copy_to_clipboard: '复制到剪贴板',
        copy_success: '复制成功!',
        copy_failure: '复制失败',
    },
    it: {
        copy: 'Copiare',
        copy_to_clipboard: 'Copiato negli appunti',
        copy_success: 'Copiato!',
        copy_failure: 'Errore durante la copia',
    },
}

let locale = 'en'
if (
    document.documentElement.lang !== undefined &&
    messages[document.documentElement.lang] !== undefined
) {
    locale = document.documentElement.lang
}

let doc_url_root =
    'URL_ROOT' in DOCUMENTATION_OPTIONS ? DOCUMENTATION_OPTIONS.URL_ROOT : ''
if (doc_url_root == '#') {
    doc_url_root = ''
}

/**
 * SVG files for our copy buttons
 */
const iconCheck = `<svg xmlns="http://www.w3.org/2000/svg" class="icon icon-tabler icon-tabler-check" width="44" height="44" viewBox="0 0 24 24" stroke-width="2" stroke="#22863a" fill="none" stroke-linecap="round" stroke-linejoin="round">
  <title>${messages[locale]['copy_success']}</title>
  <path stroke="none" d="M0 0h24v24H0z" fill="none"/>
  <path d="M5 12l5 5l10 -10" />
</svg>`

// If the user specified their own SVG use that, otherwise use the default
let iconCopy = `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="size-6">
  <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 0 0 2.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 0 0-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 0 0 .75-.75 2.25 2.25 0 0 0-.1-.664m-5.8 0A2.251 2.251 0 0 1 13.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25ZM6.75 12h.008v.008H6.75V12Zm0 3h.008v.008H6.75V15Zm0 3h.008v.008H6.75V18Z" />
</svg>
`
if (!iconCopy) {
    iconCopy = `<svg xmlns="http://www.w3.org/2000/svg" class="icon icon-tabler icon-tabler-copy" width="44" height="44" viewBox="0 0 24 24" stroke-width="1.5" stroke="#000000" fill="none" stroke-linecap="round" stroke-linejoin="round">
  <title>${messages[locale]['copy_to_clipboard']}</title>
  <path stroke="none" d="M0 0h24v24H0z" fill="none"/>
  <rect x="8" y="8" width="12" height="12" rx="2" />
  <path d="M16 8v-2a2 2 0 0 0 -2 -2h-8a2 2 0 0 0 -2 2v8a2 2 0 0 0 2 2h2" />
</svg>`
}

const codeCellId = (index) => `codecell${index}`

// Clears selected text since ClipboardJS will select the text when copying
const clearSelection = () => {
    if (window.getSelection) {
        window.getSelection().removeAllRanges()
    } else if ('selection' in document) {
        ;(document.selection as Selection).empty()
    }
}

// Changes tooltip text for a moment, then changes it back
// We want the timeout of our `success` class to be a bit shorter than the
// tooltip and icon change, so that we can hide the icon before changing back.
const timeoutIcon = 1500
const timeoutSuccessClass = 1500

const temporarilyChangeTooltip = (el, oldText, newText) => {
    el.setAttribute('data-tooltip', newText)
    el.classList.add('success')
    // Remove success a little bit sooner than we change the tooltip
    // So that we can use CSS to hide the copybutton first
    setTimeout(() => el.classList.remove('success'), timeoutSuccessClass)
    setTimeout(() => el.setAttribute('data-tooltip', oldText), timeoutIcon)
}

// Changes the copy button icon for two seconds, then changes it back
const temporarilyChangeIcon = (el) => {
    el.innerHTML = iconCheck
    setTimeout(() => {
        el.innerHTML = iconCopy
    }, timeoutIcon)
}

const addCopyButtonToCodeCells = () => {
    // If ClipboardJS hasn't loaded, wait a bit and try again. This
    // happens because we load ClipboardJS asynchronously.

    // Add copybuttons to all of our code cells
    const COPYBUTTON_SELECTOR = '.highlight pre'
    const codeCells = document.querySelectorAll(COPYBUTTON_SELECTOR)
    codeCells.forEach((codeCell, index) => {
        if (codeCell.id) {
            return
        }

        const id = codeCellId(index)
        codeCell.setAttribute('id', id)

        const clipboardButton = (id) =>
            `<button aria-label="Copy code to clipboard" class="copybtn o-tooltip--left" data-tooltip="${messages[locale]['copy']}" data-clipboard-target="#${id}">
      ${iconCopy}
    </button>`
        codeCell.insertAdjacentHTML('afterend', clipboardButton(id))
    })

    function escapeRegExp(string) {
        return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') // $& means the whole matched string
    }

    function filterText(target, excludes) {
        const clone = target.cloneNode(true) // clone as to not modify the live DOM
        clone.querySelectorAll(excludes).forEach((node) => node.remove())
        return clone.innerText
    }

    // Callback when a copy button is clicked. Will be passed the node that was clicked
    // should then grab the text and replace pieces of text that shouldn't be used in output
    function formatCopyText(
        textContent,
        copybuttonPromptText,
        isRegexp = false,
        onlyCopyPromptLines = true,
        removePrompts = true,
        copyEmptyLines = true,
        lineContinuationChar = '',
        hereDocDelim = ''
    ) {
        let regexp
        let match

        // Do we check for line continuation characters and "HERE-documents"?
        const useLineCont = !!lineContinuationChar
        const useHereDoc = !!hereDocDelim

        // create regexp to capture prompt and remaining line
        if (isRegexp) {
            regexp = new RegExp('^(' + copybuttonPromptText + ')(.*)')
        } else {
            regexp = new RegExp(
                '^(' + escapeRegExp(copybuttonPromptText) + ')(.*)'
            )
        }

        const outputLines = []
        let promptFound = false
        let gotLineCont = false
        let gotHereDoc = false
        const lineGotPrompt = []
        for (const line of textContent.split('\n')) {
            match = line.match(regexp)
            if (match || gotLineCont || gotHereDoc) {
                promptFound = regexp.test(line)
                lineGotPrompt.push(promptFound)
                if (removePrompts && promptFound) {
                    outputLines.push(match[2])
                } else {
                    outputLines.push(line)
                }
                gotLineCont = line.endsWith(lineContinuationChar) && useLineCont
                if (line.includes(hereDocDelim) && useHereDoc)
                    gotHereDoc = !gotHereDoc
            } else if (!onlyCopyPromptLines) {
                outputLines.push(line)
            } else if (copyEmptyLines && line.trim() === '') {
                outputLines.push(line)
            }
        }

        // If no lines with the prompt were found then just use original lines
        if (lineGotPrompt.some((v) => v === true)) {
            textContent = outputLines.join('\n')
        }

        // Remove a trailing newline to avoid auto-running when pasting
        if (textContent.endsWith('\n')) {
            textContent = textContent.slice(0, -1)
        }
        return textContent
    }

    const copyTargetText = (trigger) => {
        const target = document.querySelector(
            trigger.attributes['data-clipboard-target'].value
        )
        // get filtered text
        const excludes = ['.code-callout', '.linenos', '.language-apiheader']
        const text = Array.from(target.querySelectorAll('code'))
            .map((code) => filterText(code, excludes))
            .join('\n')
        return formatCopyText(text, '', false, true, true, true, '', '')
    }

    // Initialize with a callback so we can modify the text before copy
    const clipboard = new ClipboardJS('.copybtn', { text: copyTargetText })

    // Update UI with error/success messages
    clipboard.on('success', (event) => {
        clearSelection()
        temporarilyChangeTooltip(
            event.trigger,
            messages[locale]['copy'],
            messages[locale]['copy_success']
        )
        temporarilyChangeIcon(event.trigger)
    })

    clipboard.on('error', (event) => {
        temporarilyChangeTooltip(
            event.trigger,
            messages[locale]['copy'],
            messages[locale]['copy_failure']
        )
    })
}

export function initCopyButton() {
    addCopyButtonToCodeCells()
}

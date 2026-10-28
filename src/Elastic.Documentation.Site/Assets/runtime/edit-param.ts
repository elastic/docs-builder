import { $optional } from 'select-dom'

export function applyEditParam() {
    const urlParams = new URLSearchParams(window.location.search)
    if (urlParams.has('edit')) {
        $optional('.edit-this-page.hidden')?.classList.remove('hidden')
    }
}

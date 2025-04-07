export function initDismissibleBanner() {
    const banner = document.getElementById('dismissible-banner')
    const dismissButton = document.getElementById('dismissible-button')

    if (!localStorage.getItem('bannerDismissed')) {
        banner?.classList.remove('hidden')
    }

    dismissButton?.addEventListener('click', () => {
        banner?.classList.add('hidden')
        localStorage.setItem('bannerDismissed', 'true')
    })
}

export function initDismissibleBanner() {
    const banner = document.getElementById('dismissible-banner')
    const dismissButton = document.getElementById('dismissible-button')

    if (!localStorage.getItem('bannerDismissed')) {
        banner?.style.setProperty('display', 'flex')
    }

    dismissButton?.addEventListener('click', () => {
        banner?.style.setProperty('display', 'none')
        localStorage.setItem('bannerDismissed', 'true')
    })
}

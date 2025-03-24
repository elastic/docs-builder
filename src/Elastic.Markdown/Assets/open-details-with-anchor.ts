import { UAParser } from 'ua-parser-js';
const { getBrowser } = new UAParser();

// This is a fix for anchors in details elements in non-Chrome browsers.
export function OpenDetailsWithAnchor() {
	if (window.location.hash) {
		const target = document.querySelector(window.location.hash);
		if (target) {
			const closestDetails = target.closest('details');
			if (closestDetails) {
				const browser = getBrowser();
				if (browser.name !== 'Chrome') {
					closestDetails.open = true;
					target.scrollIntoView({ behavior: "instant", block: "start" });
				}
			}
		}
	}
}

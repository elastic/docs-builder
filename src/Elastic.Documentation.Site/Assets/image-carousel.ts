class ImageCarousel {
    private container: HTMLElement
    private slides: HTMLElement[]
    private indicators: HTMLElement[]
    private prevButton: HTMLElement | null
    private nextButton: HTMLElement | null
    private currentIndex: number = 0
    private touchStartX: number = 0
    private touchEndX: number = 0

    constructor(container: HTMLElement) {
        this.container = container
        if (!this.container) {
            console.warn('Carousel container element is null or undefined')
            return
        }

        this.slides = Array.from(
            this.container.querySelectorAll('.carousel-slide')
        )
        
        // Don't initialize if no slides
        if (this.slides.length === 0) {
            console.warn('No carousel slides found')
            return
        }
        
        this.indicators = Array.from(
            this.container.querySelectorAll('.carousel-indicator')
        )
        this.prevButton = this.container.querySelector('.carousel-prev')
        this.nextButton = this.container.querySelector('.carousel-next')

        this.initializeSlides()
        this.setupEventListeners()
        this.positionControls()
    }

    private initializeSlides(): void {
        // Initialize all slides as inactive
        this.slides.forEach((slide, index) => {
            this.setSlideState(slide, index === 0)
        })

        // Initialize indicators
        this.indicators.forEach((indicator, index) => {
            this.setIndicatorState(indicator, index === 0)
        })
    }

    private setSlideState(slide: HTMLElement, isActive: boolean): void {
        slide.setAttribute('data-active', isActive.toString())
        slide.style.display = isActive ? 'block' : 'none'
        slide.style.opacity = isActive ? '1' : '0'
    }

    private setIndicatorState(indicator: HTMLElement, isActive: boolean): void {
        indicator.setAttribute('data-active', isActive.toString())
    }

    private setupEventListeners(): void {
        // Navigation controls
        this.prevButton?.addEventListener('click', () => this.prevSlide())
        this.nextButton?.addEventListener('click', () => this.nextSlide())

        // Indicators
        this.indicators.forEach((indicator, index) => {
            indicator.addEventListener('click', () => this.goToSlide(index))
        })

        // Handle image clicks for modal
        this.slides.forEach(slide => {
            const imageLink = slide.querySelector('.carousel-image-reference')
            if (imageLink) {
                imageLink.addEventListener('click', (e) => {
                    e.preventDefault()
                    const modalId = imageLink.getAttribute('data-modal-id')
                    if (modalId) {
                        const modal = document.getElementById(modalId)
                        if (modal) {
                            modal.style.display = 'flex'
                        }
                    }
                })
            }
        })

        // Keyboard navigation
        document.addEventListener('keydown', (e) => {
            if (!this.isInViewport()) return

            if (e.key === 'ArrowLeft') this.prevSlide()
            else if (e.key === 'ArrowRight') this.nextSlide()
        })

        // Touch events
        this.container.addEventListener('touchstart', (e) => {
            this.touchStartX = e.changedTouches[0].screenX
        })

        this.container.addEventListener('touchend', (e) => {
            this.touchEndX = e.changedTouches[0].screenX
            this.handleSwipe()
        })
    }

    private prevSlide(): void {
        const newIndex =
            (this.currentIndex - 1 + this.slides.length) % this.slides.length
        this.goToSlide(newIndex)
    }

    private nextSlide(): void {
        const newIndex = (this.currentIndex + 1) % this.slides.length
        this.goToSlide(newIndex)
    }

    private goToSlide(index: number): void {
        // Update slides
        this.setSlideState(this.slides[this.currentIndex], false)
        this.setSlideState(this.slides[index], true)

        // Update indicators
        if (this.indicators.length > 0) {
            this.setIndicatorState(this.indicators[this.currentIndex], false)
            this.setIndicatorState(this.indicators[index], true)
        }

        this.currentIndex = index
    }

    private handleSwipe(): void {
        const threshold = 50
        const diff = this.touchStartX - this.touchEndX

        if (Math.abs(diff) < threshold) return

        if (diff > 0) this.nextSlide()
        else this.prevSlide()
    }

    private isInViewport(): boolean {
        const rect = this.container.getBoundingClientRect()
        return (
            rect.top >= 0 &&
            rect.left >= 0 &&
            rect.bottom <=
                (window.innerHeight || document.documentElement.clientHeight) &&
            rect.right <=
                (window.innerWidth || document.documentElement.clientWidth)
        )
    }

    private positionControls(): void {
        if (!this.prevButton || !this.nextButton) return

        // Wait for images to load before positioning
        const images = Array.from(this.container.querySelectorAll('img'))
        if (images.length === 0) return

        let loadedCount = 0
        const totalImages = images.length

        const positionAfterLoad = () => {
            loadedCount++
            if (loadedCount === totalImages) {
                this.calculateControlPosition()
            }
        }

        images.forEach(img => {
            if (img.complete) {
                positionAfterLoad()
            } else {
                img.addEventListener('load', positionAfterLoad)
                img.addEventListener('error', positionAfterLoad) // Handle failed loads
            }
        })
    }

    private calculateControlPosition(): void {
        if (!this.prevButton || !this.nextButton) return

        const images = Array.from(this.container.querySelectorAll('img'))
        let minHeight = Infinity

        // Find the smallest image height among all images
        images.forEach(img => {
            const height = img.offsetHeight
            if (height > 0 && height < minHeight) {
                minHeight = height
            }
        })

        // Position controls at 40% the height of the smallest image
        // But ensure a minimum distance from the top (50px) and don't go below 80% of the smallest image
        if (minHeight !== Infinity && minHeight > 0) {
            const fortyPercentHeight = Math.floor(minHeight * 0.4)
            const minTop = 50 // Minimum 50px from top
            const maxTop = Math.floor(minHeight * 0.8) // Maximum 80% down the smallest image
            
            const controlTop = Math.max(minTop, Math.min(fortyPercentHeight, maxTop))
            
            this.prevButton.style.top = `${controlTop}px`
            this.nextButton.style.top = `${controlTop}px`
            
            // Debug logging (remove in production)
            console.log(`Carousel controls positioned: minHeight=${minHeight}px, controlTop=${controlTop}px`)
        }
    }
}

// Export function to initialize carousels
export function initImageCarousel(): void {
    // Find all carousel containers
    const carousels = document.querySelectorAll('.carousel-container')

    // Process each carousel
    carousels.forEach((carouselElement) => {
        const carousel = carouselElement as HTMLElement

        // Skip if carousel already has slides (server-rendered)
        const existingSlides = carousel.querySelectorAll('.carousel-slide')
        if (existingSlides.length > 0) {
            // Just initialize the existing carousel
            new ImageCarousel(carousel)
            return
        }

        // Get the existing track for dynamic carousels
        let track = carousel.querySelector('.carousel-track')
        if (!track) {
            track = document.createElement('div')
            track.className = 'carousel-track'
            carousel.appendChild(track)
        }

        // Find all image links that might be related to this carousel
        const section = findSectionForCarousel(carousel)
        if (!section) return

        // First, collect all images we want in the carousel
        const allImageLinks = Array.from(
            section.querySelectorAll('a[href*="epr.elastic.co"]')
        )

        // Track URLs to prevent duplicates
        const processedUrls = new Set()

        // Process the existing slides first
        existingSlides.forEach((slide) => {
            const imageRef = slide.querySelector('a.carousel-image-reference')
            if (imageRef && imageRef instanceof HTMLAnchorElement) {
                processedUrls.add(imageRef.href)
            }
        })

        // Find standalone images (not already in carousel slides)
        const standaloneImages = allImageLinks.filter((img) => {
            if (processedUrls.has(img.href)) {
                return false // Skip if already processed
            }

            // Don't count images already in carousel slides
            const isInCarousel = img.closest('.carousel-slide') !== null
            if (isInCarousel) {
                processedUrls.add(img.href)
                return false
            }

            processedUrls.add(img.href)
            return true
        })

        // Add the standalone images to the carousel
        let slideIndex = existingSlides.length
        standaloneImages.forEach((imgLink) => {
            // Find container to hide
            const imgContainer = findClosestContainer(imgLink, carousel)

            // Create a new slide
            const slide = document.createElement('div')
            slide.className = 'carousel-slide'
            slide.setAttribute('data-index', slideIndex.toString())
            if (slideIndex === 0 && existingSlides.length === 0) {
                slide.setAttribute('data-active', 'true')
            }

            // Create a proper carousel image reference wrapper
            const imageRef = document.createElement('a')
            imageRef.className = 'carousel-image-reference'
            imageRef.href = imgLink.href
            imageRef.target = '_blank'

            // Clone the image
            const img = imgLink.querySelector('img')
            if (img) {
                imageRef.appendChild(img.cloneNode(true))
            }

            slide.appendChild(imageRef)
            track.appendChild(slide)

            // Hide the original container properly
            if (imgContainer) {
                try {
                    // Find the parent element that might be a paragraph or div containing the image
                    let parent = imgContainer
                    let maxAttempts = 3 // Don't go too far up the tree

                    while (
                        maxAttempts > 0 &&
                        parent &&
                        parent !== document.body
                    ) {
                        // If this is one of these elements, hide it
                        if (
                            parent.tagName === 'P' ||
                            (parent.tagName === 'DIV' &&
                                !parent.classList.contains(
                                    'carousel-container'
                                ))
                        ) {
                            parent.style.display = 'none'
                            break
                        }
                        parent = parent.parentElement
                        maxAttempts--
                    }

                    // If we couldn't find a suitable parent, just hide the container itself
                    if (maxAttempts === 0) {
                        imgContainer.style.display = 'none'
                    }
                } catch (e) {
                    console.error('Failed to hide original image:', e)
                }
            }

            slideIndex++
        })

        // Only set up controls if we have multiple slides
        const totalSlides = track.querySelectorAll('.carousel-slide').length
        if (totalSlides > 1) {
            // Add controls if they don't exist
            if (!carousel.querySelector('.carousel-prev')) {
                const prevButton = document.createElement('button')
                prevButton.type = 'button'
                prevButton.className = 'carousel-control carousel-prev'
                prevButton.setAttribute('aria-label', 'Previous slide')
                prevButton.innerHTML = '<span aria-hidden="true">←</span>'
                carousel.appendChild(prevButton)
            }

            if (!carousel.querySelector('.carousel-next')) {
                const nextButton = document.createElement('button')
                nextButton.type = 'button'
                nextButton.className = 'carousel-control carousel-next'
                nextButton.setAttribute('aria-label', 'Next slide')
                nextButton.innerHTML = '<span aria-hidden="true">→</span>'
                carousel.appendChild(nextButton)
            }

            // Add or update indicators
            let indicators = carousel.querySelector('.carousel-indicators')
            if (!indicators) {
                indicators = document.createElement('div')
                indicators.className = 'carousel-indicators'
                carousel.appendChild(indicators)
            } else {
                indicators.innerHTML = '' // Clear existing indicators
            }

            for (let i = 0; i < totalSlides; i++) {
                const indicator = document.createElement('button')
                indicator.type = 'button'
                indicator.className = 'carousel-indicator'
                indicator.setAttribute('data-index', i.toString())
                if (i === 0) {
                    indicator.setAttribute('data-active', 'true')
                }
                indicator.setAttribute('aria-label', `Go to slide ${i + 1}`)
                indicators.appendChild(indicator)
            }
        }

        // Initialize this carousel
        new ImageCarousel(carousel)
    })
}

// Helper to find a suitable container for an image
function findClosestContainer(
    element: Element,
    carousel: Element
): Element | null {
    let current = element
    while (
        current &&
        !current.contains(carousel) &&
        current !== document.body
    ) {
        // Stop at these elements
        if (
            current.tagName === 'P' ||
            current.tagName === 'DIV' ||
            current.classList.contains('carousel-container')
        ) {
            return current
        }
        current = current.parentElement!
    }
    return element
}

// Helper to find the section containing a carousel
function findSectionForCarousel(carousel: Element): Element | null {
    // Look for containing section, article, or main element
    let section = carousel.closest(
        'section, article, main, div.markdown-content'
    )
    if (!section) {
        // Fallback to parent element
        section = carousel.parentElement
    }
    return section
}

// Initialize all carousels when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    initImageCarousel()
})

/** True when the reader asked the OS for less animation. */
export function prefersReducedMotion(): boolean {
    return (
        typeof window.matchMedia === 'function' &&
        window.matchMedia('(prefers-reduced-motion: reduce)').matches
    )
}

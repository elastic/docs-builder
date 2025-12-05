/**
 * Message throttler for controlling the speed of streaming updates.
 * Non-React version that can be used in Zustand stores or other non-component code.
 *
 * Automatically speeds up after a configurable duration to drain buffers faster.
 */

const DEFAULT_THROTTLE_DELAY_MS = 25
const DEFAULT_FAST_DELAY_MS = 5
const DEFAULT_SPEEDUP_AFTER_MS = 5_000

export interface MessageThrottlerOptions<T> {
    delayInMs?: number
    fastDelayInMs?: number
    speedupAfterMs?: number
    onMessage: (message: T) => void
}

export class MessageThrottler<T> {
    private queue: T[] = []
    private timer: ReturnType<typeof setTimeout> | null = null
    private isProcessing = false
    private delayInMs: number
    private fastDelayInMs: number
    private speedupAfterMs: number
    private startTime: number | null = null
    private onMessage: (message: T) => void

    constructor({
        delayInMs = DEFAULT_THROTTLE_DELAY_MS,
        fastDelayInMs = DEFAULT_FAST_DELAY_MS,
        speedupAfterMs = DEFAULT_SPEEDUP_AFTER_MS,
        onMessage,
    }: MessageThrottlerOptions<T>) {
        this.delayInMs = delayInMs
        this.fastDelayInMs = fastDelayInMs
        this.speedupAfterMs = speedupAfterMs
        this.onMessage = onMessage
    }

    /**
     * Start the speedup timer. Call this when the first meaningful content arrives.
     */
    startSpeedupTimer(): void {
        if (this.startTime === null) {
            this.startTime = Date.now()
        }
    }

    private getCurrentDelay(): number {
        if (this.startTime === null) {
            return this.delayInMs
        }
        const elapsed = Date.now() - this.startTime
        return elapsed >= this.speedupAfterMs
            ? this.fastDelayInMs
            : this.delayInMs
    }

    private processNext = () => {
        if (this.queue.length === 0) {
            this.isProcessing = false
            this.timer = null
            return
        }

        const nextMessage = this.queue.shift()!
        this.onMessage(nextMessage)

        if (this.queue.length > 0) {
            this.timer = setTimeout(this.processNext, this.getCurrentDelay())
        } else {
            this.isProcessing = false
            this.timer = null
        }
    }

    push(message: T): void {
        this.queue.push(message)
        if (!this.isProcessing) {
            this.isProcessing = true
            this.timer = setTimeout(this.processNext, this.getCurrentDelay())
        }
    }

    clear(): void {
        if (this.timer) {
            clearTimeout(this.timer)
            this.timer = null
        }
        this.queue = []
        this.isProcessing = false
    }

    get pending(): number {
        return this.queue.length
    }
}

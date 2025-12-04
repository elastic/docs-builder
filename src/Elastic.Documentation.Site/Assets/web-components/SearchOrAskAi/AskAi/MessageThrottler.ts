/**
 * Message throttler for controlling the speed of streaming updates.
 * Non-React version that can be used in Zustand stores or other non-component code.
 */

const DEFAULT_THROTTLE_DELAY_MS = 25

export interface MessageThrottlerOptions<T> {
    delayInMs?: number
    onMessage: (message: T) => void
}

export class MessageThrottler<T> {
    private queue: T[] = []
    private timer: ReturnType<typeof setTimeout> | null = null
    private isProcessing = false
    private delayInMs: number
    private onMessage: (message: T) => void

    constructor({
        delayInMs = DEFAULT_THROTTLE_DELAY_MS,
        onMessage,
    }: MessageThrottlerOptions<T>) {
        this.delayInMs = delayInMs
        this.onMessage = onMessage
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
            this.timer = setTimeout(this.processNext, this.delayInMs)
        } else {
            this.isProcessing = false
            this.timer = null
        }
    }

    push(message: T): void {
        this.queue.push(message)
        if (!this.isProcessing) {
            this.isProcessing = true
            this.timer = setTimeout(this.processNext, this.delayInMs)
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

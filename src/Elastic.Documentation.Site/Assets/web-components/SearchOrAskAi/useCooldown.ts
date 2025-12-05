import { useCooldownActions } from './cooldown.store'
import { ModalMode } from './modalmodes'
import { useEffect, useRef } from 'react'

interface UseCooldownParams {
    domain: ModalMode
    cooldown: number | null
    onCooldownFinished: () => void
}

export function useCooldown({
    domain,
    cooldown,
    onCooldownFinished,
}: UseCooldownParams) {
    const { updateCooldown } = useCooldownActions()
    const intervalRef = useRef<number | null>(null)
    const onFinishedRef = useRef(onCooldownFinished)

    useEffect(() => {
        onFinishedRef.current = onCooldownFinished
    }, [onCooldownFinished])

    useEffect(() => {
        // Clear existing interval
        if (intervalRef.current !== null) {
            clearInterval(intervalRef.current)
            intervalRef.current = null
        }

        // Start countdown if cooldown is set
        if (cooldown !== null && cooldown > 0) {
            // Use a ref to track current countdown value across interval ticks
            const countdownRef = { current: cooldown }

            intervalRef.current = window.setInterval(() => {
                countdownRef.current -= 1

                if (countdownRef.current <= 0) {
                    if (intervalRef.current !== null) {
                        clearInterval(intervalRef.current)
                        intervalRef.current = null
                    }
                    updateCooldown(domain, null)
                    onFinishedRef.current()
                } else {
                    updateCooldown(domain, countdownRef.current)
                }
            }, 1000)
        }

        return () => {
            if (intervalRef.current !== null) {
                clearInterval(intervalRef.current)
                intervalRef.current = null
            }
        }
    }, [domain, cooldown, updateCooldown])
}

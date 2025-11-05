import { useEffect, useRef, useState } from 'react'
import { useCooldown, useModalActions } from '../modal.store'

/**
 * Hook to manage countdown timer synchronization with the store cooldown.
 */
export function useCountdownTimer(initialCooldown?: number | null): {
    countdown: number | null
    cooldownFinished: boolean
} {
    const storeCooldown = useCooldown()
    const { notifyCooldownFinished } = useModalActions()
    const cooldownSource = initialCooldown ?? storeCooldown
    const [countdown, setCountdown] = useState<number | null>(cooldownSource)
    const intervalRef = useRef<number | null>(null)
    const previousCountdownRef = useRef<number | null>(cooldownSource)
    const [cooldownFinished, setCooldownFinished] = useState(false)
    
    useEffect(() => {
        if (intervalRef.current !== null) {
            return
        }
        
        if (cooldownSource !== null && cooldownSource > 0) {
            if (countdown === null || cooldownSource > countdown) {
                setCountdown(cooldownSource)
            }
        } else if (cooldownSource === null) {
            setCountdown(null)
        }
    }, [cooldownSource])
    
    useEffect(() => {
        if (
            cooldownSource !== null &&
            cooldownSource > 0 &&
            intervalRef.current === null
        ) {
            setCountdown(cooldownSource)
            intervalRef.current = setInterval(() => {
                setCountdown((prev) => {
                    if (prev === null || prev <= 0) {
                        if (intervalRef.current) {
                            clearInterval(intervalRef.current)
                            intervalRef.current = null
                        }
                        return null
                    }
                    const newValue = prev - 1
                    // If countdown reaches 0 or below, clear it immediately
                    if (newValue <= 0) {
                        if (intervalRef.current) {
                            clearInterval(intervalRef.current)
                            intervalRef.current = null
                        }
                        notifyCooldownFinished()
                        return null
                    }
                    return newValue
                })
            }, 1000) as unknown as number
        } else if (
            (cooldownSource === null || cooldownSource <= 0) &&
            intervalRef.current !== null
        ) {
            // Stop timer if cooldown expired
            clearInterval(intervalRef.current)
            intervalRef.current = null
            setCountdown(null)
            notifyCooldownFinished()
        }
        
        return () => {
            // Clean up interval on unmount
            if (intervalRef.current !== null) {
                clearInterval(intervalRef.current)
                intervalRef.current = null
            }
        }
    }, [cooldownSource])
    
    
    // Track when cooldown transitions from non-null to null
    useEffect(() => {
        const wasActive = previousCountdownRef.current !== null && previousCountdownRef.current > 0
        const isActive = countdown !== null && countdown > 0
        
        if (wasActive && !isActive) {
            // Cooldown just finished
            setCooldownFinished(true)
            // Reset the flag after a short delay to allow components to react
            const timer = setTimeout(() => setCooldownFinished(false), 100)
            return () => clearTimeout(timer)
        } else {
            setCooldownFinished(false)
        }
        
        previousCountdownRef.current = countdown
    }, [countdown])
    
    return { countdown, cooldownFinished }
}


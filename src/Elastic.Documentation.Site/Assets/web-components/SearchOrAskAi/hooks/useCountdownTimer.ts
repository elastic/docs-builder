import { useEffect, useRef, useState } from 'react'
import { useCooldown } from '../modal.store'

/**
 * Hook to manage countdown timer synchronization with the store cooldown.
 * Automatically starts/stops timer and syncs with store state.
 * 
 * @param onCountdownChange - Optional callback when countdown changes
 * @returns Object with countdown value and cooldownFinished flag
 */
export function useCountdownTimer(
    onCountdownChange?: (countdown: number | null) => void
): { countdown: number | null; cooldownFinished: boolean } {
    const storeCooldown = useCooldown()
    const [countdown, setCountdown] = useState<number | null>(storeCooldown)
    const intervalRef = useRef<number | null>(null)
    const previousCountdownRef = useRef<number | null>(storeCooldown)
    const [cooldownFinished, setCooldownFinished] = useState(false)
    
    // Handle new 429 errors and initialize countdown from store
    useEffect(() => {
        if (storeCooldown !== null && storeCooldown > 0) {
            // Only update if this is a new/longer cooldown
            if (countdown === null || storeCooldown > countdown) {
                setCountdown(storeCooldown)
            }
        } else if (storeCooldown === null) {
            // Store cleared, reset countdown
            setCountdown(null)
        }
    }, [storeCooldown])
    
    // Start/stop timer based on store cooldown
    useEffect(() => {
        // If there's an active cooldown in the store and no timer running, start one
        if (storeCooldown !== null && storeCooldown > 0 && intervalRef.current === null) {
            setCountdown(storeCooldown)
            intervalRef.current = setInterval(() => {
                setCountdown((prev) => {
                    // If already cleared or invalid, stop timer
                    if (prev === null || prev <= 0) {
                        if (intervalRef.current) {
                            clearInterval(intervalRef.current)
                            intervalRef.current = null
                        }
                        onCountdownChange?.(null)
                        return null
                    }
                    const newValue = prev - 1
                    // If countdown reaches 0 or below, clear it immediately
                    if (newValue <= 0) {
                        if (intervalRef.current) {
                            clearInterval(intervalRef.current)
                            intervalRef.current = null
                        }
                        onCountdownChange?.(null)
                        return null
                    }
                    onCountdownChange?.(newValue)
                    return newValue
                })
            }, 1000) as unknown as number
        } else if ((storeCooldown === null || storeCooldown <= 0) && intervalRef.current !== null) {
            // Stop timer if cooldown expired
            clearInterval(intervalRef.current)
            intervalRef.current = null
            setCountdown(null)
            onCountdownChange?.(null)
        }
        
        return () => {
            // Clean up interval on unmount
            if (intervalRef.current !== null) {
                clearInterval(intervalRef.current)
                intervalRef.current = null
            }
        }
    }, [storeCooldown, onCountdownChange])
    
    // Sync local countdown with store when store changes externally (only when timer not running)
    useEffect(() => {
        // Only sync if timer is not running to avoid interfering with active countdown
        if (intervalRef.current === null) {
            if (storeCooldown !== countdown) {
                // Only sync if store has a value - if store is null, countdown should stay null (cooldown expired)
                if (storeCooldown !== null && storeCooldown > 0) {
                    setCountdown(storeCooldown)
                } else if (storeCooldown === null) {
                    // Store is cleared, ensure countdown is also cleared
                    setCountdown(null)
                }
            }
        }
    }, [storeCooldown, countdown])
    
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


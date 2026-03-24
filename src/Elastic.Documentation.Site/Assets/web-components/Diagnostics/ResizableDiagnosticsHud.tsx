import { DiagnosticsHud } from './DiagnosticsHud'
import { useDiagnosticsStore } from './diagnostics.store'
import { EuiResizableButton, keys, useEuiTheme } from '@elastic/eui'
import * as React from 'react'
import { useState, useCallback, useRef } from 'react'

const MIN_HEIGHT = 125
const DEFAULT_HEIGHT = 200
const MAX_HEIGHT_VH = 66
const MAX_HEIGHT = `${MAX_HEIGHT_VH}vh`
const KEYBOARD_STEP = 10

const getMaxHeightPx = () => (window.innerHeight * MAX_HEIGHT_VH) / 100

const getPointerY = (
    e: TouchEvent | MouseEvent | React.MouseEvent | React.TouchEvent
): number =>
    (e as TouchEvent).targetTouches
        ? (e as TouchEvent).targetTouches[0].pageY
        : (e as MouseEvent).pageY

export const ResizableDiagnosticsHud: React.FC = () => {
    const { isHudOpen } = useDiagnosticsStore()
    const { euiTheme } = useEuiTheme()
    const [panelHeight, setPanelHeight] = useState(DEFAULT_HEIGHT)
    const startHeight = useRef(panelHeight)
    const startY = useRef(0)

    const onPointerMove = useCallback((e: MouseEvent | TouchEvent) => {
        const offset = startY.current - getPointerY(e)
        const maxPx = getMaxHeightPx()
        setPanelHeight(
            Math.max(MIN_HEIGHT, Math.min(startHeight.current + offset, maxPx))
        )
    }, [])

    const onPointerUp = useCallback(() => {
        startY.current = 0
        window.removeEventListener('mousemove', onPointerMove)
        window.removeEventListener('mouseup', onPointerUp)
        window.removeEventListener('touchmove', onPointerMove)
        window.removeEventListener('touchend', onPointerUp)
    }, [onPointerMove])

    const onResizeStart = useCallback(
        (e: React.MouseEvent | React.TouchEvent) => {
            startY.current = getPointerY(e)
            startHeight.current = panelHeight
            window.addEventListener('mousemove', onPointerMove)
            window.addEventListener('mouseup', onPointerUp)
            window.addEventListener('touchmove', onPointerMove)
            window.addEventListener('touchend', onPointerUp)
        },
        [panelHeight, onPointerMove, onPointerUp]
    )

    const onKeyDown = useCallback((e: React.KeyboardEvent) => {
        const maxPx = getMaxHeightPx()
        switch (e.key) {
            case keys.ARROW_UP:
                e.preventDefault()
                setPanelHeight((h) => Math.min(h + KEYBOARD_STEP, maxPx))
                break
            case keys.ARROW_DOWN:
                e.preventDefault()
                setPanelHeight((h) => Math.max(h - KEYBOARD_STEP, MIN_HEIGHT))
                break
        }
    }, [])

    if (!isHudOpen) {
        return null
    }

    return (
        <div
            style={{
                display: 'flex',
                flexDirection: 'column',
                height: panelHeight,
                maxHeight: MAX_HEIGHT,
                backgroundColor: euiTheme.colors.body,
                borderTop: `1px solid ${euiTheme.border.color}`,
                flexShrink: 0,
            }}
        >
            <EuiResizableButton
                isHorizontal={false}
                onMouseDown={onResizeStart}
                onTouchStart={onResizeStart}
                onKeyDown={onKeyDown}
                aria-label="Resize diagnostics panel"
            />
            <div
                style={{
                    flex: 1,
                    minHeight: 0,
                    display: 'flex',
                    flexDirection: 'column',
                }}
            >
                <DiagnosticsHud />
            </div>
        </div>
    )
}

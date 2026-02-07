import { DiagnosticsHud } from './DiagnosticsHud'
import { useDiagnosticsStore } from './diagnostics.store'
import { EuiResizableButton, keys } from '@elastic/eui'
import * as React from 'react'
import { useState, useCallback, useRef } from 'react'

const MIN_PANEL_HEIGHT = 150
const DEFAULT_PANEL_HEIGHT = 300
const MAX_PANEL_HEIGHT_VH = 50
const MAX_PANEL_HEIGHT = '50vh'
const KEYBOARD_OFFSET = 10

const getMaxPanelHeightPx = () =>
    (window.innerHeight * MAX_PANEL_HEIGHT_VH) / 100

const getMouseOrTouchY = (
    e: TouchEvent | MouseEvent | React.MouseEvent | React.TouchEvent
): number => {
    const y = (e as TouchEvent).targetTouches
        ? (e as TouchEvent).targetTouches[0].pageY
        : (e as MouseEvent).pageY
    return y
}

export const ResizableDiagnosticsHud: React.FC = () => {
    const { isHudOpen } = useDiagnosticsStore()
    const [panelHeight, setPanelHeight] = useState(DEFAULT_PANEL_HEIGHT)
    const initialPanelHeight = useRef(panelHeight)
    const initialMouseY = useRef(0)

    const onMouseMove = useCallback((e: MouseEvent | TouchEvent) => {
        const mouseOffset = initialMouseY.current - getMouseOrTouchY(e)
        const changedPanelHeight = initialPanelHeight.current + mouseOffset
        const maxPx = getMaxPanelHeightPx()
        setPanelHeight(
            Math.max(MIN_PANEL_HEIGHT, Math.min(changedPanelHeight, maxPx))
        )
    }, [])

    const onMouseUp = useCallback(() => {
        initialMouseY.current = 0
        window.removeEventListener('mousemove', onMouseMove)
        window.removeEventListener('mouseup', onMouseUp)
        window.removeEventListener('touchmove', onMouseMove)
        window.removeEventListener('touchend', onMouseUp)
    }, [onMouseMove])

    const onResizeStart = useCallback(
        (e: React.MouseEvent | React.TouchEvent) => {
            initialMouseY.current = getMouseOrTouchY(e)
            initialPanelHeight.current = panelHeight
            window.addEventListener('mousemove', onMouseMove)
            window.addEventListener('mouseup', onMouseUp)
            window.addEventListener('touchmove', onMouseMove)
            window.addEventListener('touchend', onMouseUp)
        },
        [panelHeight, onMouseMove, onMouseUp]
    )

    const onKeyDown = useCallback((e: React.KeyboardEvent) => {
        const maxPx = getMaxPanelHeightPx()
        switch (e.key) {
            case keys.ARROW_UP:
                e.preventDefault()
                setPanelHeight((h) => Math.min(h + KEYBOARD_OFFSET, maxPx))
                break
            case keys.ARROW_DOWN:
                e.preventDefault()
                setPanelHeight((h) =>
                    Math.max(h - KEYBOARD_OFFSET, MIN_PANEL_HEIGHT)
                )
                break
        }
    }, [])

    if (!isHudOpen) {
        return null
    }

    return (
        <div
            className="fixed bottom-0 left-0 right-0 flex flex-col bg-grey-140 border-t border-grey-120 shadow-2xl"
            style={{
                height: panelHeight,
                maxHeight: MAX_PANEL_HEIGHT,
                zIndex: 9998,
            }}
        >
            <EuiResizableButton
                isHorizontal={false}
                onMouseDown={onResizeStart}
                onTouchStart={onResizeStart}
                onKeyDown={onKeyDown}
                aria-label="Resize diagnostics panel"
            />
            <div className="flex-1 min-h-0 flex flex-col">
                <DiagnosticsHud embedded />
            </div>
        </div>
    )
}

import { useEffect, useState } from "react";
import { useSearchActions, useSearchTerm } from "./search.store";
import { useModalActions } from "./modal.store";

export const useSyncSearchParams = () => {
    const searchTerm = useSearchTerm()
    const { setSearchTerm, submitAskAiTerm } = useSearchActions()
    const { openModal } = useModalActions()

    const searchQueryParamKey = 'q'
    const [isInitialized, setIsInitialized] = useState(false)
    useEffect(() => {
        const params = new URLSearchParams(window.location.search)
        const urlQuery = params.get(searchQueryParamKey)
        if (urlQuery) {
            setSearchTerm(urlQuery)
            submitAskAiTerm(urlQuery)
        }
        setIsInitialized(true)
    }, [])

    useEffect(() => {
        if (!isInitialized) {
            return
        }
        const url = new URL(window.location)
        if (searchTerm) {
            url.searchParams.set(searchQueryParamKey, searchTerm)
        } else {
            url.searchParams.delete(searchQueryParamKey)
        }
        window.history.replaceState({}, '', url)
    }, [searchTerm, isInitialized])

    useEffect(() => {
        const url = new URL(window.location)
        if (url.searchParams.has(searchQueryParamKey)) {
            openModal()
        }
    }, [])
};

import { navigationSearchStore } from './navigationSearch.store'
import { act } from 'react'

describe('navigationSearch.store', () => {
    beforeEach(() => {
        // Reset store state before each test
        act(() => {
            navigationSearchStore.getState().actions.clearSearchTerm()
        })
    })

    describe('setSearchTerm', () => {
        it('should set search term', () => {
            // Arrange
            const searchTerm = 'elasticsearch'

            // Act
            act(() => {
                navigationSearchStore.getState().actions.setSearchTerm(searchTerm)
            })

            // Assert
            expect(navigationSearchStore.getState().searchTerm).toBe(searchTerm)
        })

        it('should update existing search term', () => {
            // Arrange
            act(() => {
                navigationSearchStore.getState().actions.setSearchTerm('old term')
            })

            // Act
            act(() => {
                navigationSearchStore.getState().actions.setSearchTerm('new term')
            })

            // Assert
            expect(navigationSearchStore.getState().searchTerm).toBe('new term')
        })
    })

    describe('clearSearchTerm', () => {
        it('should clear search term', () => {
            // Arrange
            act(() => {
                navigationSearchStore.getState().actions.setSearchTerm('test search')
            })

            // Act
            act(() => {
                navigationSearchStore.getState().actions.clearSearchTerm()
            })

            // Assert
            expect(navigationSearchStore.getState().searchTerm).toBe('')
        })
    })

    describe('initial state', () => {
        it('should have empty search term on initialization', () => {
            // Assert
            expect(navigationSearchStore.getState().searchTerm).toBe('')
        })
    })
})

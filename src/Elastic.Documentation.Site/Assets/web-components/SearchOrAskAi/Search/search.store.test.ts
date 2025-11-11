import { searchStore } from './search.store'
import { act } from 'react'

describe('search.store', () => {
    beforeEach(() => {
        // Reset store state before each test
        act(() => {
            searchStore.getState().actions.clearSearchTerm()
        })
    })

    describe('setSearchTerm', () => {
        it('should set search term', () => {
            // Arrange
            const searchTerm = 'elasticsearch'

            // Act
            act(() => {
                searchStore.getState().actions.setSearchTerm(searchTerm)
            })

            // Assert
            expect(searchStore.getState().searchTerm).toBe(searchTerm)
        })

        it('should update existing search term', () => {
            // Arrange
            act(() => {
                searchStore.getState().actions.setSearchTerm('old term')
            })

            // Act
            act(() => {
                searchStore.getState().actions.setSearchTerm('new term')
            })

            // Assert
            expect(searchStore.getState().searchTerm).toBe('new term')
        })
    })

    describe('clearSearchTerm', () => {
        it('should clear search term', () => {
            // Arrange
            act(() => {
                searchStore.getState().actions.setSearchTerm('test search')
            })

            // Act
            act(() => {
                searchStore.getState().actions.clearSearchTerm()
            })

            // Assert
            expect(searchStore.getState().searchTerm).toBe('')
        })
    })

    describe('initial state', () => {
        it('should have empty search term on initialization', () => {
            // Assert
            expect(searchStore.getState().searchTerm).toBe('')
        })
    })
})

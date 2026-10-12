export interface SavedMessage { role: 'user' | 'assistant'; text: string; citations?: number[] }
export interface SavedAction { mode: 'translate' | 'explain' | 'summarize'; source: string; result: string; page: number }
export interface HighlightRect { x: number; y: number; width: number; height: number }
export interface SavedHighlight { id: string; page: number; text: string; rects: HighlightRect[]; note?: string }
export interface SavedBookmark { id: string; page: number; title: string }
export interface ReadingState { page: number; zoom: number; messages: SavedMessage[]; actions: SavedAction[]; highlights?: SavedHighlight[]; bookmarks?: SavedBookmark[] }
export interface LibraryItem { id: string; name: string; size: number; updatedAt: string; page: number }
export interface LibraryDetail extends Omit<LibraryItem, 'page'> { state: ReadingState }

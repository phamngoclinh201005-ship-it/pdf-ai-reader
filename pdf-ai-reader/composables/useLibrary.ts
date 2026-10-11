import type { LibraryItem, LibraryDetail, ReadingState } from '~/types/library'

export function useLibrary() {
  const config = useRuntimeConfig()
  const options = { baseURL: config.public.apiBase, credentials: 'include' as const }
  return {
    list: () => $fetch<LibraryItem[]>('/api/library', options),
    detail: (id: string) => $fetch<LibraryDetail>(`/api/library/${encodeURIComponent(id)}`, options),
    file: (id: string) => $fetch<Blob>(`/api/library/${encodeURIComponent(id)}/file`, { ...options, responseType: 'blob' }),
    upload: (file: File) => { const body = new FormData(); body.append('file', file); return $fetch<LibraryItem>('/api/library', { ...options, method: 'POST', body }) },
    save: (id: string, body: ReadingState) => $fetch(`/api/library/${encodeURIComponent(id)}/state`, { ...options, method: 'PUT', body }),
    remove: (id: string) => $fetch(`/api/library/${encodeURIComponent(id)}`, { ...options, method: 'DELETE' }),
  }
}

const destinations = new Set(['/workspace', '/library', '/settings', '/admin'])
export function safeAuthRedirect(value: unknown): string {
  if (typeof value !== 'string' || !value.startsWith('/') || value.startsWith('//')) return '/workspace'
  try {
    const url = new URL(value, 'https://paperflow.internal')
    if (url.origin !== 'https://paperflow.internal' || !destinations.has(url.pathname)) return '/workspace'
    return url.pathname + url.search + url.hash
  } catch { return '/workspace' }
}

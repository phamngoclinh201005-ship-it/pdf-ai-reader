export function providerFailure(error: unknown) {
  const e = error as { name?: string; status?: number; statusCode?: number; message?: string; error?: { details?: unknown[] }; details?: unknown[] }
  const status = ['TimeoutError', 'AbortError'].includes(e?.name || '') ? 504 : Number(e?.status || e?.statusCode || 502)
  // Inspect provider metadata only to classify; never log raw messages/details.
  const details = JSON.stringify(e?.error?.details || e?.details || []) + String(e?.message || '')
  const dailyQuota = status === 429 && /per.?day|daily|billing|limit\s*[:=]\s*0|quota.?exceeded/i.test(details)
  return { status, kind: dailyQuota ? 'quota_exceeded' : status === 429 ? 'rate_limit' : status === 504 ? 'timeout' : 'provider_error', retryable: !dailyQuota && [408, 429, 500, 502, 503, 504].includes(status) }
}

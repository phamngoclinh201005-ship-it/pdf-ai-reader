import type { PaperFlowUser } from '~/types/auth';
import type { OtpDelivery } from '~/types/otp';

export const useAuth = () => {
  const user = useState<PaperFlowUser | null>('auth-user', () => null);
  const loading = useState('auth-loading', () => false);
  const sessionCheckedAt = useState('auth-checked-at', () => 0);
  const app = useNuxtApp() as ReturnType<typeof useNuxtApp> & { _paperflowSession?: Promise<PaperFlowUser | null> };

  const config = useRuntimeConfig();
  const headers = import.meta.server ? useRequestHeaders(['cookie']) : undefined;
  const api = <T>(path: string, options: Record<string, any> = {}) => $fetch<T>(path, {
    baseURL: config.public.apiBase,
    credentials: 'include',
    headers,
    ...options,
  });
  const { remember } = useOtpChallenge();
  const register = async (data: { email: string; displayName: string; password: string }) => {
    const response = await api<OtpDelivery>('/api/auth/register', { method: 'POST', body: data });
    if (response.timing) remember(data.email, response.timing);
    return response;
  };
  const verify = (email: string, code: string) =>
    api<{ message: string }>('/api/auth/verify-email', { method: 'POST', body: { email, code } });
  const resendOtp = async (email: string) => {
    const response = await api<OtpDelivery>('/api/auth/resend-otp', { method: 'POST', body: { email } });
    if (response.timing) remember(email, response.timing);
    return response;
  };
  const login = async (email: string, password: string) => {
    const result = await api<{ user: PaperFlowUser }>('/api/auth/login', { method: 'POST', body: { email, password } });
    user.value = result.user;
    sessionCheckedAt.value = Date.now();
    return result.user;
  };
  const me = () => {
    if (app._paperflowSession) return app._paperflowSession;
    loading.value = true;
    app._paperflowSession = (async () => {
      try {
        const result = await api<{ user: PaperFlowUser }>('/api/auth/me', { timeout: 3000 });
        user.value = result.user;
        return result.user;
      } catch { user.value = null; return null; }
      finally { loading.value = false; sessionCheckedAt.value = Date.now(); app._paperflowSession = undefined; }
    })();
    return app._paperflowSession;
  };
  const ensureSession = () => sessionCheckedAt.value && Date.now() - sessionCheckedAt.value < 30000
    ? Promise.resolve(user.value) : me();
  const logout = async () => {
    await api('/api/auth/logout', { method: 'POST' });
    user.value = null;
    sessionCheckedAt.value = Date.now();
  };
  return { user, loading, register, verify, resendOtp, login, me, ensureSession, logout };
};

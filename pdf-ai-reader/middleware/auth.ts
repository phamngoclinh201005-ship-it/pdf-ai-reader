import { safeAuthRedirect } from '~/utils/auth-redirect'
export default defineNuxtRouteMiddleware(async (to) => {
  const { me } = useAuth();
  const user = await me();
  if (!user) return navigateTo({ path: '/login', query: { redirect: safeAuthRedirect(to.fullPath) } });
});

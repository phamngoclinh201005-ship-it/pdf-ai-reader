export default defineNuxtRouteMiddleware(async () => {
  const { me } = useAuth();
  const user = await me();
  if (!user) return navigateTo({ path: '/login', query: { redirect: '/admin' } });
  if (user.role !== 'Admin') return navigateTo('/workspace');
});

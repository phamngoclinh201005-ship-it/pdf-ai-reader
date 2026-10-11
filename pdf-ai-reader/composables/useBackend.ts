
/** Health checks target the standalone .NET API; AI uses Nuxt endpoints. */
export const useBackend = () => {
  const config = useRuntimeConfig();

  const checkHealth = async () => {
    return await $fetch<{
      success: boolean;
      message: string;
      timestamp: string;
    }>('/api/health', {
      baseURL: config.public.apiBase,
    });
  };

  return {
    checkHealth,
  };
};


import { defineNuxtConfig } from 'nuxt/config';

export default defineNuxtConfig({
  compatibilityDate: '2025-01-01',
  buildDir: process.env.NUXT_BUILD_DIR || '.nuxt',
  ...(process.env.NUXT_BUILD_DIR ? { vite: { cacheDir: `${process.env.NUXT_BUILD_DIR}/cache/vite` } } : {}),

  modules: ['@nuxt/ui'],

  css: ['~/assets/css/main.css'],

  runtimeConfig: {
    backendApiBase: process.env.NUXT_BACKEND_API_BASE || 'https://paperflow-api-reif.onrender.com',
    googleTranslationEnabled: false,
    googleTranslationApiKey: '',
    geminiApiKey: process.env.NUXT_GEMINI_API_KEY || process.env.GEMINI_API_KEY || '',
    geminiModel: process.env.NUXT_GEMINI_MODEL || process.env.GEMINI_MODEL || 'gemini-2.5-flash',
    public: {
      apiBase: process.env.NUXT_PUBLIC_API_BASE ?? '',
    },
  },

  routeRules: {
    '/api/library': { proxy: `${process.env.NUXT_BACKEND_API_BASE || 'https://paperflow-api-reif.onrender.com'}/api/library` },
    '/api/library/**': { proxy: `${process.env.NUXT_BACKEND_API_BASE || 'https://paperflow-api-reif.onrender.com'}/api/library/**` },
    '/api/auth/**': { proxy: `${process.env.NUXT_BACKEND_API_BASE || 'https://paperflow-api-reif.onrender.com'}/api/auth/**` },
    '/api/health': { proxy: `${process.env.NUXT_BACKEND_API_BASE || 'https://paperflow-api-reif.onrender.com'}/api/health` },
  },

  app: {
    pageTransition: false,
    head: {
      title: 'PaperFlow — Đọc PDF cùng AI',
      link: [{ rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' }],
    },
  },
  experimental: { viewTransition: false },
  colorMode: { preference: 'dark', fallback: 'dark', classSuffix: '' },
  icon: { serverBundle: { collections: ['lucide'] } },
});

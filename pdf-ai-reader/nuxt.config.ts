
import { defineNuxtConfig } from 'nuxt/config';

export default defineNuxtConfig({
  compatibilityDate: '2025-01-01',

  modules: ['@nuxt/ui'],

  css: ['~/assets/css/main.css'],

  runtimeConfig: {
    geminiApiKey: process.env.GEMINI_API_KEY || '',
    geminiModel: process.env.GEMINI_MODEL || 'gemini-2.5-flash',
    public: {
      apiBase: process.env.NUXT_PUBLIC_API_BASE ?? '',
    },
  },

  routeRules: {
    '/api/auth/**': { proxy: `${process.env.NUXT_BACKEND_API_BASE || 'https://paperflow-api-reif.onrender.com'}/api/auth/**` },
    '/api/health': { proxy: `${process.env.NUXT_BACKEND_API_BASE || 'https://paperflow-api-reif.onrender.com'}/api/health` },
  },

  app: {
    pageTransition: { name: 'page', mode: 'out-in' },
    head: {
      title: 'PaperFlow — Đọc PDF cùng AI',
      link: [{ rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' }],
    },
  },
  experimental: { viewTransition: true },
  colorMode: { preference: 'dark', fallback: 'dark', classSuffix: '' },
  icon: { serverBundle: { collections: ['lucide'] } },
});

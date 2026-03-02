/**
 * i18next initialization and configuration.
 *
 * @description Sets up i18next with:
 * - Browser language detection (navigator, localStorage)
 * - English as default/fallback language
 * - Namespaces: `assets` (asset form strings), `common` (shared UI — future)
 * - Translation JSONs are bundled (not lazy-loaded) for simplicity
 *
 * Import this module as a side-effect in App.tsx to initialize i18n
 * before any component renders.
 */
import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import LanguageDetector from "i18next-browser-languagedetector";

import assetsEn from "@/locales/en/assets.json";
import commonEn from "@/locales/en/common.json";

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      en: {
        assets: assetsEn,
        common: commonEn,
      },
    },
    fallbackLng: "en",
    defaultNS: "common",
    ns: ["assets", "common"],
    interpolation: {
      escapeValue: false, // React already escapes
    },
    detection: {
      order: ["navigator", "localStorage", "htmlTag"],
      caches: ["localStorage"],
    },
  });

export default i18n;

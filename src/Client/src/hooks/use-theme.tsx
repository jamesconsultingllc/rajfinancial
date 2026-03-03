import { createContext, useContext, useEffect, useState, ReactNode } from "react";

type Theme = "dark" | "light" | "system";

interface ThemeContextType {
  /** The stored theme preference (may be "system"). */
  theme: Theme;
  /** The resolved theme applied to the document ("dark" or "light"). */
  resolvedTheme: "dark" | "light";
  toggleTheme: () => void;
  setTheme: (theme: Theme) => void;
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

/**
 * Resolves the effective theme class from the stored preference.
 *
 * @param preference - The stored theme preference
 * @returns "dark" or "light"
 */
function resolveTheme(preference: Theme): "dark" | "light" {
  if (preference === "system") {
    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  }
  return preference;
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState<Theme>(() => {
    if (typeof window !== "undefined") {
      const stored = localStorage.getItem("raj-theme") as Theme;
      if (stored) return stored;
      return "system";
    }
    return "dark";
  });

  const resolvedTheme = resolveTheme(theme);

  useEffect(() => {
    const root = window.document.documentElement;
    root.classList.remove("light", "dark");
    root.classList.add(resolvedTheme);
    localStorage.setItem("raj-theme", theme);
  }, [theme, resolvedTheme]);

  // Listen for OS theme changes when set to "system"
  useEffect(() => {
    if (theme !== "system") return;
    const mq = window.matchMedia("(prefers-color-scheme: dark)");
    const handler = () => {
      const root = window.document.documentElement;
      root.classList.remove("light", "dark");
      root.classList.add(mq.matches ? "dark" : "light");
    };
    mq.addEventListener("change", handler);
    return () => mq.removeEventListener("change", handler);
  }, [theme]);

  const toggleTheme = () => {
    setTheme(prev => {
      const resolved = resolveTheme(prev);
      return resolved === "dark" ? "light" : "dark";
    });
  };

  return (
    <ThemeContext.Provider value={{ theme, resolvedTheme, toggleTheme, setTheme }}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useTheme() {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error("useTheme must be used within a ThemeProvider");
  }
  return context;
}

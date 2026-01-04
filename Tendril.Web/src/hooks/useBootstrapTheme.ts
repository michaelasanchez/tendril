import { useState, useLayoutEffect } from 'react';

type Theme = 'light' | 'dark';

export const useBootstrapTheme = (initialOverride?: Theme) => {
  const [theme, setTheme] = useState<Theme>(() => {
    // 1. Priority: Debugging Override
    // If you passed 'dark' or 'light', we force it immediately.
    if (initialOverride) return initialOverride;

    // 2. Priority: Local Storage
    const stored = localStorage.getItem('theme') as Theme | null;
    if (stored) return stored;

    // 3. Priority: System Preference
    if (
      window.matchMedia &&
      window.matchMedia('(prefers-color-scheme: dark)').matches
    ) {
      return 'dark';
    }

    return 'light';
  });

  useLayoutEffect(() => {
    // This runs synchronously before the browser paints.
    // It handles the initial load AND all subsequent updates.
    document.documentElement.setAttribute('data-bs-theme', theme);
    localStorage.setItem('theme', theme);
  }, [theme]);

  const toggleTheme = () => {
    setTheme((prev) => (prev === 'light' ? 'dark' : 'light'));
  };

  return { theme, toggleTheme };
};
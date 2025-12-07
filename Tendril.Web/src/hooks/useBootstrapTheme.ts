import React, { useState, useLayoutEffect } from 'react';

/**
 * useBootstrapTheme Hook (Anti-Flicker Version)
 * * * Logic:
 * 1. Checks LocalStorage.
 * 2. Checks System Preference.
 * 3. Sets the attribute SYNCHRONOUSLY inside the state initializer to prevent 
 * the "white flash" before the first paint.
 */
export const useBootstrapTheme = () => {
  const [theme, setTheme] = useState(() => {
    // 1. Resolve the initial theme
    // We check localStorage first
    let initialTheme = localStorage.getItem('theme');
    
    // If nothing in storage, check system preference
    if (!initialTheme) {
      if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        initialTheme = 'dark';
      } else {
        initialTheme = 'light';
      }
    }

    // 2. CRITICAL: Set the attribute immediately. 
    // Doing this here (during the render phase of the first mount) ensures 
    // the DOM has the correct attribute before the browser paints the screen.
    // This solves the "White Flash" issue.
    document.documentElement.setAttribute('data-bs-theme', initialTheme);

    return initialTheme;
  });

  // 3. Handle subsequent updates
  // useLayoutEffect fires synchronously after all DOM mutations.
  // We use this to catch updates when the user clicks the toggle button.
  useLayoutEffect(() => {
    document.documentElement.setAttribute('data-bs-theme', theme);
    localStorage.setItem('theme', theme);
  }, [theme]);

  const toggleTheme = () => {
    setTheme((prevTheme) => (prevTheme === 'light' ? 'dark' : 'light'));
  };

  return { theme, toggleTheme };
};
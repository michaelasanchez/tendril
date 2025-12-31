import { useEffect, useState } from "react";
import { useLocalStorage } from "./useLocalStorage";

export type AppTheme = "light" | "dark";

const setThemeAttribute = (theme: AppTheme) => {
  const htmlTag = document.getElementsByTagName("html");

  htmlTag[0].setAttribute("data-bs-theme", theme);
};

export interface ThemeState {
  current: AppTheme;
  setTheme: (theme: AppTheme) => void;
}

export const useTheme = (themeKey: string): ThemeState => {
  const themeStorage = useLocalStorage(themeKey);

  const [theme, setTheme] = useState<AppTheme>(() => {
    const defaultTheme = themeStorage.exists()
      ? (themeStorage.fetch() as AppTheme)
      : window.matchMedia &&
        window.matchMedia("(prefers-color-scheme: dark)").matches
      ? "dark"
      : "light";

    setThemeAttribute(defaultTheme);

    return defaultTheme;
  });

  useEffect(() => {
    themeStorage.commit(theme.toString());
    setThemeAttribute(theme);
  }, [theme]);

  return {
    current: theme,
    setTheme,
  };
};

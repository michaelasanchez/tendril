import React from 'react';

export interface LocalStorageState {
  commit: (value: string) => void;
  exists: () => boolean;
  fetch: () => string | null;
}

const getState = (key: string): LocalStorageState => {
  const fetchValue = (): string | null => {
    return window.localStorage.getItem(key);
  };

  const commitValue = (value: string) => {
    window.localStorage.setItem(key, value);
  };

  const valueExists = (): boolean => {
    return !!window.localStorage.getItem(key);
  };

  return {
    fetch: fetchValue,
    commit: commitValue,
    exists: valueExists,
  };
};

export const useLocalStorage = (key: string): LocalStorageState => {
  const [state, setState] = React.useState<LocalStorageState>(getState(key));

  React.useEffect(() => setState(getState(key)), [key]);

  return state;
};

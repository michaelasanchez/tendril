import { useEffect, useState } from 'react';

export const useBreakpoint = (breakpoint = '992px') => {
  const [isMatch, setIsMatch] = useState(false);

  useEffect(() => {
    const media = window.matchMedia(`(min-width: ${breakpoint})`);

    // Initial check
    setIsMatch(media.matches);

    // Listener for resize
    const listener = (e: {
      matches: boolean | ((prevState: boolean) => boolean);
    }) => setIsMatch(e.matches);
    media.addEventListener('change', listener);

    return () => media.removeEventListener('change', listener);
  }, [breakpoint]);

  return isMatch;
};

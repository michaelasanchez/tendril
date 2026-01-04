import { useEffect, useRef } from 'react';

export const useEffectOnce = (effect: () => void) => {
  // Check if we have run already
  const hasRun = useRef(false);

  useEffect(() => {
    // If already run, exit early
    if (hasRun.current) return;

    // Mark as run
    hasRun.current = true;

    // Execute the effect
    effect();

    // Note: We deliberately do not return a cleanup function here
    // because this hook is intended for "fire and forget" operations.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);
};

import { useState, useEffect } from 'react';

export const useFetch = <T>(url: string, options?: RequestInit) => {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    const fetchData = async () => {
      setLoading(true);
      try {
        const response = await fetch(url, { 
          ...options, 
          signal: controller.signal // Connects the controller
        });
        
        if (!response.ok) throw new Error(response.statusText);
        
        const json = await response.json();
        setData(json);
        setError(null);
      } catch (err) {
        // Ignore abort errors, throw everything else
        if (err instanceof Error && err.name !== 'AbortError') {
          setError(err);
        }
      } finally {
        // Only turn off loading if we weren't aborted
        if (!controller.signal.aborted) {
            setLoading(false);
        }
      }
    };

    fetchData();

    return () => {
      controller.abort(); // Cancels the request on unmount/remount
    };
  }, [url]); // Re-runs if URL changes

  return { data, loading, error };
};
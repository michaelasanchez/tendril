import { useEffect, useState } from 'react';
import Spinner from 'react-bootstrap/Spinner';

type ElapsedClockProps = {
  runStart?: Date | string | null;
  // Adjusted to take the current time so it can calculate the difference
  formatElapsed: (start: Date, now: Date) => string;
};

export const ElapsedClock: React.FC<ElapsedClockProps> = ({
  runStart,
  formatElapsed,
}) => {
  const [now, setNow] = useState<Date>(new Date());

  useEffect(() => {
    // If no start time, don't bother with the interval
    if (!runStart) return;

    const interval = setInterval(() => {
      setNow(new Date());
    }, 1000);

    // Cleanup is crucial to prevent memory leaks
    return () => clearInterval(interval);
  }, [runStart]);

  if (!runStart) return null;

  // Ensure we have a Date object
  const start = typeof runStart === 'string' ? new Date(runStart) : runStart;

  return (
    <div>
      <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
        <Spinner animation="border" size="sm" />
        <span>Running...</span>
      </div>
      {/* Pass both start AND now to the formatter */}
      <div style={{ fontFamily: 'monospace', fontSize: '1.2rem' }}>
        {formatElapsed(start, now)}
      </div>
    </div>
  );
};
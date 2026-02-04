import cn from 'classnames';
import { useMemo } from 'react';
import { EventCard, EventGrouper } from '.';
import type { Event, Guid } from '../types/api';
import styles from './EventList.module.css';

interface EventListProps {
  className?: string;
  events: Event[];
  favorites?: Set<Guid>;
  lastRef?: (node: HTMLDivElement | null) => void;
  onEventClick?: (event: Event) => void;
  onFavorite?: (event: Event) => void;
}

export const EventList: React.FC<EventListProps> = ({
  className,
  events,
  favorites,
  lastRef,
  onEventClick,
  onFavorite,
}) => {
  const groups = useMemo(() => EventGrouper.byDay(events), [events]);

  return (
    <div className={cn(styles.EventList, className)}>
      {groups.map((g, i) => (
        <div key={i}>
          <h3 className={styles.DayLabel}>{g.label}</h3>
          <div className={styles.DayGroup}>
            {g.events.map((e) => (
              <EventCard
                key={e.id}
                className={styles.EventCard}
                event={e}
                favorite={favorites?.has(e.id)}
                ref={e === g.events[g.events.length - 1] ? lastRef : undefined}
                onClick={() => onEventClick?.(e)}
                onFavorite={() => onFavorite?.(e)}
              />
            ))}
          </div>
        </div>
      ))}
    </div>
  );
};

import { format } from "date-fns";
import { useMemo } from "react";
import { EventCard } from ".";
import type { Event } from "../types/api";
import styles from "./EventList.module.css";

interface EventListProps {
  events: Event[];
  from: Date;
  venueFilter?: string | null;
  onEventClick?: (event: Event) => void;
}

export const EventList: React.FC<EventListProps> = ({
  events,
  from,
  venueFilter = null,
  onEventClick,
}) => {
  const groups = useMemo(() => {
    const filtered = events.filter(
      (e) =>
        from < new Date(e.startUtc) &&
        (!venueFilter || e.venueName == venueFilter)
    );

    return groupEventsByDay(filtered);
  }, [events, from, venueFilter]);

  return (
    <div className={styles.EventList}>
      {Object.keys(groups).map((g) => (
        <div key={g}>
          <h3>{format(new Date(g), "MMM dd")}</h3>
          <div className={styles.DayGroup}>
            {groups[g].map((e) => (
              <EventCard
                key={e.id}
                className={styles.EventCard}
                event={e}
                onClick={() => onEventClick?.(e)}
              />
            ))}
          </div>
        </div>
      ))}
    </div>
  );

  function getDayKey(utc: string) {
    const d = new Date(utc);
    return d.toISOString().slice(0, 10); // "2025-12-08"
  }

  function groupEventsByDay(events: Event[]) {
    return events?.reduce((groups, e) => {
      const key = getDayKey(e.startUtc);

      if (!groups[key]) groups[key] = [];

      groups[key].push(e);

      return groups;
    }, {} as Record<string, Event[]>);
  }
};

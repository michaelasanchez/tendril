import { format, parse } from "date-fns";
import { useMemo } from "react";
import { EventCard } from ".";
import type { Event } from "../types/api";
import styles from "./EventList.module.css";

interface EventListProps {
  events: Event[];
  onEventClick?: (event: Event) => void;
}

interface EventGroup {
  label: string;
  events: Event[];
}

export const EventList: React.FC<EventListProps> = ({
  events,
  onEventClick,
}) => {
  const groups = useMemo(() => groupEventsByDay(events), [events]);

  return (
    <div className={styles.EventList}>
      {groups.map((g, i) => (
        <div key={i}>
          <h3 className={styles.DayLabel}>{g.label}</h3>
          <div className={styles.DayGroup}>
            {g.events.map((e) => (
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

  function groupEventsByDay(events: Event[]): EventGroup[] {
    const grouped = events.reduce((groups, event) => {
      const dateKey = event.startUtc.split("T")[0];
      if (!groups[dateKey]) {
        groups[dateKey] = [];
      }

      groups[dateKey].push(event);

      return groups;
    }, {} as Record<string, Event[]>);

    return Object.keys(grouped)
      .sort()
      .map((g) => {
        const dateObj = parse(g, "yyyy-MM-dd", new Date());

        return {
          label: format(dateObj, "MMM dd"),
          events: grouped[g],
        };
      });
  }
};

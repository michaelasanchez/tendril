import cn from "classnames";
import { format } from "date-fns";
import { useMemo } from "react";
import { Button, Card } from "react-bootstrap";
import type { Event } from "../types/api";
import styles from "./EventList.module.css";
import NoImage from "./no-image.svg";

interface EventListProps {
  events: Event[];
  from: Date;
  venueFilter?: string | null;
}

export const EventList: React.FC<EventListProps> = ({
  events,
  from,
  venueFilter = null,
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
              <Card key={e.id} className={styles.EventCard}>
                <div className={cn(!e.imageUrl && styles.NoImage)}>
                  <Card.Img variant="top" src={e.imageUrl ?? NoImage} />
                </div>
                <Card.Body className={styles.CardBody}>
                  <div className={styles.CardContent}>
                    <Card.Title>{e.title}</Card.Title>
                    <span className="text-muted">@</span>
                    <time>{format(new Date(e.startUtc), "hh:mm a")}</time>

                    <div className="text-muted">{e.venueName}</div>
                    {e.description && <div>{e.description}</div>}
                  </div>
                  {e.ticketUrl && (
                    <div className={styles.CardActions}>
                      <Button
                        href={e.ticketUrl}
                        target="blank"
                        variant="outline-info"
                      >
                        Tickets
                      </Button>
                    </div>
                  )}
                </Card.Body>
              </Card>
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

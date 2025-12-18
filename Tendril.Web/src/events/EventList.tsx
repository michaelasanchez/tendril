import { format } from "date-fns";
import { Card } from "react-bootstrap";
import type { Event } from "../types/api";
import styles from "./EventList.module.css";

interface EventListProps {
  events: Event[];
}

export const EventList: React.FC<EventListProps> = ({ events }) => {
  const groups = groupEventsByDay(events);

  return (
    <div className={styles.EventList}>
      {Object.keys(groups).map((g) => (
        <div key={g}>
          <h3>{format(new Date(g), "MMM dd")}</h3>
          <div>
            {groups[g].map((e) => (
              <Card key={e.id} className={styles.EventCard}>
                {e.imageUrl && <Card.Img variant="top" src={e.imageUrl} />}
                <Card.Body>
                  <Card.Title>{e.title}</Card.Title>
                  <span className="text-muted">@</span>
                  <time>{format(new Date(e.startUtc), "hh:mm a")}</time>

                  <div className="text-muted">{e.venueName}</div>
                  {e.description && <div>{e.description}</div>} 
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

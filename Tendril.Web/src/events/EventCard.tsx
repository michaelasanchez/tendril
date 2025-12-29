import cn from "classnames";
import { format } from "date-fns";
import { Card } from "react-bootstrap";
import { Icon } from "../components/Icon";
import type { Event } from "../types/api";
import styles from "./EventList.module.css";
import NoImage from "./no-image.svg";

interface Props {
  event: Event;
  className?: string | CSSModuleClasses;
  onClick?: () => void;
}

export const EventCard: React.FC<Props> = ({ event, className, onClick }) => {
  return (
    <Card
      key={event.id}
      className={cn(styles.EventCard, className)}
      onClick={onClick}
    >
      <div className={cn(!event.imageUrl && styles.NoImage)}>
        <Card.Img
          className={styles.CardImage}
          variant="top"
          src={event.imageUrl ?? NoImage}
          loading="lazy"
        />
      </div>
      <Card.Body className={styles.CardBody}>
        <div className={styles.CardContent}>
          <Card.Title>{event.title}</Card.Title>
          <div className={styles.CardRow}>
            <Icon name="location" /> {event.location ?? event.venueName}
          </div>
          <div className={styles.CardRow}>
            <Icon name="calendar" /> {formatDate(event.startUtc, "date")} •{" "}
            {formatDate(event.startUtc, "time")}
          </div>
          <div className={styles.CardRow}>
            <Icon name="ticket" /> ???
          </div>
          {/*           
          <span className="text-muted">@</span>
          <time>{formatDate(event.startUtc)}</time>
          {event.endUtc && (
            <>
              {" "}
              <span>-</span>
              <time> {formatDate(event.endUtc)}</time>
            </>
          )}
          {event.venueName && (
            <div>
              {event.venueUrl ? (
                <a href={event.venueUrl}>{event.venueName}</a>
              ) : (
                <>{event.venueName}</>
              )}
            </div>
          )} */}
          {/* {event.location && <div className="text-muted">{event.location}</div>} */}
          {/* {event.description && <div>{event.description}</div>} */}
        </div>
        {/* {event.ticketUrl && (
          <div className={styles.CardActions}>
            <Button
              href={event.ticketUrl}
              target="blank"
              variant="outline-info"
            >
              Tickets
            </Button>
          </div>
        )} */}
      </Card.Body>
    </Card>
  );

  function formatDate(dateStr: string, dateFormat: "date" | "time" = "time") {
    const date = new Date(dateStr);

    return dateFormat === "date"
      ? format(date, "MMM d, yyyy")
      : format(date, "h:mm a");
  }
};

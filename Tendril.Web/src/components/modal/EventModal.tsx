import { format } from "date-fns";
import { Modal } from "react-bootstrap";
import type { Event } from "../../types/api";
import { Icon, type IconName } from "../Icon";
import styles from "./Modal.module.css";

interface Props {
  event: Event | null;
  show: boolean;
  onHide: () => void;
}

const EventRow: React.FC<{
  icon: IconName;
  label: string;
  children?: React.ReactNode;
}> = ({ icon, label, children }) => (
  <div className={styles.ModalRow}>
    <div>
      <div className={styles.IconBox}>
        <Icon name={icon} size={18}/>
      </div>
    </div>
    <div>
      <h6>{label}</h6>
      {children}
    </div>
  </div>
);

export const EventModal: React.FC<Props> = ({ event, show, onHide }) => {
  console.log(event?.startUtc);
  return (
    <Modal show={show} onHide={onHide}>
      <Modal.Header closeButton></Modal.Header>
      <Modal.Body>
        <h4>{event?.title}</h4>
        <p>{event?.description}</p>
        <EventRow icon="calendar" label="Date & Time">
          <div>{event && format(event?.startUtc ?? "", "MMM d, yyyy")}</div>
          <div>{event && format(event?.startUtc ?? "", "h:mm a")}</div>
        </EventRow>
        <EventRow icon="location" label="Venue">
          {event?.location ?? event?.venueName}

        </EventRow>
        <EventRow icon="ticket" label="Tickets">
          $? - $??
        </EventRow>
      </Modal.Body>
      <Modal.Footer>
        {event?.ticketUrl && (
          <a href={event.ticketUrl} target="_blank" rel="noreferrer" className="btn btn-outline-primary">
            Get Tickets <Icon name="external" />
          </a>
        )}
        {/* <Button variant="secondary" onClick={onHide}>
          Close
        </Button>
        <Button variant="primary" onClick={onHide}>
          Save Changes
        </Button> */}
      </Modal.Footer>
    </Modal>
  );
};

import { format } from 'date-fns';
import { Modal } from 'react-bootstrap';
import NoImage from '../../assets/no-image.svg';
import type { Event } from '../../types/api';
import { Badge } from '../badge';
import { IconButton, SquareButton } from '../button';
import { Icon, type IconName } from '../Icon';
import styles from './Modal.module.css';

interface Props {
  event: Event | null;
  show: boolean;
  onHide?: () => void;
}

const EventRow: React.FC<{
  icon: IconName;
  label: string;
  children?: React.ReactNode;
}> = ({ icon, label, children }) => (
  <div className={styles.ModalRow}>
    <div>
      <div className={styles.IconBox}>
        <Icon name={icon} size={18} />
      </div>
    </div>
    <div>
      <h4>{label}</h4>
      {children}
    </div>
  </div>
);

export const EventModal: React.FC<Props> = ({ event, show, onHide }) => {
  return (
    <Modal
      className={styles.Modal}
      show={show}
      centered
      animation={true}
      onHide={onHide}
    >
      {event && (
        <>
          {/* --- Header --- */}
          <Modal.Header className={styles.Header}>
            <img src={event.imageUrl ?? NoImage} alt={event.title} />
            <div className={styles.TopRight}>
              <IconButton name="close" onClick={onHide} />
            </div>
            <div className={styles.BottomLeft}>
              {event.categoryName && (
                <Badge className={styles.Uppercase}>{event.categoryName}</Badge>
              )}
            </div>
          </Modal.Header>

          {/* --- Body --- */}
          <Modal.Body className={styles.Body}>
            <div className={styles.Content}>
              <h2>{event.title}</h2>
              <p>{event.description}</p>
            </div>
            <EventRow icon="calendar" label="Date & Time">
              <p>
                {format(new Date(event.startUtc ?? ''), 'iiii, MMMM d, yyyy')}
              </p>
              <p>{format(new Date(event.startUtc ?? ''), 'h:mm a')}</p>
            </EventRow>
            <EventRow icon="location" label="Venue">
              <p>{event.location ?? event.venueName}</p>
            </EventRow>
            {!!event.minPrice && (
              <EventRow icon="ticket" label="Tickets">
                <p>
                  ${event.minPrice}
                  {!!event.maxPrice && event.maxPrice !== event.minPrice
                    ? ` - $${event.maxPrice}`
                    : ''}
                </p>
              </EventRow>
            )}
          </Modal.Body>

          {/* --- Footer --- */}
          {(!!event.ticketUrl || !!event.detailsUrl) && (
            <Modal.Footer className={styles.Footer}>
              <div>
                {event.detailsUrl && (
                  <SquareButton
                    variant="outline-primary"
                    href={event.detailsUrl}
                    target="_blank"
                    rel="noreferrer"
                  >
                    Event Details <Icon name="external" />
                  </SquareButton>
                )}
                {event.ticketUrl && (
                  <SquareButton
                    variant="primary"
                    href={event.ticketUrl}
                    target="_blank"
                    rel="noreferrer"
                  >
                    Get Tickets <Icon name="external" />
                  </SquareButton>
                )}
              </div>

              <SquareButton
                variant="outline-secondary"
                onClick={() =>
                  navigator.share({
                    title: event.title,
                    url: window.location.href,
                  })
                }
              >
                <Icon name="share" />
              </SquareButton>
            </Modal.Footer>
          )}
        </>
      )}
    </Modal>
  );
};

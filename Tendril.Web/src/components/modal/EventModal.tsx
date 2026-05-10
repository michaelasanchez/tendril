import cn from 'classnames';
import buttonStyles from '../button/Button.module.css';
import { format, parseISO } from 'date-fns';
import type { EventAttributes } from 'ics';
import * as ics from 'ics';
import { Dropdown, Modal } from 'react-bootstrap';
import NoImage from '../../assets/no-image.svg';
import type { Event, Venue } from '../../types/api';
import { Badge } from '../badge';
import { IconButton, SquareButton } from '../button';
import ExpandableText from '../ExpandableText';
import { Icon, type IconName } from '../Icon';
import styles from './Modal.module.css';

interface Props {
  event: Event | null;
  venues: Venue[] | null;
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

export const EventModal: React.FC<Props> = ({
  event,
  venues,
  show,
  onHide,
}) => {
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
              <ExpandableText>
                <p>{event.description}</p>
              </ExpandableText>
            </div>
            <EventRow icon="calendar" label="Date & Time">
              <p>
                {format(new Date(event.startUtc ?? ''), 'iiii, MMMM d, yyyy')}
              </p>
              <p>{format(new Date(event.startUtc ?? ''), 'h:mm a')}</p>
            </EventRow>
            <EventRow icon="location" label="Venue">
              <p>
                {event.venueUrl ? (
                  <>
                    <a
                      className={styles.VenueLink}
                      href={event.venueUrl}
                      target="_blank"
                    >
                      {event.venueName}
                    </a>{' '}
                    <Icon name="external" />
                  </>
                ) : (
                  event.venueName
                )}
              </p>
              <small className="text-muted">{event.location}</small>
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
              <div className={styles.ButtonGroup}>
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

              <div className={styles.ButtonGroup}>
                <Dropdown drop="up">
                  <Dropdown.Toggle
                    variant="none"
                    className={cn(buttonStyles.SquareButton, buttonStyles.OutlineSecondary)}
                  >
                    <Icon name="ics" />
                  </Dropdown.Toggle>

                  <Dropdown.Menu>
                    <Dropdown.Item
                      onClick={() =>
                        handleGoogleCalendar(
                          event,
                          venues?.find((v) => v.name === event.venueName) ??
                            null,
                        )
                      }
                    >
                      Gmail
                    </Dropdown.Item>
                    <Dropdown.Item
                      onClick={() =>
                        handleDownloadIcs(
                          event,
                          venues?.find((v) => v.name === event.venueName) ??
                            null,
                        )
                      }
                    >
                      ICS
                    </Dropdown.Item>
                  </Dropdown.Menu>
                </Dropdown>

                {/* <SquareButton
                  variant="outline-secondary"
                  onClick={() => handleDownloadIcs(event)}
                >
                  <Icon name="ics" />
                </SquareButton> */}
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
              </div>
            </Modal.Footer>
          )}
        </>
      )}
    </Modal>
  );
};

const formatForGoogleLocal = (isoString: string) => {
  const date = parseISO(isoString);
  // This produces "20260509T193000" (no Z, no offsets)
  return format(date, "yyyyMMdd'T'HHmmss");
};

function handleGoogleCalendar(event: Event, venue: Venue | null = null) {
  const root = 'https://calendar.google.com/calendar/render?action=TEMPLATE';
  const params = new URLSearchParams({
    text: `${event.title} at ${event.venueName}`,
    dates: `${formatForGoogleLocal(event.startUtc)}${event.endUtc ? `/${formatForGoogleLocal(event.endUtc)}` : ''}`,
    details: event.description,
    location: venue?.address ?? event.location,
  });
  const url = `${root}&${params.toString()}`;
  console.log(url);
  window.open(url, '_blank', 'noreferrer');
}

function handleDownloadIcs(event: Event, venue: Venue | null = null) {
  const icsEvent: EventAttributes = {
    start: [2018, 5, 30, 6, 30],
    duration: { hours: 6, minutes: 30 },
    title: `${event.title} at ${event.venueName}`,
    description: event.description,
    location: venue?.address ?? event.location,
    url: `https://www.hello-local.app/event/${event.id}`,
    categories: [event.categoryName ?? 'Event'],
    status: 'CONFIRMED',
    busyStatus: 'BUSY',
  };

  ics.createEvent(icsEvent, (error, value) => {
    if (error) {
      console.log(error);
      return;
    }
    // 1. Create a "Blob" from the ICS string
    const blob = new Blob([value], {
      type: 'text/calendar;charset=utf-8',
    });

    // 2. Create a temporary URL for that blob
    const url = window.URL.createObjectURL(blob);

    // 3. Create a hidden <a> tag to trigger the download
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', 'event.ics'); // This names the file

    // 4. Append to body, click it, and clean up
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  });
}

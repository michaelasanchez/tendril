import cn from 'classnames';
import { format } from 'date-fns';
import { useEffect, useRef } from 'react';
import { Modal } from 'react-bootstrap';
import type { Event } from '../../types/api';
import { Badge } from '../badge';
import { IconButton, SquareButton } from '../button';
import { Icon, type IconName } from '../Icon';
import styles from './Modal.module.css';

interface Props {
  event: Event | null;
  show: boolean;
  onHide: () => void;
  onNext: () => void;
  onPrev: () => void;
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
  show,
  onHide,
  onNext,
  onPrev,
}) => {
  // --- Swipe Logic ---
  const touchStart = useRef<number | null>(null);
  const minSwipeDistance = 50;

  useEffect(() => {
    if (!show) return;

    const handleTouchStart = (e: TouchEvent) => {
      touchStart.current = e.targetTouches[0].clientX;
    };

    const handleTouchEnd = (e: TouchEvent) => {
      if (touchStart.current === null) return;

      const touchEnd = e.changedTouches[0].clientX;
      const distance = touchStart.current - touchEnd;

      if (Math.abs(distance) > minSwipeDistance) {
        if (distance > 0)
          onNext(); // Swiped Left
        else onPrev(); // Swiped Right
      }
    };

    // Attach to window to catch swipes anywhere (backdrop or modal content)
    window.addEventListener('touchstart', handleTouchStart);
    window.addEventListener('touchend', handleTouchEnd);

    // Bonus: Keyboard Navigation
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'ArrowLeft') onPrev();
      if (e.key === 'ArrowRight') onNext();
    };
    window.addEventListener('keydown', handleKeyDown);

    return () => {
      window.removeEventListener('touchstart', handleTouchStart);
      window.removeEventListener('touchend', handleTouchEnd);
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [show, onNext, onPrev]);

  return (
    <>
      {show && (
        <>
          <IconButton
            name="previous"
            className={cn(
              styles.ModalNavButton,
              styles.ModalPrev,
              'd-none',
              'd-md-block',
            )}
            onClick={onPrev}
          />
          <IconButton
            name="next"
            className={cn(
              styles.ModalNavButton,
              styles.ModalNext,
              'd-none',
              'd-md-block',
            )}
            onClick={onNext}
          />
        </>
      )}
      <Modal className={styles.Modal} show={show} onHide={onHide}>
        {!!event?.imageUrl && (
          <Modal.Header className={styles.Header}>
            <img src={event.imageUrl} />
            <div className={styles.TopRight}>
              <IconButton name="close" onClick={onHide} />
            </div>
            <div className={styles.BottomLeft}>
              {event.category && <Badge className={styles.Uppercase} />}
            </div>
          </Modal.Header>
        )}
        <Modal.Body className={styles.Body}>
          <div className={styles.Content}>
            <h2>{event?.title}</h2>
            <p>{event?.description}</p>
          </div>
          <EventRow icon="calendar" label="Date & Time">
            <p>{event && format(event.startUtc ?? '', 'iiii, MMMM d, yyyy')}</p>
            <p>{event && format(event.startUtc ?? '', 'h:mm a')}</p>
          </EventRow>
          <EventRow icon="location" label="Venue">
            <p>{event?.location ?? event?.venueName}</p>
          </EventRow>
          {!!event?.minPrice && (
            <EventRow icon="ticket" label="Tickets">
              <p>
                ${event?.minPrice}
                {!!event?.maxPrice && event?.maxPrice !== event?.minPrice
                  ? ` - $${event?.maxPrice}`
                  : ''}
              </p>
            </EventRow>
          )}
        </Modal.Body>
        <Modal.Footer className={styles.Footer}>
          {event?.ticketUrl && (
            <SquareButton
              variant="primary"
              href={event.ticketUrl}
              target="_blank"
              rel="noreferrer"
            >
              Get Tickets <Icon name="external" />
            </SquareButton>
          )}
        </Modal.Footer>
      </Modal>
    </>
  );
};

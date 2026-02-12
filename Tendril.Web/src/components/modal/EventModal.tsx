import cn from 'classnames';
import { format } from 'date-fns';
import { AnimatePresence, motion } from 'framer-motion';
import { useEffect, useRef, useState } from 'react';
import { Modal } from 'react-bootstrap';
import type { Event } from '../../types/api';
import { Badge } from '../badge';
import { IconButton, SquareButton } from '../button';
import { Icon, type IconName } from '../Icon';
import styles from './Modal.module.css';

// --- Animation Variants ---
// We use absolute positioning on 'exit' so the old modal
// floats on top/bottom while the new one slides in underneath/over.
const variants = {
  enter: (direction: number) => ({
    x: direction > 0 ? 500 : -500,
    opacity: 0,
    scale: 0.95,
  }),
  center: {
    zIndex: 1,
    x: 0,
    opacity: 1,
    scale: 1,
    position: 'relative' as const, // Reclaim space in the DOM
  },
  exit: (direction: number) => ({
    zIndex: 0,
    x: direction < 0 ? 500 : -500,
    opacity: 0,
    scale: 0.95,
    position: 'absolute' as const, // Float freely so the new one can take the space
    top: 0,
    left: 0,
    width: '100%',
  }),
};

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

const minSwipeDistance = 50;

type Direction = -1 | 0 | 1; // -1: Prev, 0: No movement, 1: Next

export const EventModal: React.FC<Props> = ({
  event,
  show,
  onHide,
  onNext,
  onPrev,
}) => {
  const [direction, setDirection] = useState<Direction>(0);

  const handleNext = () => {
    setDirection(1);
    onNext();
  };

  const handlePrev = () => {
    setDirection(-1);
    onPrev();
  };

  // --- Swipe & Keyboard Logic ---
  const touchStart = useRef<number | null>(null);

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
          handleNext(); // Swipe Left -> Next
        else handlePrev(); // Swipe Right -> Prev
      }
    };

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'ArrowLeft') handlePrev();
      if (e.key === 'ArrowRight') handleNext();
    };

    // Attach listeners globally
    window.addEventListener('touchstart', handleTouchStart);
    window.addEventListener('touchend', handleTouchEnd);
    window.addEventListener('keydown', handleKeyDown);

    return () => {
      window.removeEventListener('touchstart', handleTouchStart);
      window.removeEventListener('touchend', handleTouchEnd);
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [show, onNext, onPrev]);

  return (
    <>
      {/* Desktop Navigation Buttons */}
      {show && (
        <>
          <div
            className={cn(
              styles.ModalNavButton,
              styles.ModalPrev,
              'd-none',
              'd-md-block',
            )}
          >
            <IconButton name="previous" onClick={handlePrev} />
          </div>
          <div
            className={cn(
              styles.ModalNavButton,
              styles.ModalNext,
              'd-none',
              'd-md-block',
            )}
          >
            <IconButton name="next" onClick={handleNext} />
          </div>
        </>
      )}

      <Modal
        className={styles.Modal}
        show={show}
        onHide={onHide}
        centered
        contentClassName="bg-transparent border-0 shadow-none"
      >
        {/* Overflow hidden wrapper handles the sliding elements going out of bounds */}
        <div style={{ overflow: 'hidden', padding: '10px', margin: '-10px' }}>
          <AnimatePresence initial={false} custom={direction} mode="popLayout">
            {event && (
              <motion.div
                key={event.id ?? event.title} // Changing key triggers animation
                custom={direction}
                variants={variants}
                initial={direction !== 0 ? 'enter' : 'center'}
                animate="center"
                exit="exit"
                transition={{
                  x: { type: 'spring', stiffness: 300, damping: 30 },
                  opacity: { duration: 0.2 },
                }}
                className="modal-content"
              >
                {/* --- Header --- */}
                {!!event.imageUrl && (
                  <Modal.Header className={styles.Header}>
                    <img src={event.imageUrl} alt={event.title} />
                    <div className={styles.TopRight}>
                      <IconButton name="close" onClick={onHide} />
                    </div>
                    <div className={styles.BottomLeft}>
                      {event.category && (
                        <Badge className={styles.Uppercase}>
                          {event.category}
                        </Badge>
                      )}
                    </div>
                  </Modal.Header>
                )}

                {/* --- Body --- */}
                <Modal.Body className={styles.Body}>
                  <div className={styles.Content}>
                    <h2>{event.title}</h2>
                    <p>{event.description}</p>
                  </div>
                  <EventRow icon="calendar" label="Date & Time">
                    <p>
                      {format(
                        new Date(event.startUtc ?? ''),
                        'iiii, MMMM d, yyyy',
                      )}
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
                {event.ticketUrl && (
                  <Modal.Footer className={styles.Footer}>
                    <SquareButton
                      variant="primary"
                      href={event.ticketUrl}
                      target="_blank"
                      rel="noreferrer"
                    >
                      Get Tickets <Icon name="external" />
                    </SquareButton>
                  </Modal.Footer>
                )}
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      </Modal>
    </>
  );
};

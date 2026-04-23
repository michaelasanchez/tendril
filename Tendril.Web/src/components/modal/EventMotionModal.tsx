import cn from 'classnames';
import { format } from 'date-fns';
import { AnimatePresence, motion, type Variants } from 'framer-motion';
import { useEffect, useRef, useState } from 'react';
import { Modal } from 'react-bootstrap';
import NoImage from '../../assets/no-image.svg';
import type { Event } from '../../types/api';
import { Badge } from '../badge';
import { IconButton, SquareButton } from '../button';
import { Icon, type IconName } from '../Icon';
import styles from './Modal.module.css';

// --- Animation Variants ---
const shellVariants: Variants = {
  initial: {
    opacity: 0,
    y: -50,
    scale: 0.95,
  },
  animate: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: {
      type: 'spring',
      damping: 25,
      stiffness: 300,
    },
  },
  exit: {
    opacity: 0,
    y: 30, // Slight slide down on close feels more natural than sliding back up
    scale: 0.95,
    transition: { duration: 0.2 },
  },
};

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

export const EventMotionModal: React.FC<Props> = ({
  event,
  show,
  onHide,
  onNext,
  onPrev,
}) => {
  const [internalShow, setInternalShow] = useState(show);
  const [direction, setDirection] = useState<Direction>(0);

  const handleNext = () => {
    setDirection(1);
    onNext();
  };

  const handlePrev = () => {
    setDirection(-1);
    onPrev();
  };

  const handleStartClose = () => {
    setInternalShow(false); // Triggers <motion.div exit="exit">
  };

  const handleExitComplete = () => {
    // Only call parent's onHide once the animation is 100% finished
    if (!internalShow) {
      onHide();
    }
  };

  // --- Swipe & Keyboard Logic ---
  const touchStart = useRef<number | null>(null);

  // Sync internal state with prop when opening
  useEffect(() => {
    if (show) setInternalShow(true);
  }, [show]);

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
        onHide={handleStartClose}
        centered
        contentClassName={styles.NoWrapper}
        animation={true}
      >
        <AnimatePresence onExitComplete={handleExitComplete}>
          {internalShow && (
            <motion.div
              variants={shellVariants}
              initial="initial"
              animate="animate"
              exit="exit"
            >
              <div className={styles.Wrapper}>
                <AnimatePresence
                  key="modal-shell"
                  initial={false}
                  custom={direction}
                  mode="popLayout"
                >
                  {event && (
                    <motion.div
                      key={event.id ?? event.title}
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
                      <Modal.Header className={styles.Header}>
                        <img
                          src={event.imageUrl ?? NoImage}
                          alt={event.title}
                        />
                        <div className={styles.TopRight}>
                          <IconButton name="close" onClick={handleStartClose} />
                        </div>
                        <div className={styles.BottomLeft}>
                          {event.categoryName && (
                            <Badge className={styles.Uppercase}>
                              {event.categoryName}
                            </Badge>
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
                            {format(
                              new Date(event.startUtc ?? ''),
                              'iiii, MMMM d, yyyy',
                            )}
                          </p>
                          <p>
                            {format(new Date(event.startUtc ?? ''), 'h:mm a')}
                          </p>
                        </EventRow>
                        <EventRow icon="location" label="Venue">
                          <p>{event.location ?? event.venueName}</p>
                        </EventRow>
                        {!!event.minPrice && (
                          <EventRow icon="ticket" label="Tickets">
                            <p>
                              ${event.minPrice}
                              {!!event.maxPrice &&
                              event.maxPrice !== event.minPrice
                                ? ` - $${event.maxPrice}`
                                : ''}
                            </p>
                          </EventRow>
                        )}
                      </Modal.Body>

                      {/* --- Footer --- */}
                      {(!!event.ticketUrl || !!event.detailsUrl) && (
                        <Modal.Footer className={styles.Footer}>
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
                        </Modal.Footer>
                      )}
                    </motion.div>
                  )}
                </AnimatePresence>
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </Modal>
    </>
  );
};

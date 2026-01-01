import cn from 'classnames';
import { format } from 'date-fns';
import { Card } from 'react-bootstrap';
import { Icon } from '../components/Icon';
import { buttonStyles, cardStyles } from '../styles';
import type { Event } from '../types/api';
import styles from './EventCard.module.css';
import NoImage from './no-image.svg';

interface Props {
  event: Event;
  className?: string | CSSModuleClasses;
  favorite?: boolean;
  onClick?: () => void;
  onFavorite?: () => void;
}

const fakeCategories = [
  'EXCITING',
  'AMAZING',
  'AWESOME',
  'GREAT',
  'SPECTACULAR',
  'FANTASTIC',
  'INCREDIBLE',
  'MIND-BLOWING',
  'UNBELIEVABLE',
  'WONDERFUL',
  'STUNNING',
  'MAGNIFICENT',
  'BREATH-TAKING',
  'ASTONISHING',
  'PHENOMENAL',
  'MARVELOUS',
  'SENSATIONAL',
  'TREMENDOUS',
  'EXHILARATING',
  'THRILLING',
  'RIVETING',
  'CAPTIVATING',
  'ENTHRALLING',
  'GRIPPING',
  'ENGAGING',
  'FASCINATING',
  'CHARMING',
  'DELIGHTFUL',
  'ENCHANTING',
  'GLORIOUS',
  'RADIANT',
  'DAZZLING',
  'BRILLIANT',
  'LUMINOUS',
  'VIBRANT',
  'ELECTRIFYING',
  'DYNAMIC',
  'ENERGETIC',
  'VIVACIOUS',
  'SPIRITED',
  'ZESTY',
  'EXUBERANT',
];

export const EventCard: React.FC<Props> = ({
  className,
  event,
  favorite,
  onClick,
  onFavorite,
}) => {
  return (
    <Card
      key={event.id}
      className={cn(cardStyles.BgCard, styles.EventCard, className)}
      onClick={onClick}
    >
      <div className={cn(styles.CardHeader, !event.imageUrl && styles.NoImage)}>
        <Card.Img
          className={cn(cardStyles.CardImage, styles.CardImage)}
          variant="top"
          src={event.imageUrl ?? NoImage}
          loading="lazy"
        />

        <div className={styles.Overlay}>
          <div className={styles.TopLeft}>
            <button
              className={cn(
                buttonStyles.IconButton,
                favorite && styles.Favorite
              )}
              onClick={(e) => {
                if (!!onFavorite) {
                  e.stopPropagation();
                  onFavorite();
                }
              }}
            >
              <Icon name="favorite" />
            </button>
          </div>
          <div className={styles.TopRight}>
            <div className={styles.CategoryBadge}>
              {event.category ??
                fakeCategories[
                  (event.title.length + new Date(event.startUtc).getDate()) %
                    fakeCategories.length
                ]}
            </div>
          </div>
        </div>
      </div>
      <Card.Body className={cardStyles.CardBody}>
        <div className={styles.CardContent}>
          {/* Title */}
          <Card.Title as="h3" className={styles.CardTitle}>
            {event.title}
          </Card.Title>

          {/* Location */}
          <div className={styles.CardRow}>
            <Icon name="location" /> {event.location ?? event.venueName}
          </div>

          {/* Date & Time */}
          <div className={styles.CardRow}>
            <Icon name="calendar" /> {formatDate(event.startUtc, 'date')} •{' '}
            {formatDate(event.startUtc, 'time')}
          </div>

          {/* Cost */}
          {!!event.minPrice && (
            <div className={styles.CardRow}>
              <Icon name="ticket" /> ${event.minPrice}
              {event.maxPrice !== event.minPrice ? ` - $${event.maxPrice}` : ''}
            </div>
          )}
        </div>
      </Card.Body>
    </Card>
  );

  function formatDate(dateStr: string, dateFormat: 'date' | 'time' = 'time') {
    const date = new Date(dateStr);

    return dateFormat === 'date'
      ? format(date, 'MMM d, yyyy')
      : format(date, 'h:mm a');
  }
};

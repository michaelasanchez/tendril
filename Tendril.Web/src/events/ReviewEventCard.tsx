import cn from 'classnames';
import { format } from 'date-fns';
import { Card } from 'react-bootstrap';
import NoImage from '../assets/no-image.svg';
import { SquareButton as Button } from '../components/button';
import { Icon } from '../components/Icon';
import type { Event } from '../types/api';

// TODO: GENERALZIE !!
import styles from '../scrapers/Tab.module.css';

interface Props {
  e: Event;
}

export const ReviewEventCard: React.FC<Props> = ({ e }) => {
  return (
    <Card
      style={{
        display: 'flex',
        flexDirection: 'row',
        flexGrow: 1,
        opacity: e.status === 'Suppressed' ? 0.4 : 1,
        border: e.requiresReview
          ? '2px solid #7a3333'
          : e.status === 'Pending'
            ? '2px dashed orange'
            : undefined,
        boxShadow: e.requiresReview ? '0 0 10px #c96f6f88' : undefined,
      }}
    >
      {
        <Card.Img
          src={e.imageUrl ? e.imageUrl : NoImage}
          style={{
            maxWidth: '120px',
            borderTopRightRadius: 0,
            borderBottomRightRadius: 0,
          }}
        />
      }
      <Card.Body
        style={{
          display: 'flex',
          gap: '1em',
          justifyContent: 'space-between',
        }}
      >
        <div>
          <h3>{e.title}</h3>
          <p className={cn(styles.Clamp, !e.description && styles.Muted)}>
            {e.description ?? 'No description available'}
          </p>
          <label>
            <h4>
              {format(
                new Date((e.showStartTime ? e.startDateTime : e.startDate)!),
                'MMM dd yyy',
              )}
            </h4>
            {e.showStartTime && format(new Date(e.startDateTime!), 'hh:mm aa')}
          </label>
          <div>
            {e.detailsUrl && (
              <Button href={e.detailsUrl} target="_blank">
                Details
              </Button>
            )}
            {e.ticketUrl && (
              <Button href={e.ticketUrl} target="_blank">
                Tickets
              </Button>
            )}
          </div>
        </div>
        <div>
          <Button>
            <Icon name="copy" />
          </Button>
        </div>
      </Card.Body>
    </Card>
  );
};

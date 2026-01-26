import { useEffect, useMemo, useState } from 'react';
import { Card } from 'react-bootstrap';
import { EventsApi } from '../api/events';
import NoImage from '../assets/no-image.svg';
import { SquareButton as Button } from '../components/button';
import { FormCheck } from '../components/form';
import { Icon } from '../components/Icon';
import { cardStyles, pageStyles } from '../styles';
import type { Event, Guid } from '../types/api';
import styles from './ScraperOutputTab.module.css';

interface Props {
  scraperId: Guid;
}

interface Stats {
  pending: number;
  published: number;
  suppressed: number;
}

const defaultStats = () => ({
  pending: 0,
  published: 0,
  suppressed: 0,
});

export const ScraperOutputTab: React.FC<Props> = ({ scraperId }) => {
  const [events, setEvents] = useState<Event[]>([]);
  const [showSuppressed, setShowDisabled] = useState<boolean>(true);
  const [stats, setStats] = useState<Stats>(defaultStats());

  const load = async () => {
    if (scraperId !== 'new') {
      const events = await EventsApi.getByScraperId(scraperId);

      const stats = events.reduce((a, c) => {
        if (c.status === 'Pending') {
          a.pending++;
        }
        if (c.status === 'Published') {
          a.published++;
        }
        if (c.status === 'Suppressed') {
          a.suppressed++;
        }
        return a;
      }, defaultStats());

      setEvents(events);
      // setStats(events.((a, c) => {}, ))
    }
  };

  useEffect(() => {
    void load();
  }, [scraperId]);

  const filteredEvents = useMemo(() => {
    let filtered: Event[] = [...events];

    if (!showSuppressed) {
      filtered = filtered.filter((e) => e.status !== 'Suppressed');
    }

    return filtered;
  }, [events, showSuppressed]);

  return (
    <>
      <div className={pageStyles.pageHeader}>
        <h3>Output</h3>
      </div>
      <Card className={cardStyles.BgCard} style={{ marginBottom: '1em' }}>
        <Card.Body>
          <FormCheck
            label="Show Suppressed"
            checked={showSuppressed}
            onChange={() => setShowDisabled(!showSuppressed)}
          />
        </Card.Body>
      </Card>
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(1, 1fr)',
          gap: '1rem',
          placeItems: 'stretch',
        }}
      >
        {filteredEvents.map((e) => (
          <div
            key={e.id}
            style={{ display: 'flex', gap: '1em', alignItems: 'stretch' }}
          >
            <Card
              style={{
                display: 'flex',
                flexDirection: 'row',
                flexGrow: 1,
                opacity: e.status === 'Suppressed' ? 0.4 : 1,
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
              <Card.Body>
                <h3>{e.title}</h3>
                <label>{e.startUtc}</label>
              </Card.Body>
            </Card>
            <div className={styles.ActionContainer}>
              <Button
                disabled={e.status === 'Suppressed'}
                onClick={() =>
                  EventsApi.patch(e.id, {
                    status: e.status === 'Pending' ? 'Published' : 'Pending',
                  }).then(() => load())
                }
              >
                <Icon name={e.status === 'Pending' ? 'publish' : 'unpublish'} />
              </Button>
              <Button
                disabled={e.status === 'Pending'}
                onClick={() =>
                  EventsApi.patch(e.id, {
                    status:
                      e.status === 'Published' ? 'Suppressed' : 'Published',
                  }).then(() => load())
                }
              >
                <Icon
                  name={e.status === 'Published' ? 'invisible' : 'visible'}
                />
              </Button>
            </div>
          </div>
          // <EventCard key={e.id} event={e} />
        ))}
      </div>
    </>
  );
};

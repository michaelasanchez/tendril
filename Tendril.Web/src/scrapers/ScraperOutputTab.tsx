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
  events: Event[];
  loadEvents: () => Promise<void>;
}

interface Stats {
  pending: number;
  published: number;
  suppressed: number;
}

interface Show {
  pending: boolean;
  published: boolean;
  suppressed: boolean;
}

const defaultStats = () => ({
  pending: 0,
  published: 0,
  suppressed: 0,
});

export const ScraperOutputTab: React.FC<Props> = ({ scraperId, events, loadEvents }) => {
  const [show, setShow] = useState<Show>({
    pending: true,
    published: true,
    suppressed: false,
  });
  const [stats, setStats] = useState<Stats>(defaultStats());

  useEffect(() => {
    if (scraperId !== 'new') {
      

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

      setStats(stats);
    }
  }, [events]);

  useEffect(() => {
    void loadEvents();
  }, [scraperId]);

  const filteredEvents = useMemo(() => {
    let filtered: Event[] = [...events];

    if (!show.pending) {
      filtered = filtered.filter((e) => e.status !== 'Pending');
    }

    if (!show.published) {
      filtered = filtered.filter((e) => e.status !== 'Published');
    }

    if (!show.suppressed) {
      filtered = filtered.filter((e) => e.status !== 'Suppressed');
    }

    return filtered;
  }, [events, show]);

  return (
    <>
      <div className={pageStyles.pageHeader}>
        <h3>Output</h3>
      </div>
      <Card className={cardStyles.BgCard} style={{ marginBottom: '1em' }}>
        <Card.Body
          style={{
            display: 'flex',
            gap: '1em',
            justifyContent: 'space-between',
          }}
        >
          <div>
            <div>Pending: {stats.pending}</div>
            <div>Published: {stats.published}</div>
            <div>Suppressed: {stats.suppressed}</div>
          </div>
          <div>
            <FormCheck
              label="Show Pending"
              checked={show.pending}
              onChange={() => setShow({ ...show, pending: !show.pending })}
            />
            <FormCheck
              label="Show Published"
              checked={show.published}
              onChange={() => setShow({ ...show, published: !show.published })}
            />
            <FormCheck
              label="Show Suppressed"
              checked={show.suppressed}
              onChange={() =>
                setShow({ ...show, suppressed: !show.suppressed })
              }
            />
          </div>
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
                border:
                  e.status === 'Pending' ? '2px dashed orange' : undefined,
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
                  }).then(() => loadEvents())
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
                  }).then(() => loadEvents())
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

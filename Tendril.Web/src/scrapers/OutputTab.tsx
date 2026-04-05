import cn from 'classnames';
import { format, startOfDay } from 'date-fns';
import { useEffect, useMemo, useState } from 'react';
import { Card } from 'react-bootstrap';
import { EventsApi } from '../api/events';
import NoImage from '../assets/no-image.svg';
import { SquareButton as Button, SquareButton } from '../components/button';
import { FormCheck, FormSelect } from '../components/form';
import { Icon } from '../components/Icon';
import { cardStyles, pageStyles } from '../styles';
import type { Category, Event, Guid } from '../types/api';
import styles from './Tab.module.css';

interface Props {
  scraperId: Guid;
  categories: Category[];
  events: Event[];
  loadEvents: () => Promise<void>;
}

interface Show {
  past: boolean;
  pending: boolean;
  published: boolean;
  suppressed: boolean;
}

interface CategoryStats {
  [category: string]: number;
}

type StatusStats = { [key in keyof Show]: number };

interface Stats {
  total: StatusStats;
  category: CategoryStats;
}

const defaultStats = (): Stats => ({
  total: {
    past: 0,
    pending: 0,
    published: 0,
    suppressed: 0,
  },
  category: {},
});

const today = startOfDay(new Date());

export const OutputTab: React.FC<Props> = ({
  scraperId,
  categories,
  events,
  loadEvents,
}) => {
  const [show, setShow] = useState<Show>({
    past: false,
    pending: true,
    published: true,
    suppressed: false,
  });
  const [stats, setStats] = useState<Stats>(defaultStats());

  useEffect(() => {
    if (scraperId !== 'new') {
      const stats = events.reduce((a, e) => {
        switch (e.status) {
          case 'Pending':
            a.total.pending++;
            break;
          case 'Published':
            a.total.published++;
            break;
          case 'Suppressed':
            a.total.suppressed++;
            break;
        }

        if (!!e.categoryName) {
          if (!a.category[e.categoryName]) {
            a.category[e.categoryName] = 0;
          }

          a.category[e.categoryName]++;
        }
        return a;
      }, defaultStats());

      stats.total.past = events.filter(
        (e) => startOfDay(new Date(e.startUtc)) <= today,
      ).length;

      setStats(stats);
    }
  }, [events]);

  useEffect(() => {
    void loadEvents();
  }, [scraperId]);

  const filteredEvents = useMemo(() => {
    let filtered: Event[] = [...events];

    if (!show.past) {
      filtered = filtered.filter(
        (e) => startOfDay(new Date(e.startUtc)) >= today,
      );
    }

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
      <Card className={cn(cardStyles.BgCard, cardStyles.MarginBottom, cardStyles.Opaque, styles.FilterRow)}>
        <Card.Body
          style={{
            display: 'flex',
            gap: '1em',
            justifyContent: 'space-between',
          }}
        >
          <div>
            <div>
              <FormCheck
                label={`Past (${stats.total.past})`}
                checked={show.past}
                onChange={() => setShow({ ...show, past: !show.past })}
              />
            </div>
            <div>
              <FormCheck
                label={`Pending (${stats.total.pending})`}
                checked={show.pending}
                onChange={() => setShow({ ...show, pending: !show.pending })}
              />
            </div>
            <div>
              <FormCheck
                label={`Published (${stats.total.published})`}
                checked={show.published}
                onChange={() =>
                  setShow({ ...show, published: !show.published })
                }
              />
            </div>
            <div>
              <FormCheck
                label={`Archived (${stats.total.suppressed})`}
                checked={show.suppressed}
                onChange={() =>
                  setShow({ ...show, suppressed: !show.suppressed })
                }
              />
            </div>
          </div>
          <div>
            {Object.entries(stats?.category).map(([category, count]) => (
              <div key={category}>
                {category}: {count}
              </div>
            ))}
          </div>
          <div></div>
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
                  e.isReviewRequired ? '2px solid #7a3333' : e.status === 'Pending' ? '2px dashed orange' : undefined,
                  boxShadow: e.isReviewRequired ? '0 0 10px #c96f6f88' : undefined,
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
              <Card.Body style={{ display: 'flex', gap: '1em', justifyContent: 'space-between'}}>
                <div>
                <h3>{e.title}</h3>
                <p
                  className={cn(
                    styles.Clamp,
                    !e.description && styles.Muted,
                  )}
                >
                  {e.description ?? 'No description available'}
                </p>
                <label>
                  <h4>{format(new Date(e.startUtc), 'MMM dd yyy')}</h4>
                  {format(new Date(e.startUtc), 'hh:mm aa')}
                </label>
                <div>
                  {e.detailsUrl && <SquareButton href={e.detailsUrl} target="_blank">Details</SquareButton>}
                  {e.ticketUrl && <SquareButton href={e.ticketUrl} target="_blank">Tickets</SquareButton>}
                </div></div><div><SquareButton><Icon name="copy" /></SquareButton></div>
              </Card.Body>
            </Card>
            <div style={{ minWidth: '120px', width: '120px' }}>
              <FormSelect
                value={
                  categories?.find((c) => c.name == e.categoryName)?.id ?? ''
                }
                options={[
                  { value: '', label: 'None' },
                  ...(categories?.map((c) => ({
                    value: c.id,
                    label: c.name,
                  })) ?? []),
                ]}
                onChange={(value) =>
                  EventsApi.patch(e.id, { categoryId: value }).then(() =>
                    loadEvents(),
                  )
                }
              />
            </div>
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
                onClick={() =>
                  EventsApi.patch(e.id, {
                    status:
                      e.status !== 'Suppressed' ? 'Suppressed' : 'Pending',
                  }).then(() => loadEvents())
                }
              >
                <Icon
                  name={e.status !== 'Suppressed' ? 'archive' : 'unarchive'}
                />
              </Button>
              <Button onClick={() => EventsApi.patch(e.id, { isReviewRequired: !e.isReviewRequired }).then(() => loadEvents())}>
                <Icon name={e.isReviewRequired ? 'flagOff' : 'flag'} />
              </Button>
            </div>
          </div>
          // <EventCard key={e.id} event={e} />
        ))}
      </div>
    </>
  );
};

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

interface ShowStatus {
  past: boolean;
  pending: boolean;
  published: boolean;
  suppressed: boolean;
}

interface ShowCategory {
  [category: string]: boolean;
}

interface Show {
  status: ShowStatus;
  category: ShowCategory;
}

interface CategoryStats {
  [category: string]: number;
}

type StatusStats = { [key in keyof ShowStatus]: number };

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
    status: {
      past: false,
      pending: true,
      published: true,
      suppressed: false,
    },
    category: {},
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
          a.category[e.categoryName] ??= 0;
          a.category[e.categoryName]++;
        } else {
          a.category['None'] ??= 0;
          a.category['None']++;
        }
        return a;
      }, defaultStats());

      stats.total.past = events.filter(
        (e) => startOfDay(new Date(e.startDateTime ?? e.startDate!)) <= today,
      ).length;

      // Default category show to true
      const categories = Object.keys(stats.category).reduce((a, c) => {
        a[c] ??= true;
        return a;
      }, {} as ShowCategory);

      setStats(stats);
      setShow({
        ...show,
        category: categories,
      });
    }
  }, [events]);

  useEffect(() => {}, [scraperId]);

  const filteredEvents = useMemo(() => {
    let filtered: Event[] = [...events];

    if (!show.status.past) {
      filtered = filtered.filter(
        (e) => startOfDay(new Date(e.startDateTime ?? e.startDate!)) >= today,
      );
    }

    if (!show.status.pending) {
      filtered = filtered.filter((e) => e.status !== 'Pending');
    }

    if (!show.status.published) {
      filtered = filtered.filter((e) => e.status !== 'Published');
    }

    if (!show.status.suppressed) {
      filtered = filtered.filter((e) => e.status !== 'Suppressed');
    }

    var categories = Object.keys(stats.category);

    categories.forEach((c) => {
      if (!show.category[c]) {
        filtered = filtered.filter((e) => e.categoryName !== c);
      }
    });

    return filtered;
  }, [events, show]);

  return (
    <>
      <div className={pageStyles.pageHeader}>
        <h3>Output</h3>
      </div>
      <Card
        className={cn(
          cardStyles.BgCard,
          cardStyles.MarginBottom,
          cardStyles.Opaque,
          styles.FilterRow,
        )}
      >
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
                checked={show.status.past}
                onChange={() =>
                  setShow({
                    ...show,
                    status: { ...show.status, past: !show.status.past },
                  })
                }
              />
            </div>
            <div>
              <FormCheck
                label={`Pending (${stats.total.pending})`}
                checked={show.status.pending}
                onChange={() =>
                  setShow({
                    ...show,
                    status: { ...show.status, pending: !show.status.pending },
                  })
                }
              />
            </div>
            <div>
              <FormCheck
                label={`Published (${stats.total.published})`}
                checked={show.status.published}
                onChange={() =>
                  setShow({
                    ...show,
                    status: {
                      ...show.status,
                      published: !show.status.published,
                    },
                  })
                }
              />
            </div>
            <div>
              <FormCheck
                label={`Archived (${stats.total.suppressed})`}
                checked={show.status.suppressed}
                onChange={() =>
                  setShow({
                    ...show,
                    status: {
                      ...show.status,
                      suppressed: !show.status.suppressed,
                    },
                  })
                }
              />
            </div>
          </div>
          <div>
            {Object.entries(stats?.category).map(([category, count]) => (
              <div key={category}>
                <FormCheck
                  label={`${category}: ${count}`}
                  checked={show.category[category]}
                  onChange={() =>
                    setShow({
                      ...show,
                      category: {
                        ...show.category,
                        [category]: !show.category[category],
                      },
                    })
                  }
                />
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
                border: e.isReviewRequired
                  ? '2px solid #7a3333'
                  : e.status === 'Pending'
                    ? '2px dashed orange'
                    : undefined,
                boxShadow: e.isReviewRequired
                  ? '0 0 10px #c96f6f88'
                  : undefined,
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
                  <p
                    className={cn(styles.Clamp, !e.description && styles.Muted)}
                  >
                    {e.description ?? 'No description available'}
                  </p>
                  <label>
                    <h4>
                      {format(
                        new Date(
                          (e.showStartTime ? e.startDateTime : e.startDate)!,
                        ),
                        'MMM dd yyy',
                      )}
                    </h4>
                    {e.showStartTime &&
                      format(new Date(e.startDateTime!), 'hh:mm aa')}
                  </label>
                  <div>
                    {e.detailsUrl && (
                      <SquareButton href={e.detailsUrl} target="_blank">
                        Details
                      </SquareButton>
                    )}
                    {e.ticketUrl && (
                      <SquareButton href={e.ticketUrl} target="_blank">
                        Tickets
                      </SquareButton>
                    )}
                  </div>
                </div>
                <div>
                  <SquareButton>
                    <Icon name="copy" />
                  </SquareButton>
                </div>
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
              <Button
                onClick={() =>
                  EventsApi.patch(e.id, {
                    requiresReview: !e.isReviewRequired,
                  }).then(() => loadEvents())
                }
              >
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

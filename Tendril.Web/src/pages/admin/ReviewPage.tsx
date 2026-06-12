import { useCallback, useEffect, useState } from 'react';
import { CategoriesApi } from '../../api/categories';
import { EventsApi } from '../../api/events';
import { SquareButton as Button } from '../../components/button';
import { FormSelect } from '../../components/form';
import { Icon } from '../../components/Icon';
import { ReviewEventCard } from '../../events';
import { pageStyles } from '../../styles';
import type { Category, Event } from '../../types/api';

// TODO: need to generalize this apparently
import styles from '../../scrapers/Tab.module.css';

interface Props {}

export const ReviewPage: React.FC<Props> = () => {
  const [loading, setLoading] = useState<boolean>(true);
  const [events, setEvents] = useState<Event[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);

  // Initial load
  useEffect(() => {
    var abortController = new AbortController();

    loadEvents(abortController.signal);
    loadCategories(abortController.signal);
  }, []);

  const loadEvents = useCallback(async (signal?: AbortSignal) => {
    setLoading(true);
    try {
      const data = await EventsApi.getPending(signal);

      setEvents(data);
    } catch (error) {
      console.error('Failed to fetch scrapers', error);
    } finally {
      setLoading(false);
    }
  }, []);

  const loadCategories = useCallback(async (signal?: AbortSignal) => {
    try {
      const result = await CategoriesApi.getAll(signal);

      const sortProp = (c: Category) => c.name;

      const sorted = result.sort((a, b) =>
        sortProp(a).localeCompare(sortProp(b)),
      );

      setCategories(sorted);
    } catch (err) {
      console.error('Failed to fetch categories', err);
    }
  }, []);

  return (
    <section>
      <div className={pageStyles.pageHeader}>
        <h2>Review</h2>
      </div>
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(1, 1fr)',
          gap: '1rem',
          placeItems: 'stretch',
        }}
      >
        {events.map((e) => (
          <div
            key={e.id}
            style={{ display: 'flex', gap: '1em', alignItems: 'stretch' }}
          >
            <ReviewEventCard e={e} />
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
        ))}
      </div>
    </section>
  );
};

export default ReviewPage;

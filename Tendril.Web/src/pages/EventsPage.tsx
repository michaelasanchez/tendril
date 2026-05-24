import cn from 'classnames';
import { format } from 'date-fns';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Col, Offcanvas, Row, Spinner } from 'react-bootstrap';
import { useMatch, useNavigate } from 'react-router';
import { CategoriesApi } from '../api/categories';
import { EventsApi, type EventFilter } from '../api/events';
import { VenuesApi } from '../api/venues';
import { SquareButton } from '../components/button';
import { Icon } from '../components/Icon';
import { EventModal } from '../components/modal';
import { EventList, FiltersCard } from '../events';
import { useLocalStorage } from '../hooks';
import { pageStyles } from '../styles';
import type { Category, Event, EventResponse, Guid, Venue } from '../types/api';
import styles from './EventsPage.module.css';

type View = 'list' | 'map' | 'calendar';

interface Loading {
  events: boolean;
  categories: boolean;
  venues: boolean;
}

type Result = Omit<EventResponse, 'items'>;

export const EventsPage: React.FC = () => {
  const [view] = useState<View>('list');
  const [events, setEvents] = useState<Event[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [venues, setVenues] = useState<Venue[]>([]);
  const [loading, setLoading] = useState<Loading>({
    events: false,
    categories: false,
    venues: false,
  });

  const [result, setResult] = useState<Result>({
    categoryIds: [],
    venueIds: [],
    totalCount: 0,
    nextCursor: null,
    hasNextPage: false,
  });

  const favoritesStorage = useLocalStorage('favorites');
  const [favorites, setFavorites] = useState<Set<Guid>>(
    () => new Set(JSON.parse(favoritesStorage.fetch() || '[]')),
  );

  const filtersStorage = useLocalStorage('filters');
  const [showFavoritesOnly, setShowFavoritesOnly] = useState<boolean>(false);
  const [filter, setFilter] = useState<EventFilter>({
    ...(filtersStorage.exists() ? JSON.parse(filtersStorage.fetch()!) : {}),
    startDate: format(new Date(), 'yyyy-MM-dd'),
    endDate: '',
  });

  const [showFilters, setShowFilters] = useState(false);

  const navigate = useNavigate();
  const eventMatch = useMatch('/event/:id');

  const activeEventId = eventMatch?.params.id;

  const activeIndex = activeEventId
    ? events.findIndex((e) => e.id === activeEventId)
    : -1;

  const handleModalClose = () => navigate('/');

  // const handleOnNext = () => {
  //   const currentIndex = filteredEvents.findIndex(
  //     (e) => e.id === activeEventId,
  //   );

  //   if (currentIndex >= 0 && currentIndex < filteredEvents.length - 1) {
  //     const nextEvent = filteredEvents[currentIndex + 1];
  //     navigate(`/event/${nextEvent.id}`);
  //   }
  // };

  // const handleOnPrev = () => {
  //   const currentIndex = filteredEvents.findIndex(
  //     (e) => e.id === activeEventId,
  //   );

  //   if (currentIndex > 0) {
  //     const prevEvent = filteredEvents[currentIndex - 1];
  //     navigate(`/event/${prevEvent.id}`);
  //   }
  // };

  const handleFavorite = (event: Event) => {
    setFavorites((prev) => {
      const updated = new Set(prev);

      if (updated.has(event.id)) {
        updated.delete(event.id);
      } else {
        updated.add(event.id);
      }

      return updated;
    });
  };

  const loadEvents = useCallback(
    async (
      filter: EventFilter | null,
      cursor: string | null,
      signal?: AbortSignal,
      shouldAppend = false,
    ) => {
      setLoading((prev) => ({ ...prev, events: true }));

      try {
        const { items, ...result } = await EventsApi.get(
          filter,
          cursor,
          signal,
        );

        setEvents((prev) => (shouldAppend ? [...prev, ...items] : items));
        setResult(result);
      } finally {
        if (!signal?.aborted) {
          setLoading((prev) => ({ ...prev, events: false }));
        }
      }
    },
    [],
  );

  const loadCategories = useCallback(async (signal?: AbortSignal) => {
    setLoading((prev) => ({ ...prev, categories: true }));

    try {
      const result = await CategoriesApi.getAll(signal);

      setCategories(result);
    } catch (err) {
      console.error('Failed to fetch categories', err);
    } finally {
      if (!signal?.aborted) {
        setLoading((prev) => ({ ...prev, categories: false }));
      }
    }
  }, []);

  const loadVenues = useCallback(async (signal?: AbortSignal) => {
    setLoading((prev) => ({ ...prev, venues: true }));

    try {
      const result = await VenuesApi.getAll(signal);

      const sortProp = (v: Venue) => v.name.replace(/^The\s+/i, ''); // Ignore "The" at the start for sorting

      const sorted = result.sort((a, b) =>
        sortProp(a).localeCompare(sortProp(b)),
      );

      setVenues(sorted);
    } catch (err) {
      console.error('Failed to fetch venues', err);
    } finally {
      if (!signal?.aborted) {
        setLoading((prev) => ({ ...prev, venues: false }));
      }
    }
  }, []);

  // Initial load (categories, venues)
  useEffect(() => {
    const controller = new AbortController();

    loadCategories(controller.signal);
    loadVenues(controller.signal);

    return () => controller.abort();
  }, []);

  // This preloads events in details mode (when the modal is open)
  useEffect(() => {
    const controller = new AbortController();
    const signal = controller.signal;

    if (
      activeIndex !== null &&
      activeIndex + 1 == filteredEvents.length &&
      result.nextCursor
    ) {
      loadEvents(filter, result.nextCursor, signal, true);
    }

    return () => {
      controller.abort();
    };
  }, [activeIndex]);

  // Reload events on filter change (+ write to storage)
  useEffect(() => {
    filtersStorage.commit(JSON.stringify(filter));

    const controller = new AbortController();

    loadEvents(filter, null, controller.signal, false);

    return () => {
      controller.abort();
    };
  }, [filter]);

  // Write favorites to storage
  useEffect(() => {
    favoritesStorage.commit(JSON.stringify([...favorites]));
  }, [favorites]);

  const filteredEvents = useMemo(() => {
    if (showFavoritesOnly) {
      return events.filter((e) => favorites.has(e.id));
    }

    return events;
  }, [events, favorites, showFavoritesOnly]);

  const observer = useRef<IntersectionObserver>(null);

  const lastElementRef = useCallback(
    (node: HTMLDivElement | null) => {
      if (loading.events) return;

      if (observer.current) observer.current.disconnect();

      observer.current = new IntersectionObserver((entries) => {
        if (
          entries[0].isIntersecting &&
          result.nextCursor &&
          !showFavoritesOnly
        ) {
          loadEvents(filter, result.nextCursor, undefined, true);
        }
      });

      if (node) observer.current.observe(node);
    },
    // Fix dependencies to be specific
    [loading.events, result.nextCursor, loadEvents, filter],
  );

  // const calendarEvents = useMemo(
  //   () =>
  //     events?.map((e, i) => {
  //       const date = new Date(e.startUtc);

  //       return {
  //         id: i,
  //         title: e.title,
  //         start: date,
  //         end: date,
  //       };
  //     }) ?? [],
  //   [events]
  // );

  return (
    <>
      <section>
        <h1 className={styles.PageTitle}>Upcoming Events</h1>
        <div className={styles.SubHeaderRow}>
          <div className={cn(pageStyles.SubHeader, styles.EventsFound)}>
            {!loading.events && (
              <>
                {showFavoritesOnly ? filteredEvents.length : result.totalCount}{' '}
                events found
                {loading.events && <Spinner animation="border" size="sm" />}
              </>
            )}
          </div>
          <div className={cn('d-lg-none', styles.PageControls)}>
            {!loading.events && (
              <SquareButton
                variant="outline-primary"
                onClick={() => setShowFilters(true)}
              >
                <Icon name="sliders" /> Filters
              </SquareButton>
            )}
          </div>
        </div>

        {view === 'list' && (
          <Row>
            <Col lg={4}>
              <Offcanvas
                show={showFilters}
                onHide={() => setShowFilters(false)}
                responsive="lg"
                placement="end"
              >
                <Offcanvas.Header closeButton className={styles.Header}>
                  <div>
                    <Offcanvas.Title className={styles.Heading} as="h3">
                      <Icon name="filter" />
                      Filters
                    </Offcanvas.Title>
                    <div
                      className={cn(
                        'd-lg-none',
                        pageStyles.SubHeader,
                        styles.EventsFound,
                      )}
                    >
                      {showFavoritesOnly
                        ? filteredEvents.length
                        : result.totalCount}{' '}
                      events found
                      {loading.events && (
                        <Spinner animation="border" size="sm" />
                      )}
                    </div>
                  </div>
                </Offcanvas.Header>
                <Offcanvas.Body className={styles.Body}>
                  <FiltersCard
                    className={styles.FiltersCard}
                    filter={filter}
                    favoritesOnly={showFavoritesOnly}
                    categories={categories}
                    venues={venues}
                    onChange={(update) =>
                      setFilter((prev) => ({ ...prev, ...update }))
                    }
                    onToggleFavoritesOnly={() =>
                      setShowFavoritesOnly((v) => !v)
                    }
                  />
                </Offcanvas.Body>
              </Offcanvas>
            </Col>
            <Col lg={8} className={cn(styles.EventsColumn)}>
              <EventList
                className={styles.EventList}
                events={filteredEvents}
                favorites={favorites}
                lastRef={lastElementRef}
                onEventClick={(clicked) => navigate(`/event/${clicked.id}`)}
                onFavorite={handleFavorite}
              />
              {loading.events && (
                <div className={styles.Loading}>
                  <Spinner animation="border" className={styles.Spinner} />
                  Loading...
                </div>
              )}
            </Col>
          </Row>
        )}
      </section>

      <EventModal
        event={activeIndex >= 0 ? filteredEvents[activeIndex] : null}
        venues={venues}
        show={activeIndex >= 0}
        onHide={handleModalClose}
        // onNext={handleOnNext}
        // onPrev={handleOnPrev}
      />
    </>
  );
};

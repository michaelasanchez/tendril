import cn from 'classnames';
import { format } from 'date-fns';
import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { Col, Row, Spinner } from 'react-bootstrap';
import { useMatch, useNavigate } from 'react-router-dom';
import { CategoriesApi } from '../api/categories';
import { EventsApi, type EventFilter } from '../api/events';
import { VenuesApi } from '../api/venues';
import { SquareButton } from '../components/button';
import { Icon } from '../components/Icon';
import { EventModal } from '../components/modal';
import { EventList, FiltersCard } from '../events';
import { useLocalStorage } from '../hooks';
import { pageStyles } from '../styles';
import type { Category, Event, Guid, Venue } from '../types/api';
import styles from './EventsPage.module.css';

type View = 'list' | 'map' | 'calendar';

interface Loading {
  events: boolean;
  venues: boolean;
}

export const EventsPage: React.FC = () => {
  const [view] = useState<View>('list');
  const [showFilters, setShowFilters] = useState<boolean>(false);
  const [events, setEvents] = useState<Event[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [venues, setVenues] = useState<Venue[]>([]);
  const [loading, setLoading] = useState<Loading>({
    events: false,
    venues: false,
  });
  const [nextCursor, setNextCursor] = useState<Guid | null>(null);
  const [totalCount, setTotalCount] = useState<number>(0);

  const favoritesStorage = useLocalStorage('favorites');
  const [favorites, setFavorites] = useState<Set<Guid>>(
    () => new Set(JSON.parse(favoritesStorage.fetch() || '[]')),
  );

  const [showFavoritesOnly, setShowFavoritesOnly] = useState<boolean>(false);
  const [filter, setFilter] = useState<EventFilter>({
    startDate: format(new Date(), 'yyyy-MM-dd'),
    endDate: ''
  });

  const navigate = useNavigate();
  const eventMatch = useMatch('/event/:id');

  const activeEventId = eventMatch?.params.id;

  const activeIndex = activeEventId
    ? events.findIndex((e) => e.id === activeEventId)
    : -1;

  const handleModalClose = () => navigate('/');

  const handleOnNext = () => {
    const currentIndex = filteredEvents.findIndex(
      (e) => e.id === activeEventId,
    );

    if (currentIndex >= 0 && currentIndex < filteredEvents.length - 1) {
      const nextEvent = filteredEvents[currentIndex + 1];
      navigate(`/event/${nextEvent.id}`);
    }
  };

  const handleOnPrev = () => {
    const currentIndex = filteredEvents.findIndex(
      (e) => e.id === activeEventId,
    );

    if (currentIndex > 0) {
      const prevEvent = filteredEvents[currentIndex - 1];
      navigate(`/event/${prevEvent.id}`);
    }
  };

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
        const result = await EventsApi.get(filter, cursor, signal);
        setEvents((prev) =>
          shouldAppend ? [...prev, ...result.items] : result.items,
        );
        setNextCursor(result.nextCursor);
        setTotalCount(result.totalCount);
      } finally {
        setLoading((prev) => ({ ...prev, events: false }));
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
      setLoading((prev) => ({ ...prev, categories: false }));
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
      setLoading((prev) => ({ ...prev, venues: false }));
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
      nextCursor
    ) {
      loadEvents(filter, nextCursor, signal, true);
    }

    return () => {
      controller.abort();
    };
  }, [activeIndex]);

  // Keep favorites in sync
  useEffect(() => {
    favoritesStorage.commit(JSON.stringify([...favorites]));
  }, [favorites]);

  //
  useEffect(() => {
    const controller = new AbortController();

    loadEvents(filter, null, controller.signal, false);

    return () => {
      controller.abort();
    };
  }, [filter]);

  const observer = useRef<IntersectionObserver>(null);

  const lastElementRef = useCallback(
    (node: HTMLDivElement | null) => {
      if (loading.events) return;

      if (observer.current) observer.current.disconnect();

      observer.current = new IntersectionObserver((entries) => {
        if (entries[0].isIntersecting && nextCursor && !showFavoritesOnly) {
          loadEvents(filter, nextCursor, undefined, true);
        }
      });

      if (node) observer.current.observe(node);
    },
    // Fix dependencies to be specific
    [loading.events, nextCursor, loadEvents, filter],
  );

  const filteredEvents = useMemo(() => {
    if (showFavoritesOnly) {
      return events.filter((e) => favorites.has(e.id));
    }

    return events;
  }, [events, favorites, showFavoritesOnly]);

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
          <div className={pageStyles.SubHeader}>{totalCount} events found</div>
          <div className={cn('d-lg-none', styles.PageControls)}>
            <SquareButton
              variant={showFilters ? 'primary' : undefined}
              onClick={() => setShowFilters(!showFilters)}
            >
              <Icon name="sliders" /> Filters
            </SquareButton>
          </div>
        </div>

        {view === 'list' && (
          <Row>
            <Col lg={4}>
              <div
                className={cn('d-lg-block', showFilters ? 'd-block' : 'd-none')}
              >
                <FiltersCard
                  className={styles.FiltersCard}
                  filter={filter}
                  favoritesOnly={showFavoritesOnly}
                  categories={categories}
                  venues={venues}
                  onChange={(update) =>
                    setFilter((prev) => ({ ...prev, ...update }))
                  }
                  onToggleFavoritesOnly={() => setShowFavoritesOnly((v) => !v)}
                />
              </div>
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
                  Loading more...
                </div>
              )}
            </Col>
          </Row>
        )}
      </section>

      <EventModal
        event={activeIndex >= 0 ? filteredEvents[activeIndex] : null}
        show={activeIndex >= 0}
        onHide={handleModalClose}
        onNext={handleOnNext}
        onPrev={handleOnPrev}
      />
    </>
  );
};

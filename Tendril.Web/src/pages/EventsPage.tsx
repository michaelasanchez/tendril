import cn from 'classnames';
import { format, parse } from 'date-fns';
import React, { useEffect, useMemo, useState } from 'react';
import { Col, Container, Row } from 'react-bootstrap';
import { EventsApi } from '../api/events';
import { SquareButton } from '../components/button';
import { Icon } from '../components/Icon';
import { EventModal } from '../components/modal';
import { EventList, FiltersCard, type EventFilter } from '../events';
import { pageStyles } from '../styles';
import type { Event, Guid } from '../types/api';
import styles from './EventsPage.module.css';

type View = 'list' | 'map' | 'calendar';

export const EventsPage: React.FC = () => {
  const [view] = useState<View>('list');
  const [showFilters, setShowFilters] = useState<boolean>();
  const [events, setEvents] = useState<Event[]>([]);
  const [activeEvent, setActiveEvent] = useState<Event | null>(null);
  const [favorites, setFavorites] = useState<Set<Guid>>(new Set());
  const [filter, setFilter] = useState<EventFilter>({
    startDate: format(new Date(), 'yyyy-MM-dd'),
  });

  const handleModalClose = () => setActiveEvent(null);

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

  const handleSetFilter = (update: Partial<EventFilter>) => {
    setFilter((prev) => ({ ...prev, ...update }));
  };

  // Initial load
  useEffect(() => {
    void (async () => {
      const data = await EventsApi.getAll();

      setEvents(data);
    })();
  }, []);

  const filteredEvents = useMemo(() => {
    const { title, startDate, endDate, category, location } = filter;

    let filtered = events;

    if (filter.favoritesOnly) {
      filtered = filtered.filter((e) => favorites.has(e.id));
    }

    if (title) {
      filtered = filtered.filter((e) =>
        e.title.toLowerCase().includes(title.toLowerCase())
      );
    }

    if (startDate) {
      const from = parse(startDate, 'yyyy-MM-dd', new Date());
      filtered = filtered.filter((e) => from <= new Date(e.startUtc));
    }

    if (endDate) {
      const to = parse(endDate, 'yyyy-MM-dd', new Date());
      filtered = filtered.filter((e) => to >= new Date(e.startUtc));
    }

    if (category) {
      filtered = filtered.filter((e) => e.category === category);
    }

    if (location) {
      filtered = filtered.filter(
        (e) => e.location === location || e.venueName === location
      );
    }

    return filtered;
  }, [events, favorites, filter]);

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

  const locations = useMemo(
    () => Array.from(new Set(events.map((e) => e.location ?? e.venueName))),
    [events]
  ) as string[];

  return (
    <Container>
      <section>
        <div className={styles.PageHeader}>
          <div>
            <h1>Upcoming Events</h1>
            <p className={pageStyles.SubHeader}>
              {filteredEvents?.length ?? 0} events found
            </p>
          </div>
          <div>

          </div>
        </div>
        <div className={cn('d-lg-none', styles.PageControls)}>
          <SquareButton
            variant={showFilters ? 'primary' : undefined}
            onClick={() => setShowFilters(!showFilters)}
          >
            <Icon name="sliders" /> Filters
          </SquareButton>
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
                  locations={locations}
                  onChange={handleSetFilter}
                />
              </div>
            </Col>
            <Col lg={8}>
              <EventList
                className={styles.EventList}
                events={filteredEvents}
                favorites={favorites}
                onEventClick={setActiveEvent}
                onFavorite={handleFavorite}
              />
            </Col>
          </Row>
        )}

        {/* {view === "calendar" && <EventCalendar events={calendarEvents} />} */}

        <EventModal
          event={activeEvent}
          show={!!activeEvent}
          onHide={handleModalClose}
        />
      </section>
    </Container>
  );
};

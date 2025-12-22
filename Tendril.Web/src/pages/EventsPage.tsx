import React, { useEffect, useMemo, useState } from "react";
import { Container } from "react-bootstrap";
import { EventsApi } from "../api/events";
import { EventCalendar, EventList } from "../events";
import type { Event } from "../types/api";
import styles from "./EventsPage.module.css";

export const EventsPage: React.FC = () => {
  const [view, setView] = useState<"list" | "calendar">("list");
  const [events, setEvents] = useState<Event[]>([]);
  const [venueFilter, setVenueFilter] = useState<string | null>(null);

  // Initial load
  useEffect(() => {
    void (async () => {
      const data = await EventsApi.getAll();

      setEvents(data);
    })();
  }, []);

  const calendarEvents = useMemo(
    () =>
      events?.map((e, i) => {
        const date = new Date(e.startUtc);

        return {
          id: i,
          title: e.title,
          start: date,
          end: date,
        };
      }) ?? [],
    [events]
  );

  const venueOptions = useMemo(
    () => Array.from(new Set(events.map((e) => e.venueName))),
    [events]
  ) as string[];

  console.log(
    "venue",
    venueFilter,
    Boolean(venueFilter),
    Boolean(venueFilter) && styles.withFilter
  );

  return (
    <Container>
      <section>
        <div className={styles.pageHeader}>
          <h2>Events</h2>
        </div>

        <div className={styles.pageControls}>
          <label>
            <span>Venue</span>
            <select
              value={venueFilter ?? ""}
              onChange={(e) => setVenueFilter(e.target.value)}
            >
              <option value=""></option>
              {venueOptions.map((o) => (
                <option key={o} value={o}>
                  {o}
                </option>
              ))}
            </select>
          </label>
        </div>

        {view === "list" && (
          <EventList events={events} venueFilter={venueFilter} />
        )}
        {view === "calendar" && <EventCalendar events={calendarEvents} />}
      </section>
    </Container>
  );
};

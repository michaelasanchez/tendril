import React, { useEffect, useMemo, useState } from "react";
import { EventsApi } from "../api/events";
import { EventCalendar, EventList } from "../events";
import type { Event } from "../types/api";
import { Container } from "react-bootstrap";

export const EventsPage: React.FC = () => {
  const [view, setView] = useState<"list" | "calendar">("list");
  const [events, setEvents] = useState<Event[]>([]);

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

  return (
      <Container>
    <section>
        
      <div className="page-header">
        <h2>Events</h2>
      </div>

      {view === "list" && <EventList events={events} />}
      {view === "calendar" && <EventCalendar events={calendarEvents} />}
    </section>
      </Container>
  );
};

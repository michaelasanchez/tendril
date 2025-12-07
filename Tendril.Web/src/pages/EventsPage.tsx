// src/pages/EventsPage.tsx
import moment from "moment";
import React, { useEffect, useState } from "react";
import type { Event, View } from "react-big-calendar";
import { Calendar, momentLocalizer } from "react-big-calendar";
import { EventsApi } from "../api/events";

export const EventsPage: React.FC = () => {
  const [events, setEvents] = useState<Event[]>([]);

  const [date, setDate] = useState<Date>(new Date());
  const [view, setView] = useState<View>("month");

  const localizer = momentLocalizer(moment);

  useEffect(() => {
    void (async () => {
      const data = await EventsApi.getAll();

      const events = data?.map<Event>((e, i) => {
        const date = new Date(e.startUtc);

        return {
          id: i,
          title: e.title,
          start: date,
          end: date,
        };
      });

      setEvents(events);
    })();
  }, []);

  return (
    <section>
      <div className="page-header">
        <h2>Events</h2>
      </div>

      <Calendar
        localizer={localizer}
        events={events}
        startAccessor="start"
        endAccessor="end"
        views={['month', 'agenda']}
        view={view}
        date={date}
        onNavigate={(date, view) => {
          setDate(date);
          setView(view);
        }}
        onView={setView}
        style={{ height: 800, width: "100vw" }}
      />
    </section>
  );
};
